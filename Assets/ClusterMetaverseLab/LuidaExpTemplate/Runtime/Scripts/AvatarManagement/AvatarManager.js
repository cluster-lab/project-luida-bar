// ===== LUIDA Avatar Manager =====
// Runs on the LUIDA-AvatarSpawner item.
// Reads AVATAR_INDEX_MAP from the prepended constants header.
//
// Supports two input paths:
//   1. Direct messages: luida_assign_avatar / luida_unassign_avatar (from state-listening items)
//   2. Gimmick integer commands: polls global states "luida_avatar_cmd" + "luida_avatar_participant"
//
// Both paths route through tryExecuteOrQueue, which defers execution until the
// target participant is "ready" (in $.groupState.participants, exists(), and
// has humanoid pose data) — see #avatar-fake-gimmick-race-fix. Falls back to
// best-effort execution after MAX_WAIT_SECONDS to keep behavior bounded.

const MAX_WAIT_SECONDS = 5;

$.onStart(() => {
  $.state.createdAvatars = []; // flat list of created avatar item handles
  $.state.lastCmd = 0;         // last processed command value
  // One slot per participant number; later commands overwrite earlier ones.
  // Entry: { op: 'assign'|'unassign', avatarID?: string, waitTime: number }
  $.state.pendingByParticipant = {};
});

// --- Resolve target to a PlayerHandle ---
// target can be: PlayerHandle (from direct callers) or integer (participant number, 1-based).
// Returns null silently when out of bounds — the readiness queue is responsible
// for distinguishing "not registered yet" (transient) from "never will be"
// (timeout warning fires after MAX_WAIT_SECONDS).
function resolvePlayer(target) {
  if (target === null || target === undefined) return null;
  if (typeof target === "number") {
    const participants = $.groupState.participants;
    if (!participants || target < 1 || target > participants.length) return null;
    return participants[target - 1];
  }
  return target;
}

// --- Core assignment logic ---
function assignAvatarToPlayer(player, avatarID) {
  if (!player || !player.exists()) {
    $.log("[AvatarManager] Cannot assign avatar: player does not exist");
    return;
  }

  // Remove any existing avatars for this player first
  unassignAllFromPlayer(player);

  // Spawn the wrapper item
  try {
    const handle = $.createItem(
      new WorldItemTemplateId(avatarID),
      player.getPosition(),
      player.getRotation()
    );
    handle.send("assignPlayer", { player: player });

    const list = $.state.createdAvatars || [];
    list.push(handle);
    $.state.createdAvatars = list;

    $.log("[AvatarManager] Assigned avatar '" + avatarID + "' to player " + player.userDisplayName);
  } catch (e) {
    $.log("[AvatarManager] createItem failed (rate limit?): " + e);
  }
}

function unassignAllFromPlayer(player) {
  if (!player) return;

  const list = $.state.createdAvatars || [];
  for (let i = 0; i < list.length; i++) {
    try {
      list[i].send("unassignIfPlayer", player.id);
    } catch (e) { /* item may already be gone */ }
  }

  $.log("[AvatarManager] Unassigned all avatars from player " + player.userDisplayName);
}

// --- Readiness gate ---
// A participant is "ready" to receive an avatar command when they're enrolled
// in $.groupState.participants, their PlayerHandle exists, and Cluster has
// finished populating their humanoid pose data. The third gate is what
// AvatarSyncClone actually needs to drive the bones — without it, the wrapper
// can spawn but stay in T-pose if pose data never arrives before sync starts.
function isPlayerReady(player) {
  if (!player || !player.exists()) return false;
  try {
    return player.getHumanoidBoneRotation(HumanoidBone.Hips) != null;
  } catch (e) {
    return false;
  }
}

// Execute a queued payload now. No readiness check — caller is responsible.
// Returns true on success, false on no-op (e.g. participant disappeared).
function executePayload(payload, participantNumber) {
  const player = resolvePlayer(participantNumber);
  if (!player) return false;
  if (payload.op === "assign") {
    if (!payload.avatarID) return false;
    assignAvatarToPlayer(player, payload.avatarID);
    return true;
  }
  if (payload.op === "unassign") {
    unassignAllFromPlayer(player);
    return true;
  }
  return false;
}

function clearPending(participantNumber) {
  if (!$.state.pendingByParticipant[participantNumber]) return;
  const next = { ...$.state.pendingByParticipant };
  delete next[participantNumber];
  $.state.pendingByParticipant = next;
}

function queuePending(payload, participantNumber) {
  const next = { ...$.state.pendingByParticipant };
  next[participantNumber] = {
    op: payload.op,
    avatarID: payload.avatarID,
    waitTime: 0
  };
  $.state.pendingByParticipant = next;
}

// Tries to execute now. If the participant isn't ready, queues the payload
// (overwriting any older queued payload for the same participant, so rapid
// re-triggers resolve to the latest command).
function tryExecuteOrQueue(payload, participantNumber) {
  const player = resolvePlayer(participantNumber);
  if (isPlayerReady(player)) {
    executePayload(payload, participantNumber);
    clearPending(participantNumber);
    return;
  }
  queuePending(payload, participantNumber);
  $.log("[AvatarManager] Participant " + participantNumber +
        " not ready — queued op=" + payload.op +
        (payload.avatarID ? (" avatarID=" + payload.avatarID) : ""));
}

// --- Message handlers (for state-listening items) ---
$.onReceive((messageType, arg, sender) => {
  if (messageType === "luida_assign_avatar") {
    const participantNumber = arg.target !== undefined ? arg.target : arg.participantIndex;
    if (typeof participantNumber !== "number") return;
    tryExecuteOrQueue({ op: "assign", avatarID: arg.avatarID }, participantNumber);
  }

  if (messageType === "luida_unassign_avatar") {
    const participantNumber = arg.target !== undefined ? arg.target : arg.participantIndex;
    if (typeof participantNumber !== "number") return;
    tryExecuteOrQueue({ op: "unassign" }, participantNumber);
  }
});

// --- Gimmick trigger polling (integer command) ---
// Polls two global integer states:
//   "luida_avatar_cmd"         - action (>0 = assign avatar at index cmd-1, -1 = unassign)
//   "luida_avatar_participant" - participant number (1-based)
// After handling, sends a single reset signal to clear both.
$.onUpdate((deltaTime) => {
  if (typeof AVATAR_INDEX_MAP === "undefined") return;

  try {
    const cmd = $.getStateCompat("global", "luida_avatar_cmd", "integer");
    if (cmd !== 0 && cmd !== $.state.lastCmd) {
      $.state.lastCmd = cmd;

      const participantNumber = $.getStateCompat("global", "luida_avatar_participant", "integer") || 1;

      if (cmd > 0) {
        // Assign: cmd = avatarIndex + 1 (1-based)
        const avatarID = AVATAR_INDEX_MAP[cmd - 1];
        if (avatarID) {
          tryExecuteOrQueue({ op: "assign", avatarID: avatarID }, participantNumber);
        }
      } else if (cmd === -1) {
        tryExecuteOrQueue({ op: "unassign" }, participantNumber);
      }

      // Reset both global states via CCK GlobalLogic on the spawner.
      // Fired immediately on pickup (not after the work completes) so the
      // gimmick stays re-triggerable while the command is still queued.
      $.sendSignalCompat("this", "luida_avatar_cmd_reset");
    }

    if (cmd === 0 && $.state.lastCmd !== 0) {
      $.state.lastCmd = 0;
    }
  } catch (e) {
    $.log("[AvatarManager] Gimmick trigger poll error: " + e);
  }

  // --- Retry queued commands ---
  // For each pending entry: execute if the participant is now ready, time out
  // after MAX_WAIT_SECONDS, otherwise just advance waitTime. Uses the
  // immutable-copy pattern so $.state propagates.
  const pending = $.state.pendingByParticipant;
  if (!pending) return;
  let nextPending = pending;
  let dirty = false;
  for (const key in pending) {
    if (!Object.prototype.hasOwnProperty.call(pending, key)) continue;
    const entry = pending[key];
    const participantNum = parseInt(key, 10);
    const player = resolvePlayer(participantNum);

    if (isPlayerReady(player)) {
      const payload = { op: entry.op, avatarID: entry.avatarID };
      executePayload(payload, participantNum);
      $.log("[AvatarManager] Deferred op=" + entry.op +
            (entry.avatarID ? (" avatarID=" + entry.avatarID) : "") +
            " executed for participant " + key +
            " after " + entry.waitTime.toFixed(2) + "s");
      if (!dirty) { nextPending = { ...pending }; dirty = true; }
      delete nextPending[key];
      continue;
    }

    const newWait = entry.waitTime + deltaTime;
    if (newWait >= MAX_WAIT_SECONDS) {
      $.log("[AvatarManager] WARN: timeout (" + MAX_WAIT_SECONDS +
            "s) waiting for participant " + key +
            " — executing op=" + entry.op + " anyway");
      const payload = { op: entry.op, avatarID: entry.avatarID };
      executePayload(payload, participantNum);
      if (!dirty) { nextPending = { ...pending }; dirty = true; }
      delete nextPending[key];
      continue;
    }

    if (!dirty) { nextPending = { ...pending }; dirty = true; }
    nextPending[key] = { op: entry.op, avatarID: entry.avatarID, waitTime: newWait };
  }
  if (dirty) $.state.pendingByParticipant = nextPending;
});

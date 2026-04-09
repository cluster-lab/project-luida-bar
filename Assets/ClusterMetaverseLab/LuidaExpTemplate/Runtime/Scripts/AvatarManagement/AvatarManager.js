// ===== LUIDA Avatar Manager =====
// Runs on the LUIDA-AvatarSpawner item.
// Reads SPAWNER_MODE and DEFAULT_AVATAR_ID from the prepended constants header.
//
// Modes:
//   "messageDriven"     — waits for luida_assign_avatar / luida_unassign_avatar messages
//   "autoAssignOnJoin"  — scans players periodically, assigns DEFAULT_AVATAR_ID to new joiners

const SCAN_INTERVAL = 0.5; // seconds between player scans in autoAssignOnJoin mode

$.onStart(() => {
  $.state.assignments = {};  // playerId → { handle: ItemHandle, avatarID: string }
  $.state.scanTimer = 0;
});

// --- Resolve target to a PlayerHandle ---
// target can be: PlayerHandle (from direct callers) or integer (participant index from state-listening items)
function resolvePlayer(target) {
  if (target === null || target === undefined) return null;
  // If it's a number, look up from groupState.participants
  if (typeof target === "number") {
    const participants = $.groupState.participants;
    if (!participants || target < 0 || target >= participants.length) {
      $.log("[AvatarManager] Invalid participant index: " + target);
      return null;
    }
    return participants[target];
  }
  // Otherwise assume it's already a PlayerHandle
  return target;
}

// --- Core assignment logic ---
function assignAvatarToPlayer(player, avatarID, boneOffsets) {
  if (!player || !player.exists()) {
    $.log("[AvatarManager] Cannot assign avatar: player does not exist");
    return;
  }

  const playerId = player.id;
  const current = $.state.assignments;

  // If already assigned, unassign the old one first
  if (current[playerId]) {
    try {
      current[playerId].handle.send("unassign", null);
    } catch (e) {
      $.log("[AvatarManager] Failed to unassign previous avatar: " + e);
    }
    delete current[playerId];
    $.state.assignments = current;
  }

  // Spawn the wrapper item
  try {
    const handle = $.createItem(
      new WorldItemTemplateId(avatarID),
      player.getPosition(),
      player.getRotation()
    );
    handle.send("assignPlayer", { player: player, boneOffsets: boneOffsets || null });
    current[playerId] = { handle: handle, avatarID: avatarID };
    $.state.assignments = current;
    $.log("[AvatarManager] Assigned avatar '" + avatarID + "' to player " + player.userDisplayName);
  } catch (e) {
    $.log("[AvatarManager] createItem failed (rate limit?): " + e);
  }
}

function unassignPlayer(player) {
  if (!player) return;
  const playerId = player.id;
  const current = $.state.assignments;
  if (current[playerId]) {
    try {
      current[playerId].handle.send("unassign", null);
    } catch (e) {
      $.log("[AvatarManager] Failed to send unassign: " + e);
    }
    delete current[playerId];
    $.state.assignments = current;
    $.log("[AvatarManager] Unassigned avatar from player " + player.userDisplayName);
  }
}

// --- Message handlers ---
$.onReceive((messageType, arg, sender) => {
  if (messageType === "luida_assign_avatar") {
    const player = resolvePlayer(arg.target !== undefined ? arg.target : arg.participantIndex);
    if (player) {
      assignAvatarToPlayer(player, arg.avatarID, arg.boneOffsets);
    }
  }

  if (messageType === "luida_unassign_avatar") {
    const player = resolvePlayer(arg.target !== undefined ? arg.target : arg.participantIndex);
    if (player) {
      unassignPlayer(player);
    }
  }
});

// --- Auto-assign on join (only active when SPAWNER_MODE === "autoAssignOnJoin") ---
$.onUpdate((deltaTime) => {
  if (typeof SPAWNER_MODE === "undefined" || SPAWNER_MODE !== "autoAssignOnJoin") return;

  $.state.scanTimer = ($.state.scanTimer || 0) + deltaTime;
  if ($.state.scanTimer < SCAN_INTERVAL) return;
  $.state.scanTimer = 0;

  const avatarID = (typeof DEFAULT_AVATAR_ID !== "undefined") ? DEFAULT_AVATAR_ID : null;
  if (!avatarID) return;

  const players = $.getPlayersNear($.getPosition(), Infinity);
  const current = $.state.assignments;
  const currentIds = {};

  // Assign avatars to new players
  for (let i = 0; i < players.length; i++) {
    const p = players[i];
    currentIds[p.id] = true;
    if (!current[p.id]) {
      assignAvatarToPlayer(p, avatarID, null);
    }
  }

  // Clean up assignments for players who left
  const toRemove = [];
  for (const pid in current) {
    if (!currentIds[pid]) {
      toRemove.push(pid);
    }
  }
  for (let i = 0; i < toRemove.length; i++) {
    try {
      current[toRemove[i]].handle.send("unassign", null);
    } catch (e) { /* item may already be gone */ }
    delete current[toRemove[i]];
  }
  if (toRemove.length > 0) {
    $.state.assignments = current;
  }
});

// ===== LUIDA Avatar Manager =====
// Runs on the LUIDA-AvatarSpawner item.
// Reads AVATAR_INDEX_MAP from the prepended constants header.
//
// Supports two input paths:
//   1. Direct messages: luida_assign_avatar / luida_unassign_avatar (from state-listening items)
//   2. Gimmick integer commands: polls global states "luida_avatar_cmd" + "luida_avatar_participant"

$.onStart(() => {
  $.state.createdAvatars = []; // flat list of created avatar item handles
  $.state.lastCmd = 0;         // last processed command value
});

// --- Resolve target to a PlayerHandle ---
// target can be: PlayerHandle (from direct callers) or integer (participant index, 0-based)
function resolvePlayer(target) {
  if (target === null || target === undefined) return null;
  if (typeof target === "number") {
    const participants = $.groupState.participants;
    if (!participants || target < 0 || target >= participants.length) {
      $.log("[AvatarManager] Invalid participant index: " + target);
      return null;
    }
    return participants[target];
  }
  return target;
}

// --- Core assignment logic ---
function assignAvatarToPlayer(player, avatarID, boneOffsets) {
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
    handle.send("assignPlayer", { player: player, boneOffsets: boneOffsets || null });

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

// --- Message handlers (for state-listening items) ---
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
      unassignAllFromPlayer(player);
    }
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
      const participantIndex = participantNumber - 1; // Convert 1-based to 0-based for resolvePlayer

      if (cmd > 0) {
        // Assign: cmd = avatarIndex + 1 (1-based)
        const avatarIndex = cmd - 1;
        const player = resolvePlayer(participantIndex);
        const avatarID = AVATAR_INDEX_MAP[avatarIndex];
        if (player && avatarID) {
          assignAvatarToPlayer(player, avatarID, null);
        }
      } else if (cmd === -1) {
        // Unassign all avatars from participant
        const player = resolvePlayer(participantIndex);
        if (player) {
          unassignAllFromPlayer(player);
        }
      }

      // Reset both global states via CCK GlobalLogic on the spawner
      $.sendSignalCompat("this", "luida_avatar_cmd_reset");
    }

    if (cmd === 0 && $.state.lastCmd !== 0) {
      $.state.lastCmd = 0;
    }
  } catch (e) {
    $.log("[AvatarManager] Gimmick trigger poll error: " + e);
  }
});

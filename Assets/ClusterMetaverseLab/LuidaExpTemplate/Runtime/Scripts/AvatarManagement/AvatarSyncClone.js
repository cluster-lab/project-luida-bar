// ===== LUIDA Avatar Sync Clone =====
// Attached to each spawned avatar wrapper item.
// Reads BONE_MAP and BONE_PARENT from the prepended per-avatar BoneMap header.
// Syncs the assigned player's humanoid pose to this item's sub-nodes every frame.

let boneNodes = [];
let hipsNode = null;
let headNode = null;

$.onStart(() => {
  $.state.player = null;
  $.state.needsScale = false;
  $.state.scaled = false;
  $.state.poseWaitTime = 0;
  $.state.warnedNoPose = false;

  // Cache bone sub-node references using the baked-in BONE_MAP
  boneNodes = [];
  for (let i = 0; i < BONE_MAP.length; i++) {
    const entry = BONE_MAP[i];
    const node = $.subNode(entry.name);
    if (!node) continue;
    const parentBone = BONE_PARENT[entry.bone] !== undefined ? BONE_PARENT[entry.bone] : null;
    boneNodes.push({ bone: entry.bone, node: node, parentBone: parentBone });
    if (entry.bone === HumanoidBone.Hips) hipsNode = node;
    if (entry.bone === HumanoidBone.Head) headNode = node;
  }
});

// --- Receive messages from AvatarManager ---
$.onReceive((messageType, arg, sender) => {
  if (messageType === "assignPlayer") {
    $.state.player = arg.player;
    $.requestOwner(arg.player);
  }

  if (messageType === "unassign") {
    $.state.player = null;
    $.destroy();
  }

  if (messageType === "unassignIfPlayer") {
    if ($.state.player && $.state.player.id === arg) {
      $.state.player = null;
      $.destroy();
    }
  }
});

// --- Quaternion math helpers ---
function rotateVector(q, x, y, z) {
  const qx = q.x, qy = q.y, qz = q.z, qw = q.w;
  const tx = 2 * (qy * z - qz * y);
  const ty = 2 * (qz * x - qx * z);
  const tz = 2 * (qx * y - qy * x);
  return new Vector3(
    x + qw * tx + (qy * tz - qz * ty),
    y + qw * ty + (qz * tx - qx * tz),
    z + qw * tz + (qx * ty - qy * tx)
  );
}

function multiplyQuaternions(q1, q2) {
  return new Quaternion(
    q1.w * q2.x + q1.x * q2.w + q1.y * q2.z - q1.z * q2.y,
    q1.w * q2.y - q1.x * q2.z + q1.y * q2.w + q1.z * q2.x,
    q1.w * q2.z + q1.x * q2.y - q1.y * q2.x + q1.z * q2.w,
    q1.w * q2.w - q1.x * q2.x - q1.y * q2.y - q1.z * q2.z
  );
}

// --- Per-frame sync ---
$.onUpdate((deltaTime) => {
  const player = $.state.player;
  if (!player || !player.exists()) return;

  // Diagnostic: if pose data is still null 3s after the wrapper got its player,
  // log once. Helps surface the rare "AvatarManager timed out and spawned the
  // wrapper anyway, but pose data never arrived" case to the experimenter.
  // Behavior unchanged — the existing `if (!boneRot) continue;` skip handles
  // the actual sync.
  const hipsRotProbe = player.getHumanoidBoneRotation(HumanoidBone.Hips);
  if (!hipsRotProbe) {
    $.state.poseWaitTime += deltaTime;
    if (!$.state.warnedNoPose && $.state.poseWaitTime > 3) {
      $.log("[AvatarSyncClone] Player pose still null after 3s — avatar may stay in T-pose");
      $.state.warnedNoPose = true;
    }
  } else if ($.state.warnedNoPose) {
    $.log("[AvatarSyncClone] Pose data now available; sync resuming");
    $.state.warnedNoPose = false;
  }

  // Sync root position and rotation from player
  const pos = player.getPosition();
  const rot = player.getRotation();
  if (pos) $.setPosition(pos);
  if (rot) $.setRotation(rot);

  // Sync Hips position (local space relative to root) — before scaling so offset affects head height
  if (hipsNode && pos && rot) {
    const hipsWorldPos = player.getHumanoidBonePosition(HumanoidBone.Hips);
    if (hipsWorldPos) {
      const dx = hipsWorldPos.x - pos.x;
      const dy = hipsWorldPos.y - pos.y;
      const dz = hipsWorldPos.z - pos.z;
      const invRot = new Quaternion(-rot.x, -rot.y, -rot.z, rot.w);
      const hipsLocalPos = rotateVector(invRot, dx, dy, dz);

      const finalX = hipsLocalPos.x;
      const finalZ = hipsLocalPos.z;

      // Sync hips Y when enabled, otherwise use the baked offset
      const finalY = AVATAR_SYNC_HIPS_Y ? hipsLocalPos.y : AVATAR_HIPS_Y_OFFSET;

      hipsNode.setPosition(new Vector3(finalX, finalY, finalZ));
    }
  }

  // --- Scaling: compare head heights after hips offset is applied ---
  // Frame 1: sync pose + hips, flag needsScale. Frame 2: compare head heights, apply scale.
  if (!$.state.scaled) {
    if ($.state.needsScale && headNode) {
      const playerHeadPos = player.getHumanoidBonePosition(HumanoidBone.Head);
      const avatarHeadPos = headNode.getGlobalPosition();
      if (playerHeadPos && avatarHeadPos && pos) {
        const playerHeadH = playerHeadPos.y - pos.y;
        const avatarHeadH = avatarHeadPos.y - pos.y;
        if (avatarHeadH > 0.01 && playerHeadH > 0.01) {
          const s = playerHeadH / avatarHeadH;
          const transform = $.getUnityComponent("Transform");
          transform.unityProp.localScale = new Vector3(s, s, s);
          $.state.scaled = true;
        }
      }
    } else {
      $.state.needsScale = true;
    }
  }

  // Sync all bone rotations (world -> local via inverse parent)
  const worldRots = {};
  for (let i = 0; i < boneNodes.length; i++) {
    const entry = boneNodes[i];
    const boneRot = player.getHumanoidBoneRotation(entry.bone);
    if (!boneRot) continue;
    worldRots[entry.bone] = boneRot;

    if (!entry.node) continue;

    // Determine parent world rotation
    let parentWorldRot;
    if (entry.parentBone === null) {
      parentWorldRot = rot;
    } else {
      parentWorldRot = worldRots[entry.parentBone];
      if (!parentWorldRot) parentWorldRot = rot;
    }

    // Convert world rotation to local: localRot = inverse(parentWorldRot) * boneRot
    const invParent = new Quaternion(-parentWorldRot.x, -parentWorldRot.y, -parentWorldRot.z, parentWorldRot.w);
    const localBoneRot = multiplyQuaternions(invParent, boneRot);

    entry.node.setRotation(localBoneRot);
  }
});

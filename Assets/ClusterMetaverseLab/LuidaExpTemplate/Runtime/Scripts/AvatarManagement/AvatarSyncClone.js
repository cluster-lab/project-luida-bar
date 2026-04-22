// ===== LUIDA Avatar Sync Clone =====
// Attached to each spawned avatar wrapper item.
// Reads BONE_MAP and BONE_PARENT from the prepended per-avatar BoneMap header.
// Syncs the assigned player's humanoid pose to this item's sub-nodes every frame.

let boneNodes = [];
let hipsNode = null;

$.onStart(() => {
  $.state.player = null;
  $.state.owned = false;
  $.state.scaled = false;
  $.state.boneOffsets = null; // { [boneEnum]: { pos: {x,y,z}, rot: {x,y,z} } }

  // Cache bone sub-node references using the baked-in BONE_MAP
  boneNodes = [];
  for (let i = 0; i < BONE_MAP.length; i++) {
    const entry = BONE_MAP[i];
    const node = $.subNode(entry.name);
    if (!node) continue;
    const parentBone = BONE_PARENT[entry.bone] !== undefined ? BONE_PARENT[entry.bone] : null;
    boneNodes.push({ bone: entry.bone, node: node, parentBone: parentBone });
    if (entry.bone === HumanoidBone.Hips) hipsNode = node;
  }
});

// --- Receive messages from AvatarManager ---
$.onReceive((messageType, arg, sender) => {
  if (messageType === "assignPlayer") {
    $.state.player = arg.player;
    $.state.boneOffsets = arg.boneOffsets || null;
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

function eulerToQuaternion(ex, ey, ez) {
  const cx = Math.cos(ex * 0.5 * Math.PI / 180);
  const sx = Math.sin(ex * 0.5 * Math.PI / 180);
  const cy = Math.cos(ey * 0.5 * Math.PI / 180);
  const sy = Math.sin(ey * 0.5 * Math.PI / 180);
  const cz = Math.cos(ez * 0.5 * Math.PI / 180);
  const sz = Math.sin(ez * 0.5 * Math.PI / 180);
  return new Quaternion(
    sx * cy * cz - cx * sy * sz,
    cx * sy * cz + sx * cy * sz,
    cx * cy * sz - sx * sy * cz,
    cx * cy * cz + sx * sy * sz
  );
}

// --- Per-frame sync ---
$.onUpdate((deltaTime) => {
  const player = $.state.player;
  if (!player || !player.exists()) return;

  // Sync root position and rotation from player
  const pos = player.getPosition();
  const rot = player.getRotation();
  if (pos) $.setPosition(pos);
  if (rot) $.setRotation(rot);

  // Scale once after ownership is confirmed (pose-independent)
  if (!$.state.scaled) {
    const owner = $.getOwner();
    if (owner && owner.id === player.id) {
      const heightChain = [
        HumanoidBone.LeftFoot,
        HumanoidBone.LeftLowerLeg,
        HumanoidBone.LeftUpperLeg,
        HumanoidBone.Hips,
        HumanoidBone.Spine,
        HumanoidBone.Chest,
        HumanoidBone.Neck,
        HumanoidBone.Head,
      ];
      let playerHeight = 0;
      let validSegments = 0;
      for (let ci = 1; ci < heightChain.length; ci++) {
        const posA = player.getHumanoidBonePosition(heightChain[ci - 1]);
        const posB = player.getHumanoidBonePosition(heightChain[ci]);
        if (posA && posB) {
          const dx = posB.x - posA.x, dy = posB.y - posA.y, dz = posB.z - posA.z;
          playerHeight += Math.sqrt(dx * dx + dy * dy + dz * dz);
          validSegments++;
        }
      }
      if (validSegments >= 3 && typeof AVATAR_SKELETON_HEIGHT !== "undefined" && AVATAR_SKELETON_HEIGHT > 0) {
        const s = playerHeight / AVATAR_SKELETON_HEIGHT;
        const transform = $.getUnityComponent("Transform");
        transform.unityProp.localScale = new Vector3(s, s, s);
      }
      $.state.scaled = true;
    }
  }

  // Sync Hips position (local space relative to root)
  if (hipsNode && pos && rot) {
    const hipsWorldPos = player.getHumanoidBonePosition(HumanoidBone.Hips);
    if (hipsWorldPos) {
      const dx = hipsWorldPos.x - pos.x;
      const dy = hipsWorldPos.y - pos.y;
      const dz = hipsWorldPos.z - pos.z;
      const invRot = new Quaternion(-rot.x, -rot.y, -rot.z, rot.w);
      const hipsLocalPos = rotateVector(invRot, dx, dy, dz);

      // Apply position offset for Hips if configured
      let finalX = hipsLocalPos.x;
      let finalY = hipsLocalPos.y;
      let finalZ = hipsLocalPos.z;
      const offsets = $.state.boneOffsets;
      if (offsets && offsets[HumanoidBone.Hips] && offsets[HumanoidBone.Hips].pos) {
        const po = offsets[HumanoidBone.Hips].pos;
        finalX += (po.x || 0);
        finalY += (po.y || 0);
        finalZ += (po.z || 0);
      }
      hipsNode.setPosition(new Vector3(finalX, finalY, finalZ));
    }
  }

  // Sync all bone rotations (world → local via inverse parent)
  const worldRots = {};
  const offsets = $.state.boneOffsets;
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
    let localBoneRot = multiplyQuaternions(invParent, boneRot);

    // Apply rotation offset if configured for this bone
    if (offsets && offsets[entry.bone] && offsets[entry.bone].rot) {
      const ro = offsets[entry.bone].rot;
      const offsetQuat = eulerToQuaternion(ro.x || 0, ro.y || 0, ro.z || 0);
      localBoneRot = multiplyQuaternions(localBoneRot, offsetQuat);
    }

    entry.node.setRotation(localBoneRot);
  }
});

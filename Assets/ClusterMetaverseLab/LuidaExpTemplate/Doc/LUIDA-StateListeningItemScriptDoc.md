# Documentation for LUIDA state-listening items' scripts
Scripts for LUIDA state-listening items are primarily to be written using ClusterScript (https://docs.cluster.mu/script/index.html).

Additionally, we provide the following variables and functions that work specifically with LUIDA-only features or accelerate your implementation.

Tip: When asking an LLM service for coding assistance, first share the `Asset/Doc/CCK-Types.d.ts` file and this file with it.

## Two kinds of helpers

This doc covers two distinct categories of LUIDA helpers; they are NOT interchangeable:

1. **Callable JS helpers** (sections "Available Variables" → "User Feedback & Utilities" below). These are real top-level functions defined in `Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/StateManagement/StateListeningItemBase.js`. You can call them from any Customized Action JS body — e.g. `ShowItem();`, `SendDataToCollector("bridge", "safe");`.

2. **Editor-only actions** (section "Editor-only Actions" below). These appear as choices in the Action dropdown of the State-listening Items grid, but they are NOT defined as JS functions. They are either runtime row types (`Sleep`) or predefined action templates that expand to a specific JS call when added as a row (`Assign avatar to participant`, `Unassign avatar from participant`, `Sync with participant bone`). Calling them by name inside a Customized Action body throws `ReferenceError`. To use them from inline JS, copy the equivalent JS shown in each section.

## Available Variables

### `CONDITION`

- Only available if **LUIDA experiment progress automation feature is enabled and during the trial states** (e.g., `Trial - Start`, `Trial - Rest`).
- Contains values from your configured experimental variables for the current trial.
- Use `CONDITION["your_variable_name"]` to retrieve a specific condition value within the current trial.

### `PARTICIPANTS`

- An array of PlayerHandle of the participants joining this experiment."
- Use `PARTICIPANTS[1]` to retrieve the first participant, `PARTICIPANTS[2]` to retrieve the second participant, etc.

---

## Available Functions

## State Machine Control

### `ToNextState()`

- **Description**: Triggers a transition to the next experiment state.
- **Parameters**: None

---

## Item Visibility

### `ShowItem()`

- **Description**: Makes the item visible.
- **Parameters**: None

### `HideItem()`

- **Description**: Makes the item invisible.
- **Parameters**: None

---

## Child Visibility

### `ShowChild(childName)`

- **Description**: Makes a specified child object visible.
- **Parameters**:
  - `childName`: `string`

### `HideChild(childName)`

- **Description**: Makes a specified child object invisible.
- **Parameters**:
  - `childName`: `string`

---

## Item Manipulation

### `SetText(text)`

- **Description**: Sets text on a child 'Text' sub-node.
- **Parameters**: `text`: `string`

### `SetPosition(x, y, z)`

- **Description**: Sets the item's world position. **Requires the `MovableItem` component on this item.**
- **Parameters**:
  - `x`: `number`
  - `y`: `number`
  - `z`: `number`

### `AddPosition(x, y, z)`

- **Description**: Offsets the item's world position. **Requires the `MovableItem` component on this item.**
- **Parameters**:
  - `x`: `number`
  - `y`: `number`
  - `z`: `number`

### `SetRotation(x, y, z)`

- **Description**: Sets the item's world rotation using Euler degrees. **Requires the `MovableItem` component on this item.**
- **Parameters**:
  - `x`: `number`
  - `y`: `number`
  - `z`: `number`

### `AddRotation(x, y, z)`

- **Description**: Adds to the item's world rotation using Euler degrees. **Requires the `MovableItem` component on this item.**
- **Parameters**:
  - `x`: `number`
  - `y`: `number`
  - `z`: `number`

---

## Child Manipulation

### `SetChildPosition(childName, x, y, z)`

- **Description**: Sets the local position of a specified child object.
- **Parameters**:
  - `childName`: `string`
  - `x`: `number`
  - `y`: `number`
  - `z`: `number`

### `AddChildPosition(childName, x, y, z)`

- **Description**: Offsets the local position of a specified child object.
- **Parameters**:
  - `childName`: `string`
  - `x`: `number`
  - `y`: `number`
  - `z`: `number`

### `SetChildRotation(childName, x, y, z)`

- **Description**: Sets the local rotation of a specified child object using Euler degrees.
- **Parameters**:
  - `childName`: `string`
  - `x`: `number`
  - `y`: `number`
  - `z`: `number`

### `AddChildRotation(childName, x, y, z)`

- **Description**: Adds to the local rotation of a specified child object using Euler degrees.
- **Parameters**:
  - `childName`: `string`
  - `x`: `number`
  - `y`: `number`
  - `z`: `number`

---

## Data Logging

These helpers correspond to the predefined editor actions **Push data to collector** (action type `Send data to collector`), **Save pushed data in collector** (action type `Process and save collected data`), and **Upload saved data from collector** (action type `Upload collected data`) respectively. The callable JS helpers below remain valid from inside a Customized Action body.

### `SendDataToCollector(label, value)`

- **Description**: Pushes a single key/value pair onto LUIDA's in-memory Data Collector scratchpad (`$.groupState.collectedData[label] = value`). Does not flush. Equivalent editor action: **Push data to collector**.
- **Parameters**:
  - `label`: `string`
  - `value`: `any`

### `ProcessAndSaveCollectedData()`

- **Description**: Snapshots the current scratchpad into the upload buffer as one row, then clears the scratchpad. Does not upload. Equivalent editor action: **Save pushed data in collector**.
- **Parameters**: None

### `UploadCollectedData()`

- **Description**: Flushes the upload buffer to the Web Console via `exp_uploadCustomData`. Equivalent editor action: **Upload saved data from collector**.
- **Parameters**: None

---

## User Feedback & Utilities

### `SendHaptics(participantId, target, frequency, amplitude, duration)`

- **Description**: Sends haptic feedback to the player specified by `participantId`. `target` should be filled in with "left", "right", or null for both hands. `duration` is in seconds.
- **Parameters**:
  - `participantId`: `integer` (start from 1)
  - `target`: `string`
  - `frequency`: `number`
  - `amplitude`: `number`
  - `duration`: `number`

### `SendViaOsc(participantId, address, values)`

-   **Description**: Sends an OSC message from the client of the player specified by `participantId`. This is typically used to control external hardware or software outside Cluster.
-   **Parameters**:
    -   `participantId`: `integer` (starts from 1).
    -   `address`: `string`
        -   The OSC path, which must begin with `/` (e.g., `/sample`).
    -   `values`: `array`
        -   A list of arguments to send with the message. Each argument should be one of the following types: `Boolean`, `Number`, `String`.

---

# Editor-only Actions

The items below are NOT callable JS functions. They are choices in the Action dropdown of the State-listening Items grid. To use the same behavior from inline JS inside a Customized Action body, copy the "Equivalent JS" snippet shown in each section.

## Delay

### `Sleep` (editor action)

- **Description**: Pauses the chain of action rows in the current hook for the specified duration before the next row runs. Implemented by the state-listener runtime (`StateListeningItemBase.js:185-199`), which scans rows sequentially and accumulates `deltaTime` on a `Sleep` row until it reaches the configured seconds. ClusterScript has no equivalent timer / `setTimeout` API.
- **Fields**:
  - `seconds`: `number`
- **Equivalent JS**: None. There is no inline-JS equivalent — if you need a delay inside a single Customized Action body, split the body across two `Customized Action` rows separated by a `Sleep` row.

---

## Avatar Management

These actions send messages to the `LUIDA-AvatarSpawner` world item (a `WorldItemReference` that must exist in the scene and carry `AvatarManager.js`). The spawner resolves the participant number to a `PlayerHandle` via `$.groupState.participants` and then creates/destroys the avatar wrapper item for that player. If the participant has not yet been enrolled (i.e., `PARTICIPANTS[participantIndex]` would be `undefined`), the action is logged and silently ignored.

Prerequisite: avatars are registered in the LUIDA Web Console; their IDs are synced into this project via the **LUIDA > Configure experiment automation > Avatars** tab and stored in the `AvatarRegistry` asset. The editor shows the registered IDs as a dropdown.

### `Assign avatar to participant` (editor action)

- **Description**: Assigns the avatar identified by `avatarID` to the specified participant. Any avatar previously assigned to that participant is unassigned first. The avatar wrapper item is spawned at the participant's current position and continuously syncs its pose to the player (see `AvatarSyncClone.js`). Safe to call repeatedly — re-assigning the same avatar simply replaces the wrapper.
- **Fields**:
  - `avatarID`: `string` — must match an entry in the project's `AvatarRegistry`.
  - `participantIndex`: `integer` (starts from 1)
- **Equivalent JS** (for use inside a Customized Action body):
  ```javascript
  $.worldItemReference('LUIDA-AvatarSpawner').send('luida_assign_avatar', { avatarID: 'your_avatar_id', participantIndex: 1 });
  ```

### `Unassign avatar from participant` (editor action)

- **Description**: Removes **all** avatar wrapper items currently assigned to the specified participant, restoring their default Cluster avatar.
- **Fields**:
  - `participantIndex`: `integer` (starts from 1)
- **Equivalent JS**:
  ```javascript
  $.worldItemReference('LUIDA-AvatarSpawner').send('luida_unassign_avatar', { participantIndex: 1 });
  ```

---

## Participant Transform

These actions manipulate the participant's player position and rotation via `PlayerHandle.setPosition` / `setRotation` (documented in `Asset/Doc/CCK-Types.d.ts:2438, 2457`). Rate-limited to 10 calls/sec per item. The Set variants overwrite; the Add variants compose with the participant's current position/rotation (and silently no-op if the participant handle isn't available yet).

### `Set participant position` (editor action)

- **Description**: Teleports the participant to a global-space `Vector3`.
- **Fields**:
  - `participantIndex`: `integer` (starts from 1)
  - `x`, `y`, `z`: `number` — destination in world coordinates
- **Equivalent JS**:
  ```javascript
  PARTICIPANTS[1].setPosition(new Vector3(0, 0, 0));
  ```

### `Add participant position` (editor action)

- **Description**: Offsets the participant from their current position by a global-space `Vector3`. Silently no-ops if `PARTICIPANTS[i].getPosition()` returns null (participant not present or pose not yet streamed).
- **Fields**:
  - `participantIndex`: `integer`
  - `x`, `y`, `z`: `number` — world-space offset in meters
- **Equivalent JS**:
  ```javascript
  (() => {
    var p = PARTICIPANTS[1] && PARTICIPANTS[1].getPosition();
    if (p) PARTICIPANTS[1].setPosition(p.add(new Vector3(0, 0, 0)));
  })();
  ```

### `Set participant rotation` (editor action)

- **Description**: Sets the participant's facing direction to a given Euler rotation (degrees). Note: `PlayerHandle.setRotation` ignores anything but Y-axis yaw — the body stays vertical.
- **Fields**:
  - `participantIndex`: `integer`
  - `x`, `y`, `z`: `number` — Euler angles in degrees (only `y` has effect)
- **Equivalent JS**:
  ```javascript
  PARTICIPANTS[1].setRotation(new Quaternion().setFromEulerAngles(new Vector3(0, 0, 0)));
  ```

### `Add participant rotation` (editor action)

- **Description**: Rotates the participant by a given Euler delta (degrees), composed with their current rotation. Silently no-ops if `getRotation()` is unavailable.
- **Fields**:
  - `participantIndex`: `integer`
  - `x`, `y`, `z`: `number` — Euler delta in degrees (only `y` has effect)
- **Equivalent JS**:
  ```javascript
  (() => {
    var r = PARTICIPANTS[1] && PARTICIPANTS[1].getRotation();
    if (r) PARTICIPANTS[1].setRotation(r.multiply(new Quaternion().setFromEulerAngles(new Vector3(0, 0, 0))));
  })();
  ```

---

## Bone Following

### `Sync with participant bone` (editor action)

- **Description**: Reads the world (global) position and rotation of the specified bone on the specified participant's avatar via `PlayerHandle.getHumanoidBonePosition` and `PlayerHandle.getHumanoidBoneRotation`, then applies them to this item with the configured world-space offsets. Position offset is added to the bone's world position; rotation offset (Euler degrees) is pre-multiplied with the bone's world rotation. Typically placed under **During State** so the item follows the bone every frame, but also usable as a one-shot snapshot under **On State Start** / **On State Exit**. Silently no-ops if the participant is not present or the bone is unavailable on the avatar. **Requires the `MovableItem` component on this item.**
- **Fields**:
  - `participantIndex`: `integer` (starts from 1)
  - `bone`: `string` — one of the `HumanoidBone` enum names (e.g. `Head`, `RightHand`, `Hips`). See `Asset/Doc/CCK-Types.d.ts` for the full list.
  - `posOffset`: `(x, y, z)` world-space offset in meters added to the bone position.
  - `rotOffset`: `(x, y, z)` world-space Euler offset in degrees, pre-multiplied with the bone rotation.
- **Equivalent JS** (the IIFE template the editor action emits — copy and edit constants):
  ```javascript
  (() => {
    try {
      const player = PARTICIPANTS[1];
      if (!player || !player.exists()) return;
      const bone = HumanoidBone.RightHand;
      const bonePosWorld = player.getHumanoidBonePosition(bone);
      const boneRotWorld = player.getHumanoidBoneRotation(bone);
      const posOffset = new Vector3(0, 0, 0);
      const rotOffset = new Quaternion().setFromEulerAngles(new Vector3(0, 0, 0));
      if (bonePosWorld) $.setPosition(bonePosWorld.add(posOffset));
      if (boneRotWorld) $.setRotation(rotOffset.multiply(boneRotWorld));
    } catch (e) {
      $.log('[SyncWithParticipantBone] ' + e + '. Ensure MovableItem is on this item and bone name is valid.');
    }
  })();
  ```

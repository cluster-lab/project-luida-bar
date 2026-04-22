# Documentation for LUIDA state-listening items' scripts
Scripts for LUIDA state-listening items are primarily to be written using ClusterScript (https://docs.cluster.mu/script/index.html).

Additionally, we provide the following variables and functions that work specifically with LUIDA-only features or accelerate your implementation.

Tip: When asking an LLM service for coding assistance, first share the `Asset/Doc/CCK-Types.d.ts` file and this file with it.

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

### `SyncWithParticipantBone(participantIndex, bone, posOffset, rotOffset)`

- **Description**: Reads the world (global) position and rotation of the specified bone on the specified participant's avatar via `PlayerHandle.getHumanoidBonePosition` and `PlayerHandle.getHumanoidBoneRotation`, then applies them to this item with the configured world-space offsets. Position offset is added to the bone's world position; rotation offset (Euler degrees) is pre-multiplied with the bone's world rotation. Typically placed under **During State** so the item follows the bone every frame, but also usable as a one-shot snapshot under **On State Start** / **On State Exit**. Silently no-ops if the participant is not present or the bone is unavailable on the avatar. **Requires the `MovableItem` component on this item.**
- **Parameters**:
  - `participantIndex`: `integer` (starts from 1)
  - `bone`: `string` — one of the `HumanoidBone` enum names (e.g. `Head`, `RightHand`, `Hips`). See `Asset/Doc/CCK-Types.d.ts` for the full list.
  - `posOffset`: `(x, y, z)` world-space offset in meters added to the bone position.
  - `rotOffset`: `(x, y, z)` world-space Euler offset in degrees, pre-multiplied with the bone rotation.

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

### `SendDataToCollector(label, value)`

- **Description**: Sends data to LUIDA's Data Collector.
- **Parameters**:
  - `label`: `string`
  - `value`: `any`

### `ProcessAndSaveCollectedData()`

- **Description**: Signals LUIDA's Data Collector to process and save the collected data.
- **Parameters**: None

### `UploadCollectedData()`

- **Description**: Signals LUIDA's Data Collector to upload the saved data collection.
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

### `Sleep(seconds)`

- **Description**: Pauses the execution of subsequent actions in the current list for a specified duration. **Note**: This has no direct ClusterScript function equivalent.
- **Parameters**:
  - `seconds`: `number`

---

## Avatar Management

These actions send messages to the `LUIDA-AvatarSpawner` world item (a `WorldItemReference` that must exist in the scene and carry `AvatarManager.js`). The spawner resolves the participant number to a `PlayerHandle` via `$.groupState.participants` and then creates/destroys the avatar wrapper item for that player. If the participant has not yet been enrolled (i.e., `PARTICIPANTS[participantIndex]` would be `undefined`), the action is logged and silently ignored.

Prerequisite: avatars are registered in the LUIDA Web Console; their IDs are synced into this project via the **LUIDA > Configure experiment automation > Avatars** tab and stored in the `AvatarRegistry` asset. The editor shows the registered IDs as a dropdown.

### `AssignAvatarToParticipant(avatarID, participantIndex)`

- **Description**: Assigns the avatar identified by `avatarID` to the specified participant. Any avatar previously assigned to that participant is unassigned first. The avatar wrapper item is spawned at the participant's current position and continuously syncs its pose to the player (see `AvatarSyncClone.js`). Safe to call repeatedly — re-assigning the same avatar simply replaces the wrapper.
- **Parameters**:
  - `avatarID`: `string` — must match an entry in the project's `AvatarRegistry`.
  - `participantIndex`: `integer` (starts from 1)

### `UnassignAvatarFromParticipant(participantIndex)`

- **Description**: Removes **all** avatar wrapper items currently assigned to the specified participant, restoring their default Cluster avatar.
- **Parameters**:
  - `participantIndex`: `integer` (starts from 1)

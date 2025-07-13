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
- Use `PARTICIPANTS[0]` to retrieve the first participant, `PARTICIPANTS[1]` to retrieve the second participant, etc.

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

### `SendHaptics(target, frequency, amplitude, duration)`

- **Description**: Sends haptic feedback to the player. The target can be "left", "right", or null for both hands. The duration is in seconds.
- **Parameters**:
  - `target`: `string`
  - `frequency`: `number`
  - `amplitude`: `number`
  - `duration`: `number`

### `Sleep(seconds)`

- **Description**: Pauses the execution of subsequent actions in the current list for a specified duration. **Note**: This has no direct ClusterScript function equivalent.
- **Parameters**:
  - `seconds`: `number`

# Tutorial

Let's implement an experiment of hand redirection in this tutorial.

## Recommended Preliminary Knowledge

We recommend you to at least acquire some basic knowledge of the following:
- Unity
- JavaScript
- [Cluster Creator Kit (CCK)](https://docs.cluster.mu/creatorkit/)

## Preparation

1. [Create a cluster account](https://help.cluster.mu/hc/articles/115000827112)
2. [Install the required version of Unity for Cluster](https://docs.cluster.mu/creatorkit/installation/install-unity/).
3. Clone this implementation template.

## Register Experiment Information on the Web Console

1. Open the web console: https://cluster-lab.github.io/project-luida-web-console/ (If you do not have a GitHub account and cannot open the web console, please create a GitHub account first.)
  
2. Register and log in.

3. Register the experiment recruitment information:
    1. Click the "Register New Experiment" link to register a new experiment recruitment.
    2. Fill in the fields shown in the image below (for this tutorial, any string will do). Leave the other fields blank or with default values.
<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/3ccc2bb5-cb02-495e-b876-da06d21b4b14" alt="Image 1" width="500"/></td>
    <td><img src="https://github.com/user-attachments/assets/f75c5f36-3030-4f72-bbf4-1259d42dac09" alt="Image 2" width="500"/></td>
  </tr>
</table>

4. After pressing the Register button, the information will be saved and displayed on the next screen. Confirm the automatically generated experiment identifier (eID).

![image](https://github.com/user-attachments/assets/758a26d4-0fda-47d4-8528-cb98f3f2f2c7)

5. Register the questionnaire:
    1. Press the "Questionnaires" button to move to the questionnaire list screen.
    2. Click the "Register New Questionnaire" button to move to the registration screen for the questionnaire to be used in the experiment.
        ![image](https://github.com/user-attachments/assets/a21f6e34-a220-4d7f-8956-374d96ad3cb2)
    3. Enter the content of the questionnaire:
        1. Register only the questions "AC1 My Body" and "CO1 My Action" from the [Virtual Embodiment Questionnaire](https://sites.google.com/view/virtualembodimentquestionnaire/download-the-questionnaire).
        2. For each question, press the "Add Question" button to increase the question fields and fill them in according to the diagram.
        3. Once all questions are entered, press the "Register" button to complete the registration.
          ![image](https://github.com/user-attachments/assets/ab4d5a9d-8b26-4adf-a0c1-c5e31d19ac70)
    4. Confirm the questionnaire: After the screen transitions, check the registered questionnaire.

![image](https://github.com/user-attachments/assets/3f6dd78e-781c-4406-9d67-3c8b5cc0c258)

## Open the Implementation Template

Launch the downloaded implementation template from Unity Hub.

## Link with Your Cluster Account

1. From the top menu, select `Cluster > External Communication (callExternal) connection URL`.
![image](https://github.com/user-attachments/assets/9ac1311a-aa09-4e28-95a0-a2081e9883f4)
2. Click the "Generate Token" button on the web to open the token generation screen in the browser. Click the "Create Token" button there and copy the displayed token.
<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/2c0d3212-e301-417d-a594-8ed1f7dec08f" alt="Image 1" width="400"/></td>
    <td><img src="https://github.com/user-attachments/assets/4395e140-b709-48ee-b659-52e6a93009fa" alt="Image 2" width="400"/></td>
    <td><img src="https://github.com/user-attachments/assets/2b5ca13b-14cf-4c77-ae06-ed7ca7792705" alt="Image 2" width="400"/></td>
  </tr>
</table>

3. Return to Unity, paste the token, and press the "Use This Token" button.

![image](https://github.com/user-attachments/assets/df24aab2-684f-4f5b-b287-b451f3c65f6d)

## Retrieve Verify Token for External Call

1. In the screen that appears after linking the account, paste the following URL in "Register URL": `https://script.google.com/macros/s/AKfycbyamdYZGjweG65Dkykdw1oT7MxU4ZXoeqPDT3csW1M2mS3jj8gq9kZzO2iKhSBUOfx0Zg/exec`
2. Copy the displayed "verify token" and save it somewhere.

![image](https://github.com/user-attachments/assets/e780e28d-4427-426d-9dc6-fde4d12b6120)
![image](https://github.com/user-attachments/assets/8c5bea04-d868-4f8c-a664-ae8ae3abcf52)

## Register Experiment ID and Verify Token for External Call

1. From the top menu, open `Window > Luida Editor`.
![image](https://github.com/user-attachments/assets/ff78908a-2277-4a07-a37f-3a1502146343)
2. Input a name for your new scene and click the 'Create and Open Scene' button.
![image](https://github.com/user-attachments/assets/21b1f40a-4367-4448-8277-244593682525)
3. In the `Experiment Identifiers` tab, fill in `Experiment ID` with the `eID` displayed on the web console.
4. In the `Experiment Identifiers` tab, fill in `Token` with the verify token for external call you just copied.
![image](https://github.com/user-attachments/assets/e7229049-3d8d-4cee-a5d5-19f5f70a2168)

## Setup Experiment Variables & Trials Count

1. Open `Window > Luida Editor` and switch to the `Experiment Variables Editor` tab.
2. Fill in the fields following the image below:
    1. Name: `gain`
    2. Values (comma-separated): `0.75,0.8,0.85,0.9,0.95,1,1.05,1.1,1.15,1.2,1.25`
![image](https://github.com/user-attachments/assets/15fde214-9fc0-4e27-9d3c-aaab31a46863)
3. Click the 'Apply Updated Variables' button to save the changes.
4. Save the scene.

## Adjust State Transitions

Open `Window > Luida Editor` and switch to the `State List Editor` tab.

The state transitions in this tutorial will be as follows: 

```text
Start → Instruction → Practice - Task → Practice - Questionnaire → Practice - Rest → Preparation → Trial - Task → Trial - Questionnaire → Trial - Rest → AfterTrials → Questionnaire (post-exp) → End
```


Among them,
- `Practice - Task → Practice - Questionnaire → Practice - Rest` repeats 3 times.
- `Trial - Task → Trial - Questionnaire → Trial - Rest` repeats `number of variable 'gain' values ×2` times.

Edit the transition in the editor window:

- Start: Set `Transit destination state` to `Instruction`.
- Acclimatization: Press the `Remove` button to remove it.
- Questionnaire (pre-exp): Press the `Remove` button to remove it.
- Practice - Rest:
    - Check `Has Exit Time`, and set `Exit Time` to `5` (to automatically trigger state transition after 5 seconds).
    - Check `Is Repeated`, set `Repeat destination state` to `Practice - Task`, and set `Repeat Count` to `3` (to repeat the practice session 3 times).
- Trial - Rest:
    - Check `Has Exit Time`, and set `Exit Time` to `5` (to automatically trigger state transition after 5 seconds).
    - (The repetition for `Trial` states is controlled by the variables and trials count we set up in the `Experiment Variables Editor` tab, so we don't need to make additional setups here for the repetition).

![image](https://github.com/user-attachments/assets/972b97af-3b00-42d8-b0d5-82039d5d39dd)

## Prefabs for the Tutorial

Take a look at the prefabs under `Assets/_Experiment_/Prefabs/Sample_HR/`. These are for the sample scene `Sample_HR`, and we will use them in this tutorial as well.

### NextStateButton
A button to trigger transitioning to the next state when clicked.

It is attached with a CCK component `Interact Item Trigger`. According to its value, when this button is clicked, it broadcasts a Signal with the key `state_triggerTransition` globally. This implementation template listens to this Signal and triggers a transition to the next state.

![image](https://github.com/user-attachments/assets/2ec1b241-9ee3-4b62-8c00-95dc82788118)

### Message

Simply a large message panel. You can use it to give participants instructions. Edit its child gameObject's `TextView` component to change its content.
![image](https://github.com/user-attachments/assets/b3401797-2c57-4036-971c-fa0bfce4cef5)

### RightHand

Move into the `RightHand` folder to find the `RightHand` prefab. A small sphere collider is attached to the tip of the index finger.

![image](https://github.com/user-attachments/assets/eb3d93d2-2ac9-4811-a503-57d43d81271e)

## Add Objects for Each State

To add gameobjects for each state, open `Window > Luida Editor` and switch to the `Objects Manager` tab.

Here is a video to show how to add a gameobject (not scriptable) from a prefab for a specific state:

https://github.com/user-attachments/assets/06f8656e-7f9d-42a9-8830-b6586e9481ca

Let's add gameobjects for each state according to the following specifications:

| Step                   | Action Description                                                                                                                               |
|------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| **Start**              | NextStateButton × 1 (Change text to `Start`)                                                                                                  |
| **Instruction**        | NextStateButton × 1 (Change text to `Practice`), Message × 1 (Change instruction text for practice session) <br><details> <summary>Instruction text example</summary> <pre>From now on, you will perform the task of touching the green ball with your right index finger and answering questions. First, let's practice a few times. When you are ready, please hide the controller in the settings screen, look ahead, and press the "Practice" button.</pre></details>  |
| **Practice - Task**    | To be explained later.                                                                                                                          |
| **Practice - Questionnaire** | Remove the Questionnaire object. Other required changes will be explained later.                                                                                                                          |
| **Practice - Rest**    | To be explained later.                                                                                                                          |
| **Preparation**        | NextStateButton × 1 (Change text to `Start`), Message × 1 (Change instruction text for trial session) <br><details> <summary>Instruction text example</summary> <pre>The practice session (3 times) is now complete. From here, it is the real thing. Please perform the task 22 times following the same procedure. When you are ready, look ahead and press the start button.</pre></details>  |
| **Trial - Task**       | To be explained later.                                                                                                                          |
| **Trial - Questionnaire** | Remove the Questionnaire object. Other required changes will be explained later.                                                                                                                          |
| **Trial - Rest**       | To be explained later.                                                                                                                          |
| **AfterTrials**        | To be explained later.                                                                                                                          |
| **Questionnaire (post-exp)** | To be explained later.                                                                                                                  |
| **End**                | Message × 1 (Change instruction text for experiment completion) <br><details> <summary>Instruction text example</summary> <pre>The experiment is now complete. Thank you for participating! The cluster points as a reward will be granted on a later date. Please pass through the gate in front of you to exit.</pre></details> <br> World gate prefab (`Assets/ClusterGAMEWORLDCENTER/Prefabs/WorldGateToClusterLobby.prefab`) × 1. After adding: <br> a. Set position to (0, 0, 1.5). <br> b. Delete child object `SignBoard`. <br> c. Set the value of child object `WorldGate`'s `World Or Event Id` field to `006d765e-f961-435b-a183-77c35a42e241` (World ID of LUIDA recruitment world). |

![image](https://github.com/user-attachments/assets/6cc12262-b05f-468d-ab5d-f266be6bde95)

## Implement Trials session

Task Content:
1. **Trial Start**: The green sphere returns to the origin (30 cm below the head + 30 cm in front).
2. **Reset**: The experiment participant touches the green sphere at the origin. The green sphere then moves to the target location (30 cm below the head + 60 cm in front).
3. **Reaching Task During Redirection**: The participant stretches their arm to touch the sphere again. While reaching, the virtual hand is being redirected (the hand's position is affected by a gain, deviating from the actual hand's position).
4. **Post-Trial (Display Questions)**: A question asking "Is the virtual hand faster or slower than the actual hand?" and two answer buttons "Faster" and "Slower" are displayed.
5. **Next Trial**: After the participant selects one of the buttons, the next trial transitions after a brief pause.

This experiment requires changing the gain on the virtual hand for each trial, so we need objects that can access the values of the experiment condition `gain`. The following video shows how to create such condition-dependent objects.

https://github.com/user-attachments/assets/c58aa9c0-7562-40cb-952a-6c3b767f099d

Follow the steps below to implement the trials session:

### Create a Task Manager Object

1. Refer to the video above to create a condition-dependent object named `TaskManager` in the state `Trial - Task`. Confirm that the gameobject and a cluster script asset `TaskManager.js` are generated.
2. Edit `TaskManager.js` as follows.

    <details>
    
    <summary>Replace the code in the `init` function</summary>
    
    ```javascript
    $.state.timer = 0; // Initialize the timer
    $.state.isTouchable = true; // Flag to allow touching the green ball
    $.state.handOffset = new Quaternion().setFromEulerAngles(new Vector3(0, 90, 0)); // Set the offset to correct the rotation difference between the physical hand (controller) and the virtual hand
    ```
    
    </details>
    
    <details>
    
    <summary>Replace the code in the `onConditionChanged` function</summary>
    
    ```javascript
    // Initialize variable values: player, origin, target position
    if (!$.state.player || !$.state.originPos || !$.state.targetPos) {
       $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
       $.state.originPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.3));
       $.state.targetPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.6));
    }
    
    $.state.gain = 1; // The gain value remains 1 before touching the origin
    $.subNode("Sphere").setPosition($.state.originPos); // Move the green ball to the origin
    ```
    
    </details>
    
    <details>
    
    <summary>Replace the code in the `tick` function</summary>
    
    ```javascript
    if (!$.state.player || !$.state.originPos) return;
    
    // Calculate the position of the virtual hand: relative position of the physical hand (controller) from the origin × gain
    $.subNode("RightHandAnchor").setPosition(
       $.state.originPos.clone()
           .add($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand).clone()
               .sub($.state.originPos)
               .multiplyScalar($.state.gain || 1)));
    
    // Synchronize the rotation of the virtual hand with the physical hand
    $.subNode("RightHandAnchor").setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand).clone().multiply($.state.handOffset));
    
    if (!$.state.isTouchable) { // If the green ball was just touched and should not respond for a while
       $.state.timer = $.state.timer + 1; // Increment timer by 1
      
       if ($.state.timer > 10) { // If 10 frames have passed since the green ball was touched
           $.state.isTouchable = true; // Allow the green ball to be touched again from the next frame
           $.state.timer = 0; // Reset the timer to 0
       } else {
           $.setStateCompat("this", "isSphereTouched", false); // Keep the flag for detecting if the green ball was touched as false
       }
    } else if ($.getStateCompat("this", "isSphereTouched", "boolean")) { // If the green ball has been detected as touched (when it becomes true)
       /*
           When the green ball is touched, make the value of $.getStateCompat("this", "isSphereTouched", "boolean") change,
           add the CCK component `On Collide Item Trigger` to the item with this script,
           and make it send a message with the key `isSphereTouched` with the content = true towards this item.
       */
    
       $.state.isTouchable = false; // Set the flag to allow touching the green ball to false to prevent double clicking
       $.setStateCompat("this", "isSphereTouched", false); // Reset the flag for detecting if the green ball was touched to false
    
       if ($.state.isReaching) {
           // If the green ball at the target position is touched
           onTargetTouched();
       } else {
           // If the green ball at the origin is touched
           onOriginTouched();
       }
    }
    ```
    
    </details>
    
    <details>
    
    <summary>Add functions at the end of the script file</summary>
    
    ```javascript
    // Executed when the green ball at the origin is touched
    function onOriginTouched () {
       $.subNode("Sphere").setPosition($.state.targetPos); // Move the green ball to the target position
       $.state.gain = $.state.currentCondition["gain"] ? parseFloat($.state.currentCondition["gain"]) : 1; // Set the gain value for this trial
       $.state.isReaching = true; // Set the reaching (extending hand to the target position) flag to true
    }
    
    
    // Executed when the green ball at the target position is touched
    function onTargetTouched () {
       $.sendSignalCompat("this", "state_triggerTransition"); // Transition to the next state (the phase to answer questions)
       /*
           To ensure this function transitions to the next phase,
           add the CCK component `Global Logic` to the item with this script.
           In that `Global Logic`, detect the key `state_triggerTransition` for this item,
           and send a signal globally with the key `state_triggerTransition`.
       */
    
       $.state.isReaching = false; // Set the reaching (extending hand to the target position) flag to false
    }
    ```
    
    </details>

3. Once you have finished editing the script, press the `Update Script` button in the `Objects Manager` tab of the `Luida Editor`.
<img width="1179" alt="スクリーンショット 2024-11-06 20 30 20" src="https://github.com/user-attachments/assets/a6eca71d-a67f-45f1-a103-8d4258f8c4f6">

4. Add the following CCK components to the `TaskManager` gameobject:

**Global Logic**

Purpose: To transition to the next state when the sphere is touched after moving.

<details>
    
<summary>Configure the component</summary>

```text
Target: Item
Key: state_triggerTransition
Item: the TaskManager gameobject itself
----------
Global state_triggerTransition Signal
= Constant Bool true
```

Explanation: It receives a signal with key `state_triggerTransition` from itself (e.g., the first line of the onTargetTouched function in TaskManager.js) and broadcasts a signal with key `state_triggerTransition` globally.

</details>

**On Collide Item Trigger**
- Purpose: To detect contacts with the sphere.

<details>
    
<summary>Configure the component</summary>

```text
Collision Event Type: Enter
Collision Type: Collision

Triggers
----------
Target: This isSphereTouched
Value: Bool true
```

Explanation: When colliding with other objects, it sets `isSphereTouched` to true directed towards itself, which is then processed in TaskManager.js > tick function using `if ($.getStateCompat("this", "isSphereTouched", "boolean"))`.

</details>

![image](https://github.com/user-attachments/assets/91a095bb-9ca8-4126-adc9-6ff89b3e7d5c)

### Prepare Objects to be Manipulated During the Task

1. **Reaching Task Target (a small sphere)**
    1. Create a Sphere gameobject as a child of the `TaskManager` gameobject. Set its scale to `(0.05, 0.05, 0.05)` and change its material to green.
    2. Enable triggering `TaskManager`'s `OnCollideItemTrigger` event when the target is touched by the virtual hand: Add a `RigidBody` component, uncheck its `Use Gravity` checkbox, and check all checkboxes in `Constraints` to avoid any movement not controlled by scripts.

<table>
  <tr>
    <td><img width="613" alt="Screenshot 2024-11-06 15 14 00" src="https://github.com/user-attachments/assets/4d4da88b-b620-49ca-8cb4-ebf5abe465b6" width="500"></td>
    <td><img width="549" alt="Screenshot 2024-11-06 15 14 45" src="https://github.com/user-attachments/assets/fe1a2302-bf95-4288-8a77-b35261549d0e" width="500"></td>
  </tr>
</table>

2. **Make the Virtual Hand Follow the User's Hand Position**
    1. Create an empty gameobject named `RightHandAnchor` as a child of the `TaskManager` gameobject.
    2. Open the `Objects Manager` tab in the `Luida Editor` to create a gameobject named `RightHandWrapper` for the state `Trial - Task`.
![add-gameobject-to-state-no-prefab-no-script](https://github.com/user-attachments/assets/2896fba5-99d2-4609-bfb7-b59802a1266c)

    3. Add the virtual hand prefab to the scene as a child gameobject of `RightHandWrapper`.
    4. On the newly added RightHand gameobject, add a `ParentConstraint` component, set `Sources` to the `RightHandAnchor` gameobject, and then press the Activate button.
        <img width="613" alt="Screenshot 2024-11-06 15 22 29" src="https://github.com/user-attachments/assets/b47957a1-3ba3-4fcf-963f-b1f5e69f7157">

### Add a Message Panel in State `Trial - Task`

Open the `Objects Manager` tab in the `Luida Editor` to create a gameobject from prefab `Message` for the state `Trial - Task`.

<details>
    
<summary>Text Content</summary>

```text
Please touch the green sphere that appears in front of you with your right index finger. When touched, the sphere will move 30 cm away. Please touch the sphere again after it has moved.
```

</details>

### Add Question Panel and Answer Buttons in State `Trial - Questionnaire`

1. Open the `Objects Manager` tab in the `Luida Editor`. For the state `Trial - Questionnaire`, create a gameobject named `Question` from prefab `Message`, a gameobject named `FasterButton` from prefab `NextStateButton`, and a gameobject named `SlowerButton` from prefab `NextStateButton`.

https://github.com/user-attachments/assets/b9ae7a08-7f5d-4b1b-889c-0bb5afed96d2

2. Edit the text content of the `Question` gameobject.
<details>
    
<summary>Text Content for `Question`</summary>

```text
Q: Did the hand in the virtual space move faster or slower than the actual hand? If you are unsure, please press either button.
```


</details>

3. **FasterButton**: Set position to `(-0.75, 0.75, -0.5)` and add the following triggers to the `Interact Item Trigger` component:
    ```text
    Target: Global isFaster
    Value: Bool true
    ----------
    Target: Global exp_recordCustomData
    Value: Signal
    ```
4. **SlowerButton**: Set position to `(0.75, 0.75, -0.5)` and add the following triggers to the `Interact Item Trigger` component:
    ```text
    Target: Global isFaster
    Value: Bool false
    ----------
    Target: Global exp_recordCustomData
    Value: Signal
    ```

<table>
  <tr>
    <td><img width="621" alt="Faster Button" src="https://github.com/user-attachments/assets/fc967e8f-0258-4ccc-a7f5-9c1e423d5bd5" width="500"></td>
    <td><img width="610" alt="Slower Button" src="https://github.com/user-attachments/assets/2f87fbab-e9f3-4ac7-8281-4b8d170ee660" width="500"></td>
  </tr>
</table>

### Add Message in State `Trial - Rest`

Create a gameobject from the `Message` prefab and change its text to `Put your arm down.`

### Trigger Uploading Data After Trials

For the state `AfterTrials`, create a gameobject named `UploadAndNextButton` from the `NextStateButton` prefab.

On its `Interact Item Trigger` component, add one more trigger:

```text
Target: Global exp_uploadCustomData
Value: Signal
```

![image](https://github.com/user-attachments/assets/d441f574-50aa-434c-95b8-031bc43d1221)

## Implement Practice Session

The practice session is similar to the trial session, but we will only use the gain values of `0.75 (minimum)`, `1`, and `1.25 (maximum)`. Additionally, we will not access experimental conditions or record any data.

Please follow the steps below to implement the practice session.

### Duplicate Game Objects from the Trial Session to the Practice Session (Check the video below)

1. Duplicate the following game objects from the `Trial - XXX` state into the `Practice - XXX` state (copy the child game objects from `States > Trial - XXX > Objects` to `States > Practice - XXX > Objects`):
    - From `Trial - Task`: `Message`, `RightHandWrapper` → to `Practice - Task`
    - From `Trial - Questionnaire`: `Question`, `FasterButton`, `SlowerButton` → to `Practice - Questionnaire`
    - From `Trial - Rest`: `Message` → to `Practice - Rest`
2. Open the `Objects Manager` tab in the `Luida Editor` (if it’s already open, close and reopen it), and press the `Fix state_id` button for all items in the `Practice - XXX` state.

https://github.com/user-attachments/assets/14c40fdc-b412-4990-b994-32bae7aaffe9

### Create the PracticeTaskManager Object in the `Practice - Task` State

1. Refer to the video above, open the `Objects Manager` tab in the `Luida Editor`, and create a game object with a script called `PracticeTaskManager` for the `Practice - Task` state. Ensure that the created game object and a Cluster script asset named `PracticeTaskManager.js` are generated.

https://github.com/user-attachments/assets/4937f73b-7eec-4c86-9183-1251c2b22c52

2. Edit `PracticeTaskManager.js` as follows.

<details>
    
<summary>Add to the Top of the Script File</summary>

```javascript
const gains = [1, 0.75, 1.25]; // Gains applied to the virtual hand during practice
```

</details>

<details>
    
<summary>Replace the `OnStateEnter` Function</summary>

```javascript
 // Initialize variable values: player, origin, target position
if (!$.state.player || !$.state.originPos || !$.state.targetPos) {
    $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
    $.state.originPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.3));
    $.state.targetPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.6));
}

$.state.gain = 1; // The gain value remains 1 before touching the origin
$.subNode("Sphere").setPosition($.state.originPos); // Move the green sphere to the origin
```

</details>

<details>
    
<summary>Replace the `DuringState` Function</summary>

```javascript
if (!$.state.player || !$.state.originPos) return;

// Calculate the position of the virtual hand: relative position of the physical hand (controller) from the origin × gain
$.subNode("RightHandAnchor").setPosition(
    $.state.originPos.clone()
        .add($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand).clone()
            .sub($.state.originPos)
            .multiplyScalar($.state.gain || 1)));

// Synchronize the rotation of the virtual hand with the physical hand
$.subNode("RightHandAnchor").setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand).clone().multiply($.state.handOffset));

if (!$.state.isTouchable) { // If the green ball was just touched and should not respond for a while
    $.state.timer = $.state.timer + 1; // Increment timer by 1

    if ($.state.timer > 10) { // If 10 frames have passed since the green ball was touched
        $.state.isTouchable = true; // Allow the green ball to be touched again from the next frame
        $.state.timer = 0; // Reset the timer to 0
    } else {
        $.setStateCompat("this", "isSphereTouched", false); // Keep the flag for detecting if the green ball was touched as false
    }
} else if ($.getStateCompat("this", "isSphereTouched", "boolean")) {
    /*
        When the green ball is touched, make the value of $.getStateCompat("this", "isSphereTouched", "boolean") change,
        add the CCK component `On Collide Item Trigger` to the item with this script,
        and make it send a message with the key `isSphereTouched` with the content = true towards this item.
    */

    $.state.isTouchable = false; // Set the flag to allow touching the green ball to false to prevent double clicking
    $.setStateCompat("this", "isSphereTouched", false); // Reset the flag for detecting if the green ball was touched to false

    if ($.state.isReaching) {
        // If the green ball at the target position is touched
        onTargetTouched();
    } else {
        // If the green ball at the origin is touched
        onOriginTouched();
    }
}
```

</details>

<details>
    
<summary>Add Functions at the End of the Script File</summary>

```javascript
 // Executed when the green ball at the origin is touched
function onOriginTouched () {
    $.subNode("Sphere").setPosition($.state.targetPos); // Move the green ball to the target position
    $.state.gain = gains[$.state.gainId]; // Set the gain value for this trial
    $.state.isReaching = true; // Set the reaching (extending hand to the target position) flag to true
}

// Executed when the green ball at the target position is touched
function onTargetTouched () {
    $.state.gainId = $.state.gainId + 1; // Increment gain ID for the next trial
    $.sendSignalCompat("this", "state_triggerTransition"); // Transition to the next state (the phase to answer questions)
    /*
        To ensure this function transitions to the next phase,
        add the CCK component `Global Logic` to the item with this script.
        In that `Global Logic`, detect the key `state_triggerTransition` for this item,
        and send a signal globally with the key `state_triggerTransition`.
    */

    $.state.isReaching = false; // Set the reaching (extending hand to the target position) flag to false
}
```

</details>

3. Once you have finished editing the script, press the `Update Script` button in the `Objects Manager` tab of the `Luida Editor`.
<img width="1203" alt="Screenshot 2024-11-06 20 29 50" src="https://github.com/user-attachments/assets/f603a55c-08db-45af-ba40-076b7868b545">

### Adjust the Following of the Duplicated Virtual Hand in the `Practice - Task` State

1. Create an empty game object named `RightHandAnchor` as a child of the `PracticeTaskManager` object.
2. Set the `Sources` of the `ParentConstraint` component in the child game object `RightHand` of `RightHandWrapper` to the `RightHandAnchor` game object of the `PracticeTaskManager`, then press the Activate button.

<img width="619" alt="RightHand in Practice session" src="https://github.com/user-attachments/assets/80255eb0-9c0b-4514-a931-8adea53ed5c9">

### Adjust the Settings of the Duplicated Answer Buttons in the `Practice - Questionnaire` State

Remove the triggers containing the keys `isFaster` and `exp_recordCustomData` from the `Interact Item Trigger` components of `FasterButton` and `SlowerButton`.

## Adding Data Recording and Upload Objects

1. Press the `Add Custom Data Recorder` button in the `Data Recorders List` tab of the `Luida Editor`. Confirm that a game object and script have been generated.
![create-data-recorder](https://github.com/user-attachments/assets/06a8a5bd-c475-4f3a-b86d-5005393ad1c0)
2. Open the script file and replace the contents of the function `calculateData` with the following.

<details>
    
<summary>Replace the contents of the function `calculateData`</summary>

```javascript
let fileName = "reachingTaskAnswers";
let returnData = $.state.customData;

const newRecord = {
 g: $.state.currentCondition["gain"], // Current gain condition
 a: $.getStateCompat("global", "isFaster", "boolean") ? "F" : "S" // Whether 'Fast' or 'Slow' was chosen
};

if (fileName in returnData && Array.isArray(returnData[fileName])) {
 returnData[fileName].push(newRecord);
} else {
 returnData[fileName] = [newRecord];
}

return returnData;
```

Explanation: The value of "gain" for each trial and the value of `isFaster` chosen by the participant are saved. If 'Fast' is chosen, `isFaster` is recorded as true; if 'Slow' is chosen, it is recorded as false.

</details>

3. Once you have finished editing the script, press the `Update Script` button in the `Data Recorders List` tab of the `Luida Editor`.

<img width="1209" alt="Screenshot 2024-11-06 20 45 59" src="https://github.com/user-attachments/assets/e153eaff-38d2-453f-9c18-375cc954bcee">

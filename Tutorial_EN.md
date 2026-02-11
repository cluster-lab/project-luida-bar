# LUIDA Implementation Template Tutorial

This tutorial guides you through implementing an experiment on LUIDA using this implementation template, by building a simple experiment step by step.

## Overview of Using This Implementation Template

0.  Installation, account creation, and learning about CCK
1.  Create a new experiment on the LUIDA Web Console and fill in the required information
2.  Initial setup of this implementation template (Unity)
3.  Detailed experiment configuration using the Luida Editor
    1.  Set experiment variables (within-subject variables, between-subject variables) - trial count is automatically calculated
    2.  Design experiment progression (state transitions)
    3.  Link questionnaires to states
    4.  Configure object behaviors linked to states
    5.  Define custom data to record
4.  Other implementations using Unity & CCK
5.  Test locally (in Unity Editor)
6.  Upload to cluster as a world and verify operation
7.  Final settings on LUIDA Web Console (World ID registration and avatar settings)
8.  Wait for publication

---

## Prerequisites: Installation, Account Creation, and Learning CCK

Before starting this tutorial, we recommend learning the following basics:

- Unity
- JavaScript
- [Cluster Creator Kit (CCK)](https://docs.cluster.mu/creatorkit/)
  - At minimum, review [Triggers and Gimmicks](https://docs.cluster.mu/creatorkit/world/trigger-gimmick/)

Also, please complete the following preparations before starting the tutorial:

1.  [Create a cluster account](https://help.cluster.mu/hc/articles/115000827112)
2.  [Install the required version of Unity for cluster](https://docs.cluster.mu/creatorkit/installation/install-unity/)
3.  Log in to the [LUIDA Web Console](https://luida.cluster.mu/experiments) for the first time and submit an approval request. After submitting the request, please contact the administrator (y.hu@cluster.mu).

---

## 0. Experiment Plan for This Tutorial

In this experiment, we will reproduce the Stroop effect (a phenomenon where recognizing the color of text takes longer when the color differs from the meaning of the word) in VR space.
First, participants will perform repeated calculation tasks to induce cognitive load, then we will examine the effects of stimulus presentation depth conditions (near/far) on reaction time and accuracy.

- Experiment procedure
  - Start -> Instructions -> Repeated calculation tasks -> Main task repeated twice per condition -> Presence questionnaire -> End
- Experimental conditions (in the main task)
  - Between-subject condition: Depth (near/far)
  - Within-subject conditions:
    - Response target: First respond to font color, then respond to text meaning
    - Font color: Red or Blue (random presentation)
    - Text meaning: "Red" or "Blue" (random presentation)
  - Each condition is administered twice
- Evaluation metrics (in the main task):
  - Response results for each condition (accuracy can be calculated later)
  - Response time for each condition

Experiment demonstration (from start to questionnaire):
https://github.com/user-attachments/assets/852259cb-f871-4589-bdf1-cb37cefde213

---

## 1. Register Experiment Information on Web Console

<details>
<summary><h3>Follow these steps to register and configure</h3></summary>

1.  Open the [LUIDA Web Console](https://luida.cluster.mu/experiments).
2.  **Create a new experiment**: Click "+New Experiment" and register the basic experiment information with the following values:
    1. Title: `Stroop Effect Experiment`
    2. Participation requirements: `No color vision deficiency`
    3. Reward: `0`
    4. Image URL: Any string (e.g., `https://example.com/image.png`)
    5. World ID: Leave blank for now
    6. Room capacity: `1`
    7. Status: `Testing`

|                                           New Experiment Button                                           |                                     Experiment Registration Form                                      |
| :-------------------------------------------------------------------------------------------------------: | :---------------------------------------------------------------------------------------------------: |
| ![New experiment button](https://github.com/user-attachments/assets/cc1cc6c5-b0c9-4a48-bf08-daf5d345e04a) | ![Registration form](https://github.com/user-attachments/assets/a1c19c68-bed3-4b46-8d6e-5abb034f623b) |

3.  **Navigate to experiment details page**: Once the experiment is registered, click on that experiment's row from the home page to access its details page. Verify the information you just registered.
4.  **Create questionnaires**: Scroll down and perform the following operations in the questionnaire registration form:

    1. Pre-experiment questionnaire
       1. Click "Add Questionnaire" button -> Enter any title and description without selecting a template -> Click "Add" button
       2. Click the "Question List" button for the added questionnaire and add question items by filling in the form and pressing "+Add Question". Example questions:
          - Title: `Gender`, Options: `Male,Female,Other`, Question Type: `Radio Button`
          - Title: `Age`, Question Type: `Text Input`
          - Title: `I frequently experience VR`, Description: `1: Strongly disagree, 2: Disagree, 3: Somewhat disagree, 4: Neutral, 5: Somewhat agree, 6: Agree, 7: Strongly agree`, Options: `1,2,3,4,5,6,7`, Question Type: `Linear Scale`
    2. Post-experiment questionnaire 1. Click "Add Questionnaire" button -> Select "IPQ" from "Template Selection" -> Click "Add" button 2. Click the "Question List" button for the added questionnaire and verify the added questions.
       ![Creating questionnaires](https://github.com/user-attachments/assets/f468f40b-83ab-4646-83ad-a28c212a9ec4)

5.  **Copy experiment ID**: Copy the "Experiment ID" displayed at the top of the experiment details page. This ID will be used later in the Unity project.
    ![Experiment ID location](https://github.com/user-attachments/assets/20780d32-15ce-4588-a377-415b6d0fef40)

</details>

---

## 2. Download and Initial Setup of Implementation Template (Unity)

<details>
<summary><h3>Follow these steps to register and configure</h3></summary>
*Some of these steps will be automated in the future. For now, please complete all steps.

1.  Download the [latest release of the implementation template](https://github.com/cluster-lab/project-luida-bar/releases).
2.  Open the downloaded Unity project from Unity Hub. Errors may appear in the console when opening the project, but ignore them first and import the following required packages into Unity:
    - [**CSCombiner: Tool for combining Cluster Scripts in Unity Editor**](https://vkao.booth.pm/items/5924956) (ver1.01 recommended)
    - [**CSEmulator: Tool to play Cluster Scripts in Unity Editor**](https://vkao.booth.pm/items/5111235) (for now, please use **V2** instead of V3)
3.  Open `LUIDA > Scene > Create New Scene` from Unity's menu bar, enter a scene name for your experiment (e.g., `MyExperimentScene`), and press the OK button.

    <img width="482" height="120" alt="image" src="https://github.com/user-attachments/assets/78e2852f-3fd0-4655-a9c5-7a5efa189eba" />
    <img width="322" height="138" alt="image" src="https://github.com/user-attachments/assets/925099e3-3089-4a92-80ef-29e7d7200e1e" />

4.  Next, link your cluster account to this Unity project. The procedure is as follows (see the image for reference):

    1. From the Unity menu bar, open the `LUIDA > Configure experiment identifiers` window.
    2. Click the `Create Access Token` button to open the cluster access token creation page in your web browser.
    3. Click the `Create Token` button, and then copy and paste the displayed token into the `Access Token` field in the `Configure experiment identifiers` window that you just opened in Unity.

    ![cluster access token registration procedure](https://github.com/user-attachments/assets/e3f23566-e1b0-459b-8586-58ee897b7616)

5.  With the identifiers configuring window still open, enter the Experiment ID you copied from the web console earlier into the `Experiment ID` field, and then close the window.

    ![Luida Editor Experiment ID registration screen](https://github.com/user-attachments/assets/45afbd6b-502b-40ef-a1f7-b2c2c8ef9c30)

6. If the Verify Token is not generated yet, click the `Generate a new verify token` button.

   ![Verify Token generation](https://github.com/user-attachments/assets/f4fcf341-d44a-4ad1-ab31-147f01a3dde0)

7. Close the identifiers configuring window.

</details>

---

## 3. Set Experiment Variables and Trial Count

In the `LUIDA > Configure experiment automation > Experiment Variables` screen (left figure), when you register within-subject/between-subject variables for the experiment, the system automatically determines the number of trials and experimental conditions for each trial (right figure).

For details on each setting, see the [documentation](/Documentation_JA.md#experiment-variables-and-trial-count-settings).

<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/d697ecb9-555c-4a65-90fe-1cc295cabc8d" alt="variables-config-ui" width="600"/></td>
    <td><img src="https://github.com/user-attachments/assets/d888c793-3c0c-4215-94a3-2eb5d95db4e6" alt="variables-config-example" width="600"/></td>
  </tr>
</table>

<details>
<summary><h3>Follow these steps to configure</h3></summary>

1.  Open the `LUIDA > Configure experiment automation` window from Unity's menu bar and click the `Activate Experiment Automation Feature` button.

    <img width="767" height="207" alt="Configure experiment automation window" src="https://github.com/user-attachments/assets/79dffcb5-7f2e-4f4b-8f20-fd824558c3e3" />

2.  Confirm that the `Experiment Variables` tab is open.
3.  **Set within-subject variables**: Add 3 items to `Variables for Within-Subject Conditions` and configure as follows:
    - Name: `request` Values: `font, meaning` isRandom: [ ]
    - Name: `font` Values: `R,B` isRandom: [x]
    - Name: `text` Values: `Red,Blue` isRandom: [x]
4.  **Set between-subject variables**: Add 1 item to `Variables for Between-Subject Conditions` and configure as follows:
    - Name: `depth` Values: `near,far`
5.  Enter `2` in `Trials Count per Condition`. This means each condition (combination of variables) will have 2 trials.

After configuration, verify that the screen looks like the figure below.

<img width="954" height="327" alt="image" src="https://github.com/user-attachments/assets/ff61673a-d7a2-423d-b0fb-a636c5d7d285" />

</details>

---

## 4. Configure Experiment Progression (State Transitions) and Link Questionnaires

In the `LUIDA > Configure experiment automation > State Machine` screen (left figure), when you set up the experiment flow in units called "states", the experiment progresses automatically as shown in the right figure.
Each state represents a segment of the experiment (e.g., instructions, task execution, rest, questionnaire response), and you define how they transition here.

For details on each setting, see the [documentation](/Documentation_JA.md#experiment-progression-state-transitions-and-questionnaire-linking).

![state-transition-example](https://github.com/user-attachments/assets/69ce4439-a2da-4912-8344-feba222e1347)

<details>
<summary><h3>Follow these steps to configure</h3></summary>

1. **Link the first state to a questionnaire**: In the `Start` row:
   1. Press the `Add Questionnaire` button and enter `1` for `qID`.
2. **Auto-skip the instruction state after 30 seconds**: Check `Has Exit Time` in the `Intro` row and enter `30` for `Exit Time`.
3. **Add a calculation task state and repeat 5 times**:
   1. Press the `Add State Before Trials` button to add a state. Rename it to `CalculationTask`.
   2. Check `Is Repeated` in the `CalculationTask` row, select `CalculationTask` for `Repeat Destination`, and enter `5` for `Repeat Count`.
      - This state will now repeat 5 times before proceeding.
      - Increase the number if you want more calculations.
4. **Trial state**: No special settings needed for the `Trial - Start` row.
5. **Auto-skip trial rest state after 3 seconds**: Check `Has Exit Time` in the `Trial - Rest` row and enter `3` for `Exit Time`.
6. **Link the post-trial state to a questionnaire**: In the `Outro` row:
   1. Press the `Add Questionnaire` button and enter `2` for `qID`.

After configuration, verify that the screen looks like the figure below.

<img width="1110" height="1025" alt="image" src="https://github.com/user-attachments/assets/95355e8a-f512-45d5-9d66-5d98898187ef" />

</details>

---

## 5. Configure Object Behaviors According to Experiment Progression

In `LUIDA > Configure experiment automation > State-listening Items`, you can control object behaviors according to experiment progression (state transitions).

Examples of behaviors: Show/hide self or child objects, set position/rotation of self or child objects, change text content, vibrate controller, wait for seconds, custom actions (code your own), etc.

_\*Objects and behaviors not dependent on specific states can also be created/configured using standard Unity and Cluster Creator Kit (CCK) methods._

For details on each setting, see the [documentation](/Documentation_JA.md#configuring-object-behaviors-according-to-experiment-progression).

![Luida Editor State-listening Items tab](https://github.com/user-attachments/assets/1937e67a-8137-482e-95c5-6ce359d00259)

### Follow the steps below to create and configure behaviors for each object

<details>

<summary><h3>Instruction Object</h3></summary>

0. Open `LUIDA > Configure experiment automation > State-listening Items`.
1. Enter `Instruction` in `New Item Name` and click the `Add state-listening item` button.
   - A column will be added to the table below, and an object named `Instruction` will be created in the scene.
2. Click the `Add Listener` button at the cells where the `Instruction` column intersects with the `Intro`, `Trial - Start`, and `Trial - Rest` rows, and configure as shown in the table below.

|                                                  Intro                                                   |                                                  Trial - Start                                                   |                                             Trial - Rest                                             |
| :------------------------------------------------------------------------------------------------------: | :--------------------------------------------------------------------------------------------------------------: | :--------------------------------------------------------------------------------------------------: |
| ![Instruction at Intro](https://github.com/user-attachments/assets/dadb05d3-727e-4c9d-a5a7-7fc31d259787) | ![Instruction at Trial - Start](https://github.com/user-attachments/assets/d06ccd02-2873-4bc1-b3ec-0db15056198a) |      ![image](https://github.com/user-attachments/assets/b5c1d8d9-592c-4cae-bb64-80dcb625881d)       |
|      Shows when entering Intro state, text changes to experiment instructions. Hides when exiting.       |   Shows when entering Trial - Start state, text changes (see "Instruction Set Text for Trial - Start" below).    | When entering Trial - Rest state, text changes to: `Take a break for 5 seconds`. Hides when exiting. |

<details>

<summary>Instruction Set Text for Trial - Start</summary>

- On State Start 1st Set Text
  1. Click the **if** button
  2. Var Name: `request`
  3. Is Value: `font`
  4. Text: `Click the button that matches the text's font color.`
- On State Start 2nd Set Text
  1. Click the **if** button
  2. Var Name: `request`
  3. Is Value: `meaning`
  4. Text: `Click the button that matches the text's meaning.`

This makes the instruction object's text content differ based on the experimental condition `request`.

</details>

3. Find the Instruction object in the scene and adjust its position as shown in the figure below.
4. Find the Text child object and configure `TextView` as shown in the figure below.

|                                                Instruction                                                 |                                                    Text child object                                                    |
| :--------------------------------------------------------------------------------------------------------: | :---------------------------------------------------------------------------------------------------------------------: |
| ![Instruction GameObject](https://github.com/user-attachments/assets/6a8c2664-7cd2-4c9d-a8ca-375900f214f4) | ![Instruction GameObject's Text child](https://github.com/user-attachments/assets/e7cc8c1c-21a9-4463-8dc7-a36d5d9f6c1e) |

</details>

<details>

<summary><h3>Task Stimulus Text</h3></summary>

0. Open `LUIDA > Configure experiment automation > State-listening Items`.
1. **Red font text**: Enter `Text_RedFont` in `New Item Name` and click `Add state-listening item` button.
2. **Blue font text**: Enter `Text_BlueFont` in `New Item Name` and click `Add state-listening item` button.
3. Click the `Add Listener` button at the cells where the `Text_RedFont` and `Text_BlueFont` columns intersect with the `Trial - Start` row, and configure as shown in the figure below. - _Configuration explanation_ - _Only visible during `Trial - Start` state_ - _If the trial's condition `font`=`R`, only the **red font** is shown; if `font`=`B`, only the **blue font** is shown_ - _If the trial's condition `text`=`Red`, both texts show **Red**; if `text`=`Blue`, both show **Blue**_ - _If the trial's condition `depth`=`near`, both are positioned at **z=1**; if `depth`=`far`, both at **z=3**_
   <img width="499" alt="Screenshot 2025-05-28 at 15 51 20" src="https://github.com/user-attachments/assets/dcb72791-f7d0-4f05-a241-10fd421994a3" />

4. Find the Text_RedFont and Text_BlueFont objects in the scene and adjust their positions as shown in the figure below.
5. Add a `MovableItem` component to each, uncheck `Use Gravity` in `RigidBody`, and check `Is Kinematic`.
6. Find the Text child object for each and configure `TextView` as shown in the figure below.

|                                        Text_RedFont / Text_BlueFont                                        |                                                    Text child object                                                    |
| :--------------------------------------------------------------------------------------------------------: | :---------------------------------------------------------------------------------------------------------------------: |
| ![Instruction GameObject](https://github.com/user-attachments/assets/144e8368-318b-4ed1-8450-6d09502640b7) | ![Instruction GameObject's Text child](https://github.com/user-attachments/assets/293b280e-bffb-4a62-b779-f470b70d21c6) |

</details>

<details>

<summary><h3>Task Answer Buttons</h3></summary>

0. Open `LUIDA > Configure experiment automation > State-listening Items`.
1. **Red answer button**: Enter `Answer_Red` in `New Item Name` and click `Add state-listening item` button.
2. **Blue answer button**: Enter `Answer_Blue` in `New Item Name` and click `Add state-listening item` button.
3. Click the `Add Listener` button at the cells where the `Answer_Red` and `Answer_Blue` columns intersect with the `Trial - Start` row, and configure as shown in the figure below.
   - Configuration explanation: Only visible during `Trial - Start`.

<img width="506" alt="Screenshot 2025-05-28 at 16 11 54" src="https://github.com/user-attachments/assets/7a9dc11d-6518-4026-87ad-6e71b8f71628" />

4. **Button appearance**: Find the Answer_Red and Answer_Blue objects in the scene, create a Cube as a child object, and adjust position and material.

|                                              Answer_Red Text child object                                               |                                       Answer_Blue Text child object                                        |
| :---------------------------------------------------------------------------------------------------------------------: | :--------------------------------------------------------------------------------------------------------: |
| ![Instruction GameObject's Text child](https://github.com/user-attachments/assets/1838857f-5add-44a3-9b32-ad2ca4b7bff4) | ![Instruction GameObject](https://github.com/user-attachments/assets/db444f9a-e571-40e3-90f7-ee784c495cd5) |

5. **Click behavior and state transition setup**: Find the Answer_Red and Answer_Blue objects themselves, add `Interact Item Trigger` and `Luida To Next State Gimmick` components to each, and configure as shown in the figure below.

|                                                 Answer_Red                                                 |                                                       Answer_Blue                                                       |
| :--------------------------------------------------------------------------------------------------------: | :---------------------------------------------------------------------------------------------------------------------: |
| ![Instruction GameObject](https://github.com/user-attachments/assets/631c765c-8daf-4e7d-8ceb-d5ba3543bdd6) | ![Instruction GameObject's Text child](https://github.com/user-attachments/assets/49386e8f-539a-465a-b2cd-a4b53f29870a) |
|                                                isRed: Check                                                |                                                     isRed: Uncheck                                                      |

   <details>
   <summary>How it works and additional notes</summary>

> **Note**: If the `LUIDA-DataCollector` game object doesn't exist, right-click in Hierarchy and select `LUIDA > Data Collector` to create it.
>
> <img width="471" height="752" alt="image" src="https://github.com/user-attachments/assets/66a310c7-6cc6-49df-90fa-875c4820b22d" />

- See CCK documentation for the relationship between Triggers and Gimmicks: https://docs.cluster.mu/creatorkit/world/trigger-gimmick/
- Expected behavior (e.g., Answer_Red):
  1.  When clicked, `Interact Item Trigger` activates, sending a message with `isRed=true` to `LUIDA-DataCollector`, then sending a `toNextState` message to This (itself).
  2.  Upon receiving the `toNextState` message, `Luida To Next State Gimmick` triggers a state transition.
- _`Luida To Next State Gimmick` is a LUIDA-specific CCK gimmick. Others include `Luida Process Data And Save To Collection Gimmick` and `Luida Update Collected Data Gimmick`._

   </details>

</details>

<details>

<summary><h3>Timer (Time Recording) Object</h3></summary>

0. Open `LUIDA > Configure experiment automation > State-listening Items`.
1. Enter `TimeRecorder` in `New Item Name` and click `Add state-listening item` button.
2. Click the `Add Listener` button at the cells where the `TimeRecorder` column intersects with `Functions, events, variables not listening to the state machine`, `Trial - Start`, and `Outro` rows, and configure as shown in the table below.

|                      Functions, events, variables not listening to the state machine                       |                                                Trial - Start                                                 |                                                 Outro                                                  |
| :--------------------------------------------------------------------------------------------------------: | :----------------------------------------------------------------------------------------------------------: | :----------------------------------------------------------------------------------------------------: |
| ![Instruction GameObject](https://github.com/user-attachments/assets/47dbd2d3-e382-45f2-bc0a-9ceb410e0db6) | ![TimerRecorder-TrialStart](https://github.com/user-attachments/assets/837fab63-e31c-47d3-a62a-a24a8bdcd5bf) | ![TimeRecorder-Outro](https://github.com/user-attachments/assets/ea250246-4c3a-4aca-b94b-7feecde4c41e) |
|                              Script and explanation are in the section below                               |                               Script and explanation are in the section below                                |                            After all trials end, uploads the recorded data.                            |

<details>

<summary>Functions, events, variables not listening to the state machine</summary>

Initialize timer and run timer during trial:

```javascript
function Start() {
  $.state.timer = 0;
}
function Update(deltaTime) {
  if ($.state.isInTrial) $.state.timer += deltaTime;
}
```

</details>

<details>

<summary>Trial - Start</summary>

On State Start Customized Action:
Initialize timer and set in-trial flag

```javascript
$.state.isInTrial = true;
$.state.timer = 0;
```

On State End Customized Action:
Clear in-trial flag and send timer value (see CCK documentation for setStateCompat)

```javascript
$.state.isInTrial = false;
SendDataToCollector("timer", $.state.timer); // Send response time to LUIDA's Data Collector (details later)
```

Also, the `Process and save collected data` action triggers LUIDA's data recording function. Data format is described later.

</details>

</details>

<details>

<summary><h3>Calculation Task Object (for Cognitive Load)</h3></summary>

1. Open `LUIDA > Configure experiment automation > State-listening Items`.
2. Enter `CalculationTextInput` in `New Item Name` and click `Add state-listening item` button.
3. Copy and paste the following script into the `Functions, events, variables not listening to the state machine` code block for the `CalculationTextInput` column:

```javascript
function getRandomInt(max) {
  // Define function to generate random integer
  return Math.floor(Math.random() * max);
}
$.onTextInput((text, meta, status) => {
  if (status === TextInputStatus.Success) {
    ToNextState(); // Transition to next state when text input is received from participant
    // Note: We're just having them calculate, so we don't check if the answer is correct. Edit the script yourself if you want to verify.
  }
});
```

4. Click the `Add Listener` button at the cell where the `CalculationTextInput` column intersects with the `CalculationTask` row, add a Customized Action, and copy and paste the following script into the code block:

```javascript
// PARTICIPANTS[1] identifies the first participant (there's only one anyway)
PARTICIPANTS[1].requestTextInput(
  "ask_to_calculate",
  getRandomInt(100) + "+" + getRandomInt(100) + "=?"
);
```

|                      Functions, events, variables not listening to the state machine                      |                                                     Trial - Start                                                     |
| :-------------------------------------------------------------------------------------------------------: | :-------------------------------------------------------------------------------------------------------------------: |
| ![calculation-task-code](https://github.com/user-attachments/assets/891b2e76-1fc2-4e44-9c7d-54e3989549ec) | ![calculation-task-code-during-task](https://github.com/user-attachments/assets/dc5343c8-d54a-4f6e-93bf-5bfb7c342bc6) |

</details>

---

## 6. Define Custom Data to Record

LUIDA allows you to define and calculate custom data to record (e.g., isRed value sent when clicking answer buttons, timer value sent after each trial, and experimental conditions for each trial).

For details on each setting, see the [documentation](/Documentation_JA.md#defining-custom-data-to-record).

<img width="787" height="219" alt="image" src="https://github.com/user-attachments/assets/893571d5-30c0-441a-aff7-c237ce926ce1" />

Follow the instructions below to configure.

1. Find the `LUIDA-DataCollector` game object in the scene and double-click `Script Asset` in its Inspector (see figure below). The script editing screen will open.
   <img width="636" height="347" alt="data-collector" src="https://github.com/user-attachments/assets/b36ae892-08b8-4ce7-aa89-c0f405faad8b" />

2. Replace the contents with the following script and save the file:

```JavaScript
return {
  stateLog: COLLECTED_DATA["stateLog"], // The state at the time this data is recorded
  cond: CONDITION || {}, // Conditions for the corresponding trial (includes depth, request, font, text)
  ans: $.getStateCompat('this', 'isRed', 'boolean') ? "R" : "B", // answer (red or blue)
  time: COLLECTED_DATA['timer'] // time taken to answer
};
```

- The isRed value sent to LUIDA-DataCollector via CCK component is retrieved with `$.getStateCompat('this', 'isRed', 'boolean')`
- Timer value sent via `SendDataToCollector("timer", $.state.timer);` is retrieved with `COLLECTED_DATA['timer']`
- Current trial's experimental condition is retrieved with `CONDITION[variable_name]`

---

## 7. Preparation Before Uploading

<details>
<summary>Follow the steps below to configure</summary>

1.  Open `Cluster > Settings` from Unity's menu bar and check "Use beta features".
    ![Enable beta features](https://github.com/user-attachments/assets/af786e5e-07fe-4126-b350-1ed7c0401ecd)
2.  **Local test play**: Press the play button in Unity Editor to verify that the entire experiment flow, each state's behavior, and object behaviors work as intended.
3.  Save the scene.

</details>

---

## 8. Upload to cluster

<details>
<summary>Follow the steps below to configure</summary>

1.  Follow the official cluster documentation [Steps to upload a world](https://creator.cluster.mu/2020/03/28/%E5%88%B6%E4%BD%9C%E3%81%97%E3%81%9F%E3%83%AF%E3%83%BC%E3%83%AB%E3%83%89%E3%82%92%E3%80%8Ccluster%E3%80%8D%E3%81%AB%E3%82%A2%E3%83%83%E3%83%97%E3%83%AD%E3%83%BC%E3%83%89%E3%81%99%E3%82%8B/) to upload the created scene as a cluster world.
    - After uploading, enter the world yourself and experience the entire experiment as a participant to verify operation.
    - At this point, normal avatar selection is available, but once you configure avatar settings in the LUIDA web console (described later) and the experiment is officially published, participants will join with the specified avatar (or hidden avatar).
2.  If bugs are found after uploading, we recommend using the "test upload" feature after fixing in the Unity project to verify in cluster's [Test Space](https://creator.cluster.mu/2024/05/24/testspace/). This allows testing without affecting the published world.
    - Once fixes are complete and verified in the test space, perform the normal world upload again.
3.  Along with verifying operation in the cluster world, check the LUIDA web console to confirm that experiment data (questionnaire responses, other configured log data, etc.) is correctly recorded and displayed.
    ![Data verification example in web console](https://github.com/user-attachments/assets/9db65b18-7a6e-412d-8908-54a2995bfdb9)

</details>

---

## 9. Register World ID

<details>
<summary>Follow the steps below to configure</summary>

1.  Log in to the cluster official website in your web browser and verify that the uploaded experiment world appears in the list on the "My Content" page (or "World" management screen).
2.  Select the relevant world and open the world details page. The alphanumeric string at the end of the page URL is the World ID (e.g., the `XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX` part of `https://cluster.mu/w/XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX`). Copy this World ID.
    ![World verification on cluster My Content screen](https://github.com/user-attachments/assets/44821568-fa20-4f75-9cf1-c49f38b9d4e5)
3.  Open the experiment information edit screen on the LUIDA web console and register/save the copied World ID in the designated field (e.g., "World ID").
    ![World ID registration in web console](https://github.com/user-attachments/assets/c5003a53-3ea0-4b72-aa92-ea37e1e2a1d9)

</details>

---

## 10. Avatar Settings

<details>
<summary>Follow the steps below to configure</summary>

For the experiment in this tutorial, you don't need to configure participant avatars,
but if you want to hide avatars or use specific avatars for the experiment, follow these steps:

1.  In the LUIDA web console, click the "Add World-Avatar Set" button in the `Avatar Settings` section on the experiment settings page.
2.  Enter the experiment world ID you registered earlier in the `World ID` field.
3.  Configure one of the following and click "Submit" to save:

    - **To hide avatars**: Only check `Hide Avatar`. Other fields can be left empty.
    - **To specify a particular avatar**: Only check `Assign Avatar` and enter/upload the following information:
      - `Avatar Name`: Avatar name (for management)
      - `VRM Version`: VRM model version to use (e.g., `0.x` or `1.0`)
      - `Upload VRM`: Avatar VRM file
      - `Upload Thumbnail (PNG)`: Avatar thumbnail image (PNG format recommended)

    ![Avatar settings screen in LUIDA web console](https://github.com/user-attachments/assets/4af44d34-eb78-4080-9241-09459874c1a6)

</details>

---

## 11. Wait for Publication on LUIDA

Once all the above settings are complete, your experiment is ready to be listed on the LUIDA participant recruitment world. Please allow a few days for publication.

\*Note: In the future, we plan to improve LUIDA's experiment information update process so that newly registered experiments can be published more quickly (e.g., within one day).

# LUIDA Implementation Template Tutorial

This tutorial guides you through implementing an experiment on LUIDA using this implementation template, by building a simple experiment step by step.

## Overview of Using This Implementation Template

0.  Installation, account creation, and learning about CCK
1.  Create a new experiment on the LUIDA Web Console and fill in the required information
2.  Initial setup of this implementation template (Unity)
3.  Configure data collection on in-scene objects
4.  Test locally (in Unity Editor) & preparation before uploading
5.  Upload to cluster as a world and verify operation
6.  Final settings on LUIDA Web Console (World ID registration and avatar settings)
7.  Wait for publication

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

In this experiment, we will examine the priming effect. Participants first watch a randomly selected video — either "an elderly person walking" or "a young person running." After watching the video, two bridges appear: one that looks safe and one that looks dangerous. The participant walks across one of the bridges. This experiment examines whether the content of the video influences the participant's choice of bridge.

- Experiment procedure
  - Start -> Demographics questionnaire -> Watch video (randomly selected) -> Choose a bridge -> End
- Recorded data
  - Which video was shown (`isElderVideo`: true / false)
  - Which bridge was crossed (`isSafeRoad`: true / false)
- Questionnaire
  - Demographics (Age)

<img width="902" height="503" alt="スクリーンショット 2026-02-12 022027" src="https://github.com/user-attachments/assets/f519c528-9af6-48ea-8cf6-1c23ce2b19e8" />

---

## 1. Register Experiment Information on Web Console

<details>
<summary><h3>Follow these steps to register and configure</h3></summary>

1.  Open the [LUIDA Web Console](https://luida.cluster.mu/experiments).
2.  **Create a new experiment**: Click "+New Experiment" and register the basic experiment information with the following values:
    1. Title: `Priming Effect Experiment`
    2. Participation requirements: (optional)
    3. Reward: `0`
    4. Image URL: Any string (e.g., `https://example.com/image.png`)
    5. World ID: Leave blank for now
    6. Room capacity: `1`
    7. Status: `Testing`

|                                           New Experiment Button                                           |                                     Experiment Registration Form                                      |
| :-------------------------------------------------------------------------------------------------------: | :---------------------------------------------------------------------------------------------------: |
| ![New experiment button](https://github.com/user-attachments/assets/cc1cc6c5-b0c9-4a48-bf08-daf5d345e04a) | ![Registration form](https://github.com/user-attachments/assets/a1c19c68-bed3-4b46-8d6e-5abb034f623b) |

3.  **Navigate to experiment details page**: Once the experiment is registered, click on that experiment's row from the home page to access its details page. Verify the information you just registered.
4.  **Create a questionnaire**: Scroll down and perform the following operations in the questionnaire registration form:
    1. Click "Add Questionnaire" button -> Do not select a template, enter the title as `Demographics` -> Click "Add" button
    2. Click the "Question List" button for the added questionnaire and add the following question:
       - Title: `Age`, Question Type: `Text Input`, Required: Yes

<img width="2096" height="963" alt="LUIDA-sample-questionnaire-configuration" src="https://github.com/user-attachments/assets/e7133ff1-ba19-44d3-8192-fc1c1d5f0c75" />

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
3.  Open the sample scene `Assets/_Experiment_/Scenes/Sample/Priming_incomplete.unity` and press the Play button in the Unity Editor to get a feel for the scene.
4.  After reviewing the scene, go to `LUIDA > Scene > Duplicate current scene` from Unity's menu bar. In the dialog that appears, enter your own name to create a new scene. You will work on this new scene from now on.

    <img width="1038" height="301" alt="スクリーンショット 2026-02-12 023657" src="https://github.com/user-attachments/assets/77a1c3af-2863-43eb-9768-75dc473f07ff" />
    <img width="323" height="131" alt="スクリーンショット 2026-02-12 023710" src="https://github.com/user-attachments/assets/ddef3341-d53b-42c8-a950-eefbad3ee3dc" />

5.  Next, link your cluster account to this Unity project. The procedure is as follows (see the image for reference):

    1. From the Unity menu bar, open the `LUIDA > Configure experiment identifiers` window.
    2. Click the `Create Access Token` button to open the cluster access token creation page in your web browser.
    3. Click the `Create Token` button, and then copy and paste the displayed token into the `Access Token` field in the `Configure experiment identifiers` window that you just opened in Unity.

    ![cluster access token registration procedure](https://github.com/user-attachments/assets/e3f23566-e1b0-459b-8586-58ee897b7616)

6.  With the identifiers configuring window still open, enter the Experiment ID you copied from the web console earlier into the `Experiment ID` field.

    ![Luida Editor Experiment ID registration screen](https://github.com/user-attachments/assets/45afbd6b-502b-40ef-a1f7-b2c2c8ef9c30)

7. If the Verify Token is not generated yet, click the `Generate a new verify token` button.

   ![Verify Token generation](https://github.com/user-attachments/assets/f4fcf341-d44a-4ad1-ab31-147f01a3dde0)

8. Close the identifiers configuring window.

</details>

---

## 3. Complete the Experiment (Configure Data Collection)

In the new scene, you will configure components on in-scene objects so that data can be collected and uploaded to LUIDA's backend.

<details>

<summary><h3>Configure StartVideoButton</h3></summary>

Select the in-scene game object `StartVideoButton` and find its `Global Trigger Lottery` component.

1. Click the `+` button twice to add two choices.
2. Configure each choice with the following triggers:
   - Choice 1: `Global` trigger, Key = `isElderVideo`, Value Type = `Bool`, Value = `true`
   - Choice 2: `Global` trigger, Key = `isElderVideo`, Value Type = `Bool`, Value = `false`

This makes the video selection random when the button is pressed, and records which video was shown as `isElderVideo`.

<img width="786" height="695" alt="スクリーンショット 2026-02-12 020601" src="https://github.com/user-attachments/assets/a7ce183a-be0a-46ec-86cb-b7412f6cbcc0" />

</details>

<details>

<summary><h3>Configure SafeRoadCheckPoint</h3></summary>

Select the in-scene game object `SafeRoadCheckPoint` and find its `On Collide Item Trigger` component.

1. Click the `+` button 3 times to add 3 triggers, and set the values as shown in the figure below.
2. Additionally, add the following 2 components:
   - `Luida Process Data And Save To Collection Gimmick`
   - `Luida Upload Collected Data Gimmick`

After configuration, it should look like the figure below:

<img width="779" height="826" alt="スクリーンショット 2026-02-12 020954" src="https://github.com/user-attachments/assets/bc1761f1-f780-400b-ab68-f7fe9875b360" />

Figure legend:
- **Red squares**: Data collection — when this checkpoint is passed, record `isSafeRoad=true`
- **Yellow squares**: Save data trigger and gimmick — when this checkpoint is passed, save data to collection
- **Green squares**: Upload data trigger and gimmick — when this checkpoint is passed, upload collected data

</details>

<details>

<summary><h3>Configure DangerousRoadCheckPoint</h3></summary>

Select the in-scene game object `DangerousRoadCheckPoint` and configure it with the same components and triggers as `SafeRoadCheckPoint`.

**The only difference**: Set the value of `isSafeRoad` to `false`.

<img width="779" height="834" alt="スクリーンショット 2026-02-12 020959" src="https://github.com/user-attachments/assets/e36ea3ac-1dc5-41da-8984-252c96614924" />

</details>

<details>

<summary><h3>Edit the LUIDA-DataCollector Script</h3></summary>

Select the in-scene game object `LUIDA-DataCollector` and double-click the `Script Asset` in its Inspector component (see figure below). The script editing screen will open.

<img width="668" height="350" alt="スクリーンショット 2026-02-12 030242" src="https://github.com/user-attachments/assets/3ba4959d-0be6-4398-a622-7355fbe3d18f" />

Replace the contents with the following script and save the file:

```JavaScript
return {
    video: $.getStateCompat("global", "isElderVideo", "boolean") ? "Elder" : "Young", // Which video was shown
    road: $.getStateCompat("global", "isSafeRoad", "boolean") ? "Safe" : "Danger", // Which bridge was crossed
};
```

This script defines the format of the collected data. It retrieves the `isElderVideo` and `isSafeRoad` values sent to Global via CCK components using `$.getStateCompat`.

</details>

<details>

<summary><h3>Add a Questionnaire</h3></summary>

1. Right-click in the Hierarchy window and select `LUIDA > Questionnaire`.
2. In the dialog that appears, enter `1` for `qID` and press the `Create` button.

A questionnaire game object will be generated.

3. Click the generated questionnaire object and add the following components in the Inspector:
   - `Global Logic`
   - `Set Game Object Active Gimmick`
4. Set the values of the added components as shown in the figure below.

<img width="730" height="623" alt="スクリーンショット 2026-02-12 022700" src="https://github.com/user-attachments/assets/aad277ef-24ea-4b72-8fc6-47d530896143" />

This configuration makes the questionnaire automatically hide itself after the participant finishes answering it.

</details>

---

## 4. Preparation Before Uploading

<details>
<summary>Follow the steps below to configure</summary>

1.  Open `Cluster > Settings` from Unity's menu bar and check "Use beta features".
    ![Enable beta features](https://github.com/user-attachments/assets/af786e5e-07fe-4126-b350-1ed7c0401ecd)
2.  **Local test play**: Press the play button in Unity Editor to verify that the entire experiment flow (video playback, bridge selection, questionnaire display, etc.) works as intended.
3.  Save the scene.

</details>

---

## 5. Upload to cluster

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

## 6. Register World ID

<details>
<summary>Follow the steps below to configure</summary>

1.  Log in to the cluster official website in your web browser and verify that the uploaded experiment world appears in the list on the "My Content" page (or "World" management screen).
2.  Select the relevant world and open the world details page. The alphanumeric string at the end of the page URL is the World ID (e.g., the `XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX` part of `https://cluster.mu/w/XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX`). Copy this World ID.
    ![World verification on cluster My Content screen](https://github.com/user-attachments/assets/44821568-fa20-4f75-9cf1-c49f38b9d4e5)
3.  Open the experiment information edit screen on the LUIDA web console and register/save the copied World ID in the designated field (e.g., "World ID").
    ![World ID registration in web console](https://github.com/user-attachments/assets/c5003a53-3ea0-4b72-aa92-ea37e1e2a1d9)

</details>

---

## 7. Avatar Settings

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

## 8. Wait for Publication on LUIDA

Once all the above settings are complete, your experiment is ready to be listed on the LUIDA participant recruitment world. Please allow a few days for publication.

\*Note: In the future, we plan to improve LUIDA's experiment information update process so that newly registered experiments can be published more quickly (e.g., within one day).

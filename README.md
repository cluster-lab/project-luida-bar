# LUIDA's Implement Template for Experiment Worlds

To use this implement template, **please avoid directly cloning this repository.** Follow the steps in the [Getting Started](#getting-started) section to download and initialize this implement template. Please also try the tutorial we provided.

本実装テンプレートを使用される場合、**このレポジトリを直接cloneしないでください。**[Getting Started](#getting-started)セクションに従って本実装テンプレートのダウンロードと初期化を行ってください。チュートリアルもぜひ試してください。

- [Main Features](#main-features)
<!-- - [Getting Started](#getting-started) -->
- [Tutorial (JA)](https://github.com/cluster-lab/project-luida-bar/blob/exp-template/Tutorial_JA.md)
- (Under construction) [Tutorial (EN)](https://github.com/cluster-lab/project-luida-bar/blob/exp-template/Tutorial_EN.md)
<!-- - [Documentation (under construction...)](#documentation-under-construction) -->

-----

# Main Features

### Experimental variables & trials management
This implement template automatically determine number of trials & condition of each trial by registered within/between-subject variables. You can complete the setup within the provided editor window.

We also provided a template script to implement decision of the experimental conditions from between-subject variables.

本実装テンプレートは、登録された参加者内/参加者間変数に基づいて、自動的に試行の数と各試行における実験条件を決定します。
その設定は提供された設定画面から行うことができます。

また、参加者間変数から実験条件を決定するためのスクリプトのテンプレートも提供しています。

![variable-settings](https://github.com/user-attachments/assets/1cf257e1-71f7-49de-95a9-d38e3661b655)

### State management
This implement template follows a State design pattern. We have prepared default states and their transitions for you to use without additional edition.
You can still make your customization, including adding/removing/skipping/repeating a state or enable auto transition in xx seconds.

本実装テンプレートは、ステートデザインパターンに従っています。デフォルトのステートとその遷移が用意されており、追加編集なしで使用可能です。
ご自身でステートの追加・削除・スキップ・繰り返し・XX秒後に自動遷移などの設定もカスタマイズ可能です。

![state-transition](https://github.com/user-attachments/assets/050dee88-c603-407c-b60f-1b1b59d52840)

### Manage game objects that listen to state transitions and access experiment conditions
This implementation template allows you to create game objects that can perform actions based on state transitions or access experimental conditions.
Using the provided settings screen, you can edit their behaviors through the GUI, and spaces for writing scripts are also available.

本実装テンプレートでは、ステートの遷移に応じた動作や、実験条件へのアクセスが可能なゲームオブジェクトを作成できます。
提供された設定画面を使用して、GUI上で動作を簡単に編集することができ、またスクリプトを記述するための枠も用意されています。

![states-and-objects](https://github.com/user-attachments/assets/3411b3a8-988b-4462-a013-c719dc024117)

### Questionnaire generation
You don't need to create game objects for each question or answer. Just register your questionnaire on LUIDA's web console, and paste its ID the designated field on the provided editor window in this implement template. Gameobjects for each question and answer will be automatically generated on cluster during the exact experiment session.

質問紙の質問や回答ごとにゲームオブジェクトを作成する必要はありません。
LUIDA専用のウェブコンソールに質問紙内容を登録し、そのIDを本実装テンプレートに提供された設定画面の指定フィールドに貼り付けるだけで、cluster上の実験実施中に自動的にゲームオブジェクトが生成されます。

![questionnaire-registration](https://github.com/user-attachments/assets/c3522829-31c6-44c1-a248-38c472acbd2d)

### Data recording
During the exact experiment session, Cluster continuously records players' positions, poses, actions, to name a few. These data will be formatted and display on the web console.

Meanwhile, you can also setup recorders for customized data inside this template in advance.
The collected data will be listed on LUIDA's web console for you to confirm and download.

実験実施中、clusterはプレイヤーの位置、姿勢、動作などを継続的に記録します。これらのデータはLUIDA専用のウェブコンソール上で整形・表示されます。

同時に、カスタマイズなデータ記録を事前に本実装テンプレート内で設定することも可能です。
収集されたデータはLUIDA専用のウェブコンソールから確認・ダウンロードできます。

![image](https://github.com/user-attachments/assets/089340d0-dec0-487b-9be1-51b4cfceca2f)
![image](https://github.com/user-attachments/assets/0d997e6c-9c1b-456d-babc-cb8400a1ef86)

<!--
-----

# Getting Started

### English
1. Download from the [newest release](https://github.com/cluster-lab/project-luida-bar/releases).
2. Open the downloaded Unity project, **ignore the error at the first time, and import the following packages published by KaomoLab**.
    - [**CSCombiner: Combine multiple ClusterScripts of one item inside Unity Editor**](https://vkao.booth.pm/items/5924956) (ver1.01)
    - [**CSEmulator: Run ClusterScripts inside Unity Editor**](https://vkao.booth.pm/items/5111235) (newest version)
3. Issue an access token and register it for this Unity project (Follow the steps as shown in the picture below).
![cluster-access-token-registration](https://github.com/user-attachments/assets/aeec56a4-ed78-41b2-bb21-d519c659c0d5)
4. Register URL  `https://luida-web-next.vercel.app/api/cluster` for Cluster's `callExternal` feature, and register the generated verify token for this implement template (Follow the steps as shown in the picture below).
![register-call-external-url](https://github.com/user-attachments/assets/f64e75df-93f2-4b1a-9b3a-36216405feb7)
    - When opening Window > Luida Editor, if the following screen appears, please first enter the name of the scene for the experiment you are going to implement.
![image](https://github.com/user-attachments/assets/be969afc-0dc8-43a3-995b-ae8f420a5e5b)


### 日本語
1. [最新リリース](https://github.com/cluster-lab/project-luida-bar/releases)からダウンロードします。
2. ダウンロードしたUnityプロジェクトを立ち上げ、**最初はエラーを無視し、立ち上げたら以下のパッケージをインポートします**。
    - [**CSCombiner: Cluster Scriptを Unity Editor 上で結合するツール**](https://vkao.booth.pm/items/5924956) (ver1.01)
    - [**CSEmulator: Cluster Scriptを Unity Editor 上で再生できるようにするツール**](https://vkao.booth.pm/items/5111235) (最新バージョン)
3. アクセストークンを発行し、Unityプロジェクトに登録します（下の画像に示された通りに行ってください）。
![cluster-access-token-registration-jp](https://github.com/user-attachments/assets/c06f43c6-3412-4462-92a9-ac3576252e99)
4. clusterの外部通信機能用のURLを登録し、生成されたトークンを本実装テンプレートに登録します（下の画像に示された通りに行ってください）。
![register-call-external-url](https://github.com/user-attachments/assets/f64e75df-93f2-4b1a-9b3a-36216405feb7)
    - Window > Luida Editor を開く際に、以下の画面が表示された場合、まずはご自身が実装する実験のシーン名をご入力ください。
![image](https://github.com/user-attachments/assets/be969afc-0dc8-43a3-995b-ae8f420a5e5b)

-----

# Documentation (under construction...)

## Getting started

### Register your experiment on the web console

1. Access the web console with this URL: https://studious-doodle-4k9pon4.pages.github.io/
2. Login (For now, the login system is not fully implemented yet, so just fill in any text)
3. Click the Register Experiment button, and you will be redirected to the experiment detail page. A unique eID is created and displayed on the page. You will need to paste this eID in the template Unity project.

### Prepare the template

1. Clone this Unity project from branch `exp-template`
2. Duplicate the template scene and rename it
3. Open `Assets\_ExpWorld_\ExpSettings\ExpIdentifiers.js` and paste your experiment's eID to the value for the constant `expID`.
![スクリーンショット 2024-08-08 000712](https://github.com/user-attachments/assets/26798130-3215-4171-b18b-1ed96dc7c7a5)

## How to use

### Set/Edit Experiment Variables

Register within/between-subject variables with an editor window, so that the number of trials and each trial's experimental condition will be automatically determined.
If your experimental conditions are based on between-subject variables, there is a template script for you to implement how to determine them (e.g. randomly assign, calculate from questionnaire answers, etc.) 

1. From top menu, select `Window > Experiment Variables Editor` to open the experiment variables editor window. Notice that changes in this editor window only work for the currently opened scene.
![スクリーンショット 2024-08-07 231217](https://github.com/user-attachments/assets/e21bef41-9d10-4dc9-a1e4-a8aceb89fe04)
2. If not yet exists, Create a new variables asset for this newly created scene by clicking the `Create New Variables Asset` button
![スクリーンショット 2024-08-07 231227](https://github.com/user-attachments/assets/d5eb2cde-4a5b-4a4c-99b2-21da608e405b)
3. Fill in the `Length` fields with integers representing how many variables your experiment requires for within-subject and/or between-subject conditions, and then set their actual values.
![スクリーンショット 2024-08-07 232304](https://github.com/user-attachments/assets/8568966b-9c6a-4f41-9cf3-190c2b18c952)
4. If your experiment needs some calculation to decide the between-subject condition, click the `Retrieve/Create Between Subject Condition Setter`. Then, a JavaScript asset is created in the displayed path. Edit it later to implement the calculation.
5. Set the value of the field `Trials Count per Condition` with how many times your experiment repeats the trial for each unique condition.
6. Click the `Apply Updated Variables` button to save the change before closing the window.

- Every time after updating the between-subject condition setter JavaScript asset, remember to open this editor window again, and then click the `Retrieve/Create Between Subject Condition Setter` and `Apply Updated Variables` buttons so that your change is applied to the scene.

### Set/Edit States and their Transitions during the Experiment

You can use the default states and transitions as they are, while customization (e.g. skip a state, enable auto transition in xx seconds, etc.) is available with an editor window.

1. From top menu, select `Window > State List Editor` to open the state list editor window. Notice that changes in this editor window only work for the currently opened scene.
![スクリーンショット 2024-08-07 230959](https://github.com/user-attachments/assets/ea7829e7-d4e6-423d-a791-8027ad81fe1a)
2. Basically you can leave these states as they are, while you can still make some editions. Your edit will be immediately reflected to the scene (the gameobject named `States`), so you don't need to click on any button to confirm or apply changes.
![スクリーンショット 2024-08-07 233636](https://github.com/user-attachments/assets/563e53ea-bf1c-4328-8af0-4ec341a4701c)

Explanation for each field:
- Transit destination state: the next state when the current state is exited
- Has Exit Time: Check it if this state should automatically be exited in a period of time
  - Exit Time: Set how many seconds from the beginning this state will be automatically transited to the next one
- Is Repeated: Check it if this state transits not to the next state but any other state before it.
  - Repeat destination state: Set which state to transit to instead of the next state.
  - Repeat Count: How many times this state transits to the Repeat destination state. If the times of this state repeating to the assigned state reached the value here, it will transit to the original next state on the next transition.
- There are also buttons to move a state upward, move a state downward, or remove a state. Some states are not allowed to be moved or removed, and for those that are allowed, please still be careful if you really need to move or remove any of them.

You can click the `Add State` button to add more states and move or remove them if necessary.

### Invoke State transition

If a current state does not have an exit time, it requires its transition to be explicitly invoke.
Invoke a global signal trigger with key `state_triggerTransition` from anywhere, then the state will transit to its Transit destination state or Repeat destination state.
![スクリーンショット 2024-08-08 100939 copy](https://github.com/user-attachments/assets/c1d4405a-f6ac-483a-9d97-27041f15e123)

### Implementation depending on States

You can have your CCK gimmick components, logic components or script listen to the global integer key `state_currentID` which represents current state's ID (you can confirm it on the State List Editor window). You can also listen to the global signal key `state_entered` or `state_exited`.

Also, you can add state-specific gameobjects into `EnabledObjects` under each state gameobject inside the `States` gameobject, as depicted in the screenshot below, so that these state-specific gameobjects are displayed only during the state they depend on (as `EnabledObjects` has a CCK component `Set Game Object Active Gimmick` attached):

<img width="637" alt="スクリーンショット 2024-08-14 15 45 32" src="https://github.com/user-attachments/assets/b0472dee-783d-408b-8ebf-9bfcf46fdef2">

If you need more customized state-specific executions, consider the following:
1. Open the State dependent object editor window:
![スクリーンショット 2024-08-08 094614](https://github.com/user-attachments/assets/0f5775d0-222c-485e-91a6-ace003b0f44e)
2. Click the Create New stateDependentObject button
![スクリーンショット 2024-08-08 094642](https://github.com/user-attachments/assets/6174831f-86b2-4135-b4cd-ccad50a98652)
3. Set the state you want this gameobject to dependent to, and also press the Duplicate Asset button to create a CCK script for it, then complete the implementation in the script.
![スクリーンショット 2024-08-08 094658](https://github.com/user-attachments/assets/e5785831-b3f0-4412-999f-dfb19f3401a6)

### Implementation depending on Condition

CCK gimmick or logic components cannot directly access to variables/conditions.
You will need to complete a condition-dependent implementation with CCK script.
Here is a recommended procedure to do so:
1. Create a gameobject from prefab `Assets\_ExpWorld_\Prefabs\ConditionManagement\ConditionDependentObject.prefab`
2. Duplicate JavaScript asset `Assets\_ExpWorld_\Scripts\ConditionManagement\ConditionDependentTemplate.js` and assign it to the gameobject's Scriptable Item.
3. Complete the implementation of the duplicated JavaScript asset.
![スクリーンショット 2024-08-08 010024](https://github.com/user-attachments/assets/157ca5fc-37eb-4e53-a1fe-3b045897628d)

### Set questionnaires

1. There are already Questionnaire objects in each state with a name including `Questionnaire` (e.g. `Questionnaire (pre-exp)`). You can disable or remove any unnecessary ones, or add a new one from the prefab `Assets\_ExpWorld_\Prefabs\Form\Form.prefab`.
2. You don't need to create game objects for each question or answer. Just register your questionnaire on the web console, retrieve its identifier `qID`, and paste it in the field marked with a red block in the image below. Game objects for each question and answer will be automatically generated on cluster during the exact experiment session.
![スクリーンショット 2024-08-08 032108](https://github.com/user-attachments/assets/6bbf1485-e4b2-4860-a04b-ee785c19e348)

### Data Recorder/Uploader

#### Initialize

1. Create a gameobject from prefab `Assets\_ExpWorld_\Prefabs\CustomDataRecorder\CustomDataRecorder.prefab`
2. Duplicate JavaScript asset `Assets\_ExpWorld_\Scripts\CustomDataRecorder\CustomDataRecorderCalculatorTemplate.js` and assign it to the gameobject's `CS Combiner` component's last field for cluster scripts.
![image (1)](https://github.com/user-attachments/assets/9aca381b-0cab-451a-b1c4-a39ec4117142)
3. Complete the implementation of the duplicated JavaScript asset.

#### Record and upload data

1. Invoke a global signal trigger with key `exp_recordCustomData` from anywhere to run the calculation and temporary save of the custom data.
2. Invoke a global signal trigger with key `exp_uploadCustomData` from anywhere to upload the temporary saved custom data.

The image below serves as an example:
![スクリーンショット 2024-08-08 103851](https://github.com/user-attachments/assets/0dbdf8f4-b2b3-4ef8-a22a-3ee3fae60388)

## Deploy

### Before Upload to cluster

1. Find any gameobject with the CS combiner component attached, and click the "全更新" button.
2. Open the Experiment Variables Editor window again, and then click the `Retrieve/Create Between Subject Condition Setter` and finally `Apply Updated Variables` buttons.

### Upload and test your experiment world

Just upload your world (https://docs.cluster.mu/creatorkit/world/upload-world/), simply enter it on cluster, and see if everything runs well!

We recommend making use of cluster's test space feature for more effective tests: https://creator.cluster.mu/2024/05/24/testspace/
-->

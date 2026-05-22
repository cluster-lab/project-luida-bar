# LUIDA's Implement Template for Experiment Worlds

If you are using this implementation template, please **do not clone this repository directly. Download it from the latest release instead.**
If you're ready to get started, please check the Main Features below first, then follow the Tutorial to give it a try.

本実装テンプレートを使用される場合、**このレポジトリを直接cloneしないで、最新リリースからダウンロードしてください。**
始めたい方は、↓の**Main Featuresを確認してから、チュートリアルに従って**試してください。

<!-- - [Getting Started](#getting-started) -->
- [Main Features](#main-features)
- [Documentation & Tutorials](https://luida-docs.vercel.app/)
<!-- - [Tutorial (JA)](https://github.com/cluster-lab/project-luida-bar/blob/exp-template/Tutorial_JA.md) -->
<!-- - (Under construction) [Tutorial (EN)](https://github.com/cluster-lab/project-luida-bar/blob/exp-template/Tutorial_EN.md) -->
<!-- - [Documentation (under construction...)](#documentation-under-construction) -->

-----

# Main Features

Most configuration lives in a single tabbed editor window opened from `LUIDA > Configure experiment automation` (Experiment Variables / State Machine / State-listening Items). Data collector and avatar registry have their own dedicated windows under the same `LUIDA` menu. All settings are scene-scoped, so each experiment scene keeps its own configuration.

ほとんどの設定は `LUIDA > Configure experiment automation` から開けるタブ式エディタウィンドウ（実験変数 / ステートマシン / ステート連動アイテム）に集約されています。データ収集とアバター登録は同じ `LUIDA` メニュー内の専用ウィンドウから設定します。すべての設定はシーン単位で保存されるため、実験シーンごとに独立した構成を持てます。

### Experimental variables & trials management
Register within/between-subject variables in the Experiment Variables tab — the template auto-derives the number of trials and the condition assigned to each trial from the factor combinations. Between-subject assignment is balanced across participants on the server side, and you can override the default logic with a provided template script.

実験変数タブで参加者内/参加者間変数を登録すると、要因の組み合わせから試行数と各試行の条件が自動的に決定されます。参加者間条件はサーバー側で参加者間の偏りが出ないようにバランス化されます。デフォルトのロジックを上書きしたい場合は、提供されたテンプレートスクリプトで条件決定処理をカスタマイズできます。

<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/385a0f1d-c737-4075-8058-25ed22274d15" alt="variable-settings-illustration" width="400"/></td>
    <td><img src="https://github.com/user-attachments/assets/14a79734-5585-4309-95f1-24339906b249" alt="variable-settings-screenshot" width="800"/></td>
  </tr>
</table>

### State management
The State Machine tab manages a state-pattern flow that already ships with sensible defaults, so you can run an experiment without editing anything. When you do need to customize, you can add, remove, skip, or repeat states, enable auto-transition after N seconds, and bind a questionnaire to a state by qID — all from the GUI.

ステートマシンタブでは、ステートデザインパターンに基づく既定の流れが用意されており、編集なしでもそのまま実験を実施できます。必要に応じて、ステートの追加・削除・スキップ・繰り返し、XX秒後の自動遷移、qIDによる質問紙の割り当てなどをGUIから自由にカスタマイズできます。

<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/f7ccd578-cae2-46c2-b70b-1e485c2c889f" alt="state-transition-graph-example" width="350"/></td>
    <td><img src="https://github.com/user-attachments/assets/150e0356-334c-438b-97f6-3d102d0b6a22" alt="state-machine-screenshot" width="850"/></td>
  </tr>
</table>

### State-listening items with GUI-driven actions and event handlers
In the State-listening Items tab, you can hook scene GameObjects to the state machine without writing scripts. For each item you can configure:
- **Per-state actions** that fire on state start / during the state / on state exit, optionally gated by an experimental condition (e.g. only run if `CONDITION['color'] === 'red'`).
- **Always-on event handlers** for lifecycle/interaction events such as `Start`, `Update`, `onCollide`, `onInteract`, etc.

Actions are picked from a catalog of templates (item transform, child toggling, participant control, avatar assignment, data collection, OSC, haptics, …), with a free-form "Customized Action" slot and a legacy raw-script foldout for anything not covered by the GUI.

ステート連動アイテムタブでは、シーン内のGameObjectをスクリプトなしでステートマシンに連動させられます。各アイテムについて以下を設定できます：
- **ステート単位のアクション**：ステート開始時／実行中／終了時に動作。実験条件によるゲーティング（例：`CONDITION['color'] === 'red'` の時のみ実行）も可能。
- **常時動作のイベントハンドラ**：`Start`、`Update`、`onCollide`、`onInteract` などのライフサイクル／インタラクションイベントに対応。

アクションはテンプレートカタログ（アイテムのTransform操作、子オブジェクト切替、参加者制御、アバター割り当て、データ収集、OSC、ハプティクスなど）から選択でき、カタログに無い動作向けに自由記述の「Customized Action」枠と、レガシーな生スクリプト用の折りたたみ欄も用意されています。

<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/3411b3a8-988b-4462-a013-c719dc024117" alt="Image 1" width="600"/></td>
    <td><img src="https://github.com/user-attachments/assets/d0cbfe0c-b678-4201-a65f-01178ea17c9f" alt="Image 2" width="600"/></td>
  </tr>
</table>

### Questionnaire generation
You don't need to create game objects for each question or answer. Just register your questionnaire on LUIDA's web console, and paste its ID into the questionnaire field of the state where it should appear. GameObjects for each question and answer are generated automatically on Cluster during the actual experiment session.

質問紙の質問や回答ごとにゲームオブジェクトを作成する必要はありません。
LUIDA専用のウェブコンソールに質問紙内容を登録し、そのIDを表示させたいステートの質問紙フィールドに貼り付けるだけで、cluster上の実験実施中に質問・回答用ゲームオブジェクトが自動生成されます。

![questionnaire-registration](https://github.com/user-attachments/assets/c3522829-31c6-44c1-a248-38c472acbd2d)

### Data recording
Cluster continuously records players' positions, poses, and actions during a session; those are formatted and displayed on the web console with no extra setup.

For custom data, the `LUIDA > Configure data collector` window provides a no-code builder: declare the labels you want to collect (Bool / Float / Integer / Vector2 / Vector3 / String), then assemble each output field from sources such as collected values, global state reads, arithmetic, or conditional expressions. A raw-JS code mode is available as a fallback. Pushing values, saving a row to the buffer, and uploading to the backend are all exposed as state-listening actions, so most recording flows can be wired up entirely from the GUI. Collected data is listed on the web console for review and download.

実験実施中、clusterはプレイヤーの位置、姿勢、動作などを継続的に記録し、追加設定なしでLUIDA専用のウェブコンソール上に整形・表示されます。

カスタムデータについては、`LUIDA > Configure data collector` ウィンドウのノーコードビルダーから設定できます。収集したいラベル（Bool / Float / Integer / Vector2 / Vector3 / String）を宣言し、各出力フィールドを「収集済み値の参照」「グローバルステート読み取り」「四則演算」「条件分岐」などのソースから組み立てます。GUIで表現しきれない場合は生のJSを書くコードモードも利用可能です。値のpush・1行ぶんのバッファ保存・バックエンドへのアップロードはステート連動アクションとして提供されているため、多くの記録フローはGUIだけで完結します。収集データはウェブコンソールから確認・ダウンロードできます。

<img width="866" height="673" alt="data-collector-config-screenshot" src="https://github.com/user-attachments/assets/3939c91e-277a-42c8-a610-30053ec3e6b3" />
<img width="1475" height="483" alt="data-collection-result-on-web-screenshot" src="https://github.com/user-attachments/assets/05477847-f80a-4a7a-9b31-8826b179a419" />

### Avatar management
The `LUIDA > Configure avatars` window maintains a per-experiment avatar registry. Registered avatars can be assigned to participants directly from the GUI via the "Assign avatar to participant" state-listening action, so swapping avatars at specific points in an experiment doesn't require any scripting.

`LUIDA > Configure avatars` ウィンドウで、実験ごとのアバター一覧を管理できます。登録したアバターは、ステート連動アイテムの「Assign avatar to participant」アクションからGUI経由で参加者に割り当てられるため、実験中の特定タイミングでのアバター切替もスクリプトを書かずに実現できます。

<img width="890" height="788" alt="avatar-config-window-screenshot" src="https://github.com/user-attachments/assets/2003d19c-70a2-4e20-90c0-b8d0ef8ae892" />

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

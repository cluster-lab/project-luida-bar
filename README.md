# LUIDA's Implement Template for Experiment Worlds

## Table of Contents

- [Main Features](#main-features)
- [Tutorial](#tutorial)
- [Documentation (under construction...)](#documentation-under-construction)

-----

# Main Features

#### Experimental variables & trials management
This implement template automatically determine number of trials & condition of each trial by registered within/between-subject variables. You can complete the setup within the provided editor window.

We also provided a template script to implement decision of the experimental conditions from between-subject variables.

本実装テンプレートは、登録された参加者内/参加者間変数に基づいて、自動的に試行の数と各試行における実験条件を決定します。
その設定は提供された設定画面から行うことができます。

また、参加者間変数から実験条件を決定するためのスクリプトのテンプレートも提供しています。

![image](https://github.com/user-attachments/assets/3c2994f4-5bc3-40a7-9812-29f4999d59d6)

#### State management
This implement template follows a State design pattern. We have prepared default states and their transitions for you to use without additional edition, while you can still make your customization (e.g. skip a state, enable auto transition in xx seconds, etc.) with an editor window.

本実装テンプレートは、ステートデザインパターンに従っています。デフォルトのステートとその遷移が用意されており、追加編集なしで使用可能です。
ただし、提供されたエディタウィンドウを使って、ステートを追加・削除・スキップ・繰り返したり、XX秒後に自動遷移を有効にしたりといったカスタマイズも可能です。

![image](https://github.com/user-attachments/assets/d59d3e5c-e30e-429d-b83a-2ebca1550eb7)

#### Manage gameobjects by states or experimental conditions
This implement template enables gameobjects to follow state transitions or experiment conditions. You can add such gameobjects from the provided editor window, and then access the attached scripts to edit them. The scripts are also provided with templates to help you implement smoother.

本実装テンプレートでは、ステートの遷移や実験条件に従うゲームオブジェクトを作成できます。
提供された設定画面からこれらのゲームオブジェクトを作成し、付属のスクリプトにアクセスして編集できます。
そのスクリプトをスムーズに実装するためのテンプレートも用意されています。

![image](https://github.com/user-attachments/assets/b4a2c257-3979-438c-af12-2140ad33d5c0)
![image](https://github.com/user-attachments/assets/93e9ee07-be0c-4b18-b165-27972d1eacc1)

#### Questionnaire generation
You don't need to create game objects for each question or answer. Just register your questionnaire on LUIDA's web console, and paste its ID the designated field on the provided editor window. Gameobjects for each question and answer will be automatically generated on cluster during the exact experiment session.

質問紙の質問や回答ごとにゲームオブジェクトを作成する必要はありません。
LUIDA専用のウェブコンソールに質問紙内容を登録し、提供された設定画面の指定フィールドにIDを貼り付けるだけで、cluster上の実験実施中に自動的にゲームオブジェクトが生成されます。

#### Data recording
During the exact experiment session, Cluster continuously records players' positions, poses, actions, to name a few. These data will be formatted and display on the web console.

Meanwhile, you can also setup recorders for customized data inside this template in advance.
The collected data will be listed on LUIDA's web console for you to confirm and download.

実験実施中、clusterはプレイヤーの位置、姿勢、動作などを継続的に記録します。これらのデータはLUIDA専用のウェブコンソール上で整形・表示されます。

同時に、カスタマイズなデータ記録を事前に本実装テンプレート内で設定することも可能です。
収集されたデータはLUIDA専用のウェブコンソールから確認・ダウンロードできます。

![image](https://github.com/user-attachments/assets/8e227057-d100-4e89-8b41-42f7d394557a)

-----

# Tutorial

Let's implement an experiment of hand redirection in this tutorial.

### Recommended preliminary knowledge

We recommend you to at least acquire some basic knowledges of the following:
- Unity
- JavaScript
- [Cluster Creator Kit (CCK)](https://docs.cluster.mu/creatorkit/)

### Preparation

1. [clusterアカウント作成](https://help.cluster.mu/hc/articles/115000827112)
2. [clusterに必要なバージョンのUnityのインストール](https://docs.cluster.mu/creatorkit/installation/install-unity/)
を行う。
3. 本実装テンプレートをCloneする

### Register experiment information on web console

1. ウェブコンソールを開く：https://cluster-lab.github.io/project-luida-web-console/
（GitHubアカウントを持たず、ウェブコンソールを開けない場合、先にGitHubアカウントを作っておく）

2. 登録してログインする

3. 実験の募集情報を登録する：
    1. 「Register New Experiment」リンクをクリックして、新しい実験の募集情報を登録します。
    2. 下の画像に示したフィールドを埋めます（チュートリアルのため任意の文字列で大丈夫です）。その他のフィールドは空白かデフォルト値のままにします。
<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/3ccc2bb5-cb02-495e-b876-da06d21b4b14" alt="Image 1" width="500"/></td>
    <td><img src="https://github.com/user-attachments/assets/f75c5f36-3030-4f72-bbf4-1259d42dac09" alt="Image 2" width="500"/></td>
  </tr>
</table>

4. Registerボタンを押すと、情報が保存され、次の画面に表示されます。そこで自動的に生成された実験の識別子（eID）を確認します。

![image](https://github.com/user-attachments/assets/758a26d4-0fda-47d4-8528-cb98f3f2f2c7)


5. 質問紙を登録する：
    1. 「Questionnaires」ボタンを押して、質問紙一覧画面に移動します。
    2. 「Register New Questionnaire」ボタンをクリックして、実験に使用する質問紙の登録画面に移動します。
        ![image](https://github.com/user-attachments/assets/a21f6e34-a220-4d7f-8956-374d96ad3cb2)
    3. 質問紙の内容を入力する：
        1. [身体化体験に関する質問紙]([https://rothnroll.de/download/VEQ-Questionnaire-jpJP4.pdf](https://sites.google.com/view/virtualembodimentquestionnaire/download-the-questionnaire))の中から「AC1 私の身体」と「CO1 私の動作 」の質問だけを登録します。
        2. 各質問に対し、「Add Question」ボタンを押して質問の枠を増やし、図に従ってフィールドを埋めます。
        3. すべての質問が入力できたら、「Register」ボタンを押して登録を完了します。
          ![image](https://github.com/user-attachments/assets/ab4d5a9d-8b26-4adf-a0c1-c5e31d19ac70)
    4. 質問紙を確認する：画面が遷移したら、登録された質問紙を確認します。

![image](https://github.com/user-attachments/assets/3f6dd78e-781c-4406-9d67-3c8b5cc0c258)

### Open the implement template

ダウンロードした実装テンプレートをUnity Hubから起動します。

### Link with your cluster account

1. トップメニューからCluster > 外部通信(callExternal)接続先URLを選択します。
![image](https://github.com/user-attachments/assets/9ac1311a-aa09-4e28-95a0-a2081e9883f4)
2. Webでトークン発行ボタンをクリックしてブラウザでトークン発行画面を開きます。そこで「トークン作成」ボタンをクリックし、表示されたトークンをコピーしておきます。
<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/2c0d3212-e301-417d-a594-8ed1f7dec08f" alt="Image 1" width="400"/></td>
    <td><img src="https://github.com/user-attachments/assets/4395e140-b709-48ee-b659-52e6a93009fa" alt="Image 2" width="400"/></td>
    <td><img src="https://github.com/user-attachments/assets/2b5ca13b-14cf-4c77-ae06-ed7ca7792705" alt="Image 2" width="400"/></td>
  </tr>
</table>

3. Unityに戻り、トークンを貼り付けて、「このトークンを使用」ボタンを押します。

![image](https://github.com/user-attachments/assets/df24aab2-684f-4f5b-b287-b451f3c65f6d)

### Retrieve verify token for external call

1. アカウント紐づけ後に現れる画面で、「URLの登録」に以下のURLを」貼り付けます：`https://script.google.com/macros/s/AKfycbyamdYZGjweG65Dkykdw1oT7MxU4ZXoeqPDT3csW1M2mS3jj8gq9kZzO2iKhSBUOfx0Zg/exec`
2. 表示された「verify用トークン」をコピーしてどこかに保存しておきます。

![image](https://github.com/user-attachments/assets/e780e28d-4427-426d-9dc6-fde4d12b6120)
![image](https://github.com/user-attachments/assets/8c5bea04-d868-4f8c-a664-ae8ae3abcf52)

### Register experiment ID and verify token for external call
1. From the top menu, open `Window > Luida Editor`.
![image](https://github.com/user-attachments/assets/ff78908a-2277-4a07-a37f-3a1502146343)
2. Input a name for your new scene and click the 'Create and open scene' button.
![image](https://github.com/user-attachments/assets/21b1f40a-4367-4448-8277-244593682525)
3. In the `Experiment Identifiers` tab, fill in `Experiment ID` with the `eID` displayed on the web console.
4. In the `Experiment Identifiers` tab, fill in `Token` with the verify token for external call you just copied.
![image](https://github.com/user-attachments/assets/e7229049-3d8d-4cee-a5d5-19f5f70a2168)

### Setup experiment variables & trials count
1. Open `Window > Luida Editor` and switch to the `Experiment Variables Editor` tab.
2. Fill in the fields following the image below
    1. Name: `gain`
    2. Values (comma-separated): `0.75,0.8,0.85,0.9,0.95,1,1.05,1.1,1.15,1.2,1.25`
![image](https://github.com/user-attachments/assets/15fde214-9fc0-4e27-9d3c-aaab31a46863)
3. Click the 'Apply Updated Variables' button to save the changes
4. Save the scene.

### Adjust state transitions

Open `Window > Luida Editor` and switch to the `State List Editor` tab.

The state transitions in this tutorial will be like this: 
```
Start → Instruction → Practice - Task → Practice - Questionnaire → Practice - Rest → Preparation → Trial - Task → Trial - Questionnaire → Trial - Rest → AfterTrials → Questionnaire (post-exp) → End
```

Among them,
- `Practice - Task → Practice - Questionnaire → Practice - Rest` repeats 3 times
- `Trial - Task → Trial - Questionnaire → Trial - Rest` repeats `number of variable `gain`'s values ×2` times

Edit the transition in the editor window:

- Start: Set `Transit destination state` to `Instruction`
- Acclimatization: Press the `Remove` button to remove it
- Questionnaire (pre-exp): Press the `Remove` button to remove it
- Practice - Rest:
    - Check `Has Exit Time`, and set `Exit Time` to `5` (to automatically trigger state transition after 5 seconds)
    - Check `Is Repeated`, and set `Repeat destination state` to `Practice - Task`, and set `Repeat Count` to `3` (to repeat the practice session 3 times)	
- Trial - Rest:
    - Check `Has Exit Time`, and set `Exit Time` to `5` (to automatically trigger state transition after 5 seconds)
    - (The repetition for `Trial` states are controlled by the variables and trials count we setup in the `Experiment Variables Editor` tab, so we don't make additional setup here for the repetition)

![image](https://github.com/user-attachments/assets/972b97af-3b00-42d8-b0d5-82039d5d39dd)

### Prefabs for the tutorial

Take a look at the prefabs under `Assets/_Experiment_/Prefabs/Sample_HR/`.
These are for the sample scene `Sample_HR`, and we will use them in this tutorial as well.

#### NextStateButton
A button to trigger transiting to the next state when clicked.

It is attached with a CCK component `Interact Item Trigger`.
According to its value, when this button is clicked, it broadcast a Signal with the key `state_triggerTransition` globally.
This implement template listens to this Signal and trigger a transition to the next state.

![image](https://github.com/user-attachments/assets/2ec1b241-9ee3-4b62-8c00-95dc82788118)

#### Message

Simply a large message panel.
You can use it to give participants instructions.
Edit its child gameObject's `TextView` component to change its content.
![image](https://github.com/user-attachments/assets/b3401797-2c57-4036-971c-fa0bfce4cef5)

#### RightHand

Move into the `RightHand` folder to find the `RightHand` prefab.
A small sphere collider is attached on the tip of the index finger.

![image](https://github.com/user-attachments/assets/eb3d93d2-2ac9-4811-a503-57d43d81271e)

### Add objects for each state

To add gameobjects for each state, open `Window > Luida Editor` and switch to the `Objects Manager` tab.

Here is a video to show how to add a gameobject (not scriptable) from a prefab:
https://github.com/user-attachments/assets/06f8656e-7f9d-42a9-8830-b6586e9481ca

Let's add gameobjects for each state according to the following specification:

- **Start**: NextStateButton × 1 (Change text to `Start`)
- **Instruction**: NextStateButton × 1 (Change text to `Practice`), Message × 1 (Change text to an instruction of the practice session)
        <details>
            <summary>Instruction example</summary>
            ```text
            これからは、右手の人差し指で緑の玉を触って、
            質問に答える、というタスクを行っていただきます。
            まずは何回か練習しましょう。
            準備ができたら、設定画面でコントローラを非表示にし、
            前を見て「練習」ボタンを押してください。
            ```
        </details>
- **Practice**: explained later
- **Preparation**: NextStateButton × 1 (Change text to `Start`), Message × 1 (Change text to an instruction of the trial session)
        <details>
            <summary>Instruction example</summary>
            ```text
            練習（3回）は以上になります。
            ここからは本番です。
            同じ手順でタスクを22回行ってください。
            準備ができたら、前を見て開始ボタンを押してください
            ```
        </details>
- **Trial**: explained later
- **AfterTrials**: explained later
- **Questionnaire (post-exp)**: explained later
- **End**
  - Message × 1 (Change text to an instruction of leaving the experiment)
    <details>
        <summary>Instruction example</summary>
        ```text
        実験は以上になります。
        ご参加いただきありがとうございました！
        謝礼のcluster pointは後日に付与します。
        目の前のゲートに潜って退室してください。
        ```
    </details>
  - World gate prefab (`Assets/ClusterGAMEWORLDCENTER/Prefabs/WorldGateToClusterLobby.prefab`) × 1. After added:
    - Set the position to (0, 0, 1.5)
    - Delete its child gameobject `SignBoard`
    - On its child gameobject `WorldGate`, set the value of field `World Or Event Id` to `006d765e-f961-435b-a183-77c35a42e241` (World ID of LUIDA recruitment world)

![image](https://github.com/user-attachments/assets/6cc12262-b05f-468d-ab5d-f266be6bde95)

### Implement main trials

タスク内容：
1. 試行開始：緑の玉が原点（頭の下30センチ＋前30センチ）に戻る
2. リセット：実験参加者が原点にある緑の玉に触れる。すると緑の玉が目標地点（頭の下30センチ＋前60センチ）に移動する。
3. リダイレクション中のリーチングタスク：参加者が腕を伸ばし、もう一度その玉に触れる。腕を伸ばしている間、バーチャルの手がリダイレクションを受けている（手の位置にゲインがかかっており、実際の手の位置からズレている）
4. 試行後（質問の表示）：「バーチャルの手が実身体の手より速いか？遅いか」の質問文と、二択の回答ボタン「速い」と「遅い」が表示される
5. 次の試行へ：実験参加者がいずれかのボタンを選択したら、数秒の休憩の後、次の試行に移行する

This experiment requires changing the gain on the virtual hand for each trial, so we need objects that can access the values of the experiment condition `gain`.
The following video shows how to create such condition dependent objects.
https://github.com/user-attachments/assets/c58aa9c0-7562-40cb-952a-6c3b767f099d

Follow the steps below:

#### Create a Task Manager object
1. Refer to the video above to create a condition dependent object named `TaskManager` in state `Trial - Task`. Confirm that the gameobject and a cluster script asset `TaskManager.js` are generated.
2. Edit `TaskManager.js` as follows
    <details>
    
    <summary>Replace the code inside function `init`</summary>
    
    ```
    $.state.timer = 0; // タイマーの初期化
    $.state.isTouchable = true; // 緑の玉を触れられるようにするフラグ
    $.state.handOffset = new Quaternion().setFromEulerAngles(new Vector3(0, 90, 0)); // 実身体の手（コントローラ）とバーチャル手の回転の差を補正するオフセットを設定する
    ```
    
    </details>
    
    <details>
    
    <summary>Replace the code inside function `onConditionChanged`</summary>
    
    ```
    // 変数の値の初期化：プレイヤー、原点、目標地点
    if (!$.state.player || !$.state.originPos || !$.state.targetPos) {
       $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
       $.state.originPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.3));
       $.state.targetPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.6));
    }
    
    $.state.gain = 1; // 原点に触れる前はゲインの値が1のまま
    $.subNode("Sphere").setPosition($.state.originPos); // 緑の玉を原点に動かす
    ```
    
    </details>
    
    <details>
    
    <summary>Replace the code inside function `tick`</summary>
    
    ```
    if (!$.state.player || !$.state.originPos) return;
    
    // バーチャル手の位置を計算する：原点からの実身体の手（コントローラ）の相対位置×ゲイン
    $.subNode("RightHandAnchor").setPosition(
       $.state.originPos.clone()
           .add($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand).clone()
               .sub($.state.originPos)
               .multiplyScalar($.state.gain || 1)));
    
    // バーチャル手の回転を実身体の手と同期させる
    $.subNode("RightHandAnchor").setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand).clone().multiply($.state.handOffset));
    
    if (!$.state.isTouchable) { // 緑の玉が触れられたばかりで、しばらく触れても反応させてはいけない場合
       $.state.timer = $.state.timer + 1; // タイマー + 1
      
       if ($.state.timer > 10) { //　緑の玉が触れられた時点から10フレーム経ったら
           $.state.isTouchable = true; // 次のフレームから緑の玉を再び触れられるようにする
           $.state.timer = 0; // タイマーを0に戻す
       } else {
           $.setStateCompat("this", "isSphereTouched", false); // 緑の玉が触れられたと検知するフラグをfalseに固定させる
       }
    } else if ($.getStateCompat("this", "isSphereTouched", "boolean")) { // 緑の玉が触れられたと検知したら（trueになったら）
       /*
           緑の玉が触れられたら　$.getStateCompat("this", "isSphereTouched", "boolean")　の値が変わるように、
           このスクリプトが付いたアイテムにCCKのコンポーネント`On Collide Item Trigger`を追加し、
           このアイテムに向けてキー`isSphereTouched`で、メッセージ内容=trueを発信するようにしてください
       */
    
       $.state.isTouchable = false; // 二重クリックを防ぐために、緑の玉を触れられるようにするフラグをfalseにする
       $.setStateCompat("this", "isSphereTouched", false); // 緑の玉が触れられたと検知するフラグをfalseに戻す
    
       if ($.state.isReaching) {
           // 目標地点にある緑の玉が触れられる場合
           onTargetTouched();
       } else {
           // 原点にある緑の玉が触れられる場合
           onOriginTouched();
       }
    }
    ```
    
    </details>
    
    <details>
    
    <summary>Add functions at the bottom of this script file</summary>
    
    ```
    // 原点にある緑の玉が触れられる時に実行される
    function onOriginTouched () {
       $.subNode("Sphere").setPosition($.state.targetPos); // 緑の玉を目標地点に動かし
       $.state.gain = $.state.currentCondition["gain"] ? parseFloat($.state.currentCondition["gain"]) : 1; // この試行におけるゲインの値を設定する
       $.state.isReaching = true; // リーチング（目標地点まで手を伸ばす）フラグをtrueにする
    }
    
    
    // 目標地点にある緑の玉が触れられる時に実行される
    function onTargetTouched () {
       $.sendSignalCompat("this", "state_triggerTransition"); // 次のステート（質問に回答するフェーズ）に遷移させる
       /*
           この関数が実行されると次のフェーズに遷移されるように、
           このスクリプトが付いたアイテムにCCKのコンポーネント`Global Logic`を追加してください。
           その`Global Logic`の中身を、このアイテムに向けたキー`state_triggerTransition`を検知し、
           globalに向けたキー`state_triggerTransition`でsignalを発信するようにしてください
       */
    
       $.state.isReaching = false; // リーチング（目標地点まで手を伸ばす）フラグをfalseにする
    }
    ```
    
    </details>
3. Add CCK components to the `TaskManager` gameobject: TaskManager ゲームオブジェクトに、図に従ってCCKのコンポーネントを追加し、値を設定する。それぞれのコンポーネントの追加理由、および図の通りに設定された場合の動作を以下で説明：
    - Global Logic
        - 目的：移動後の玉に触れたら次のステートに遷移するために
        - 実際の動作：自分からのキーstate_triggerTransitionを持つシグナル（e.g., TaskManager.js > onTargetTouched関数の一行目）を受信し、Globalに向けてキーstate_triggerTransition付きのシグナルを発信する
    - On Collide Item Trigger
        - 目的：玉との接触を検知するために
        - 実際の動作：他のオブジェクトとCollideした際、自分に向けてisSphereTouchedをtrueに設定する
            - その際、TaskManager.js > tick関数の if ($.getStateCompat("this", "isSphereTouched", "boolean"))でそれを受け取って処理する

![image](https://github.com/user-attachments/assets/91a095bb-9ca8-4126-adc9-6ff89b3e7d5c)

#### Prepare objects to be manipulated during the task


#### Add a message panel in state `Trial - Task`

For state `Trial - Task`, create a gameobject from the `Message` prefab.

<details>
    
<summary>Text content</summary>

```
右手の人差し指で、
目の前に現れた緑の玉に触れてください。
玉は触れられたら30cm先まで移動します。
移動後の玉にもう一度触れてください。
```

</details>

If you forgot how to create one, review the video in Section `Add objects for each state`.

#### 試行時の質問＆回答の選択肢の作成

#### Add a message panel in state `Trial - Rest`

Create a gameobject from the `Message` prefab, and change its text to the following:

```
Put your arm down
```

#### Trigger uploading data after trials

For state `AfterTrials`, create a gameobject named `UploadAndNextButton` from the `NextStateButton` prefab.

On its `Interact Item Trigger` component, add one more trigger item:
```
Target: Global exp_uploadCustomData
Value: Signal
```
![image](https://github.com/user-attachments/assets/d441f574-50aa-434c-95b8-031bc43d1221)



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

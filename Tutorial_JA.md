# チュートリアル

このチュートリアルで、手のリダイレクションの実験を実装してみましょう。

### 推奨する予備知識

少なくとも以下の基礎知識を取得していることを推奨します。
- Unity
- JavaScript
- [Cluster Creator Kit (CCK)](https://docs.cluster.mu/creatorkit/)

### 準備

1. [clusterアカウント作成](https://help.cluster.mu/hc/articles/115000827112)
2. [clusterに必要なバージョンのUnityのインストール](https://docs.cluster.mu/creatorkit/installation/install-unity/) を行う。
3. 本実装テンプレートをCloneする

### ウェブコンソールで実験情報を登録

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
        1. [身体化体験に関する質問紙](https://sites.google.com/view/virtualembodimentquestionnaire/download-the-questionnaire)の中から「AC1 私の身体」と「CO1 私の動作 」の質問だけを登録します。
        2. 各質問に対し、「Add Question」ボタンを押して質問の枠を増やし、図に従ってフィールドを埋めます。
        3. すべての質問が入力できたら、「Register」ボタンを押して登録を完了します。
          ![image](https://github.com/user-attachments/assets/ab4d5a9d-8b26-4adf-a0c1-c5e31d19ac70)
    4. 質問紙を確認する：画面が遷移したら、登録された質問紙を確認します。

![image](https://github.com/user-attachments/assets/3f6dd78e-781c-4406-9d67-3c8b5cc0c258)

### 実装テンプレートを開く

ダウンロードした実装テンプレートをUnity Hubから起動します。

### clusterアカウントとリンクする

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

### 外部呼び出しの確認用トークンを取得

1. アカウント紐づけ後に現れる画面で、「URLの登録」に以下のURLを」貼り付けます：`https://script.google.com/macros/s/AKfycbyamdYZGjweG65Dkykdw1oT7MxU4ZXoeqPDT3csW1M2mS3jj8gq9kZzO2iKhSBUOfx0Zg/exec`
2. 表示された「verify用トークン」をコピーしてどこかに保存しておきます。

![image](https://github.com/user-attachments/assets/e780e28d-4427-426d-9dc6-fde4d12b6120)
![image](https://github.com/user-attachments/assets/8c5bea04-d868-4f8c-a664-ae8ae3abcf52)

### 外部呼び出し用に実験IDと確認用トークンを登録する
1. トップメニューから`Window > Luida Editor`を開きます。
![image](https://github.com/user-attachments/assets/ff78908a-2277-4a07-a37f-3a1502146343)
2. 新しいシーンの名前を入力し、「Create and open scene」ボタンをクリックします。
![image](https://github.com/user-attachments/assets/21b1f40a-4367-4448-8277-244593682525)
3. `Experiment Identifiers`タブで、ウェブコンソールに表示された`Experiment ID`を`eID`に入力します。
4. `Experiment Identifiers`タブで、確認用トークンを`Token`に入力します。
![image](https://github.com/user-attachments/assets/e7229049-3d8d-4cee-a5d5-19f5f70a2168)

### 実験変数と試行回数を設定する
1. `Window > Luida Editor`を開き、`Experiment Variables Editor`タブに切り替えます。
2. 下の画像に従ってフィールドを入力します
    1. Name: `gain`
    2. Values (カンマ区切り): `0.75,0.8,0.85,0.9,0.95,1,1.05,1.1,1.15,1.2,1.25`
![image](https://github.com/user-attachments/assets/15fde214-9fc0-4e27-9d3c-aaab31a46863)
3. 「Apply Updated Variables」ボタンをクリックして変更を保存します。
4. シーンを保存します。

### ステート遷移を調整する

`Window > Luida Editor`を開き、`State List Editor`タブに切り替えます。

このチュートリアルでは、以下のようなステート遷移を行います。 

```text
Start → Instruction → Practice - Task → Practice - Questionnaire → Practice - Rest → Preparation → Trial - Task → Trial - Questionnaire → Trial - Rest → AfterTrials → Questionnaire (post-exp) → End
```


そのうち、
- `Practice - Task → Practice - Questionnaire → Practice - Rest` は3回繰り返します。
- `Trial - Task → Trial - Questionnaire → Trial - Rest` は変数`gain`の値の数×2回繰り返します。

エディタウィンドウで遷移を編集します。

- Start：`Transit destination state`を`Instruction`に設定します。
- Acclimatization：`Remove`ボタンを押して削除します。
- Questionnaire (pre-exp)：`Remove`ボタンを押して削除します。
- Practice - Rest：
    - `Has Exit Time`にチェックを入れ、`Exit Time`を5に設定します（5秒後に自動的にステート遷移します）。
    - `Is Repeated`にチェックを入れ、`Repeat destination state`を`Practice - Task`に設定し、`Repeat Count`を3に設定します（練習セッションを3回繰り返します）。	
- Trial - Rest：
    - `Has Exit Time`にチェックを入れ、`Exit Time`を5に設定します（5秒後に自動的にステート遷移します）。
    - （`Trial`ステートの繰り返しは`Experiment Variables Editor`タブで設定した変数と試行回数で制御されるため、ここでの追加設定は不要です）

![image](https://github.com/user-attachments/assets/972b97af-3b00-42d8-b0d5-82039d5d39dd)

### チュートリアル用のプレハブ

`Assets/_Experiment_/Prefabs/Sample_HR/`内のプレハブを確認します。
これらはサンプルシーン`Sample_HR`用ですが、このチュートリアルでも使用します。

#### NextStateButton
クリック時に次のステートに遷移するボタンです。

CCKコンポーネント`Interact Item Trigger`が付いており、このボタンがクリックされると`state_triggerTransition`というキーを持つシグナルがグローバルにブロードキャストされます。
この実装テンプレートはこのシグナルを受信し、次のステートに遷移します。

![image](https://github.com/user-attachments/assets/2ec1b241-9ee3-4b62-8c00-95dc82788118)

#### Message

大きなメッセージパネルです。
参加者に指示を与えるのに使用できます。
テキスト内容を変更するには、子オブジェクトの`TextView`コンポーネントを編集します。
![image](https://github.com/user-attachments/assets/b3401797-2c57-4036-971c-fa0bfce4cef5)

#### RightHand

`RightHand`フォルダに移動して`RightHand`プレハブを見つけます。
人差し指の先端に小さな球体のコライダーが付いています。

![image](https://github.com/user-attachments/assets/eb3d93d2-2ac9-4811-a503-57d43d81271e)

### 各ステートにオブジェクトを追加する

各ステートにゲームオブジェクトを追加するには、`Window > Luida Editor`を開き、`Objects Manager`タブに切り替えます。

以下の仕様に従って各ステートにゲームオブジェクトを追加します。

- **Start**: NextStateButton × 1（テキストを`Start`に変更）
- **Instruction**: NextStateButton × 1（テキストを`Practice`に変更）、Message × 1（練習セッションの指示文にテキストを変更）

    <details>
            
    <summary>指示例</summary>
    
    ```text
    これからは、右手の人差し指で緑の玉を触って、
    質問に答える、というタスクを行っていただきます。
    まずは何回か練習しましょう。
    準備ができたら、設定画面でコントローラを非表示にし、
    前を見て「練習」ボタンを押してください。
    ```
    
    </details>
  
- **Practice**: 後で説明します。
- **Preparation**: NextStateButton × 1（テキストを`Start`に変更）、Message × 1（試行セッションの指示文にテキストを変更）

    <details>
    
    <summary>指示例</summary>
    
    ```text
    練習（3回）は以上になります。
    ここからは本番です。
    同じ手順でタスクを22回行ってください。
    準備ができたら、前を見て開始ボタンを押してください
    ```
    
    </details>
  
- **Trial**: 後で説明します。
- **AfterTrials**: 後で説明します。
- **Questionnaire (post-exp)**: 後で説明します。
- **End**
  - Message × 1（実験終了の指示文にテキストを変更）

    <details>
        
    <summary>指示例</summary>

    ```text
    実験は以上になります。
    ご参加いただきありがとうございました！
    謝礼のcluster pointは後日に付与します。
    目の前のゲートに潜って退室してください。
    ```
    
    </details>
    
  - World gate prefab (`Assets/ClusterGAMEWORLDCENTER/Prefabs/WorldGateToClusterLobby.prefab`) × 1. 追加後:
    - 位置を(0, 0, 1.5)に設定します。
    - 子オブジェクト`SignBoard`を削除します。
    - 子オブジェクト`WorldGate`の`World Or Event Id`フィールドの値を`006d765e-f961-435b-a183-77c35a42e241`（LUIDA勧誘ワールドのWorld ID）に設定します。

![image](https://github.com/user-attachments/assets/6cc12262-b05f-468d-ab5d-f266be6bde95)

### メインの試行を実装する

#### タスク内容：
1. 試行開始：緑の玉が原点（頭の下30センチ＋前30センチ）に戻る
2. リセット：実験参加者が原点にある緑の玉に触れる。すると緑の玉が目標地点（頭の下30センチ＋前60センチ）に移動する。
3. リダイレクション中のリーチングタスク：参加者が腕を伸ばし、もう一度その玉に触れる。腕を伸ばしている間、バーチャルの手がリダイレクションを受けている（手の位置にゲインがかかっており、実際の手の位置からズレている）
4. 試行後（質問の表示）：「バーチャルの手が実身体の手より速いか？遅いか」の質問文と、二択の回答ボタン「速い」と「遅い」が表示される
5. 次の試行へ：実験参加者がいずれかのボタンを選択したら、数秒の休憩の後、次の試行に移行する

この実験では、各試行でバーチャルの手にかかるゲインを変更する必要があるため、実験条件`gain`の値にアクセスできるオブジェクトが必要です。

#### TaskManagerオブジェクトを作成する
1. 上記のビデオを参照し、ステート`Trial - Task`で`TaskManager`という名前の条件依存オブジェクトを作成します。作成したゲームオブジェクトと、`TaskManager.js`というClusterスクリプトアセットが生成されることを確認します。
2. `TaskManager.js`を以下のように編集します。

    <details>
    
    <summary>関数`init`内のコードを置き換える</summary>
    
    ```javascript
    $.state.timer = 0; // タイマーの初期化
    $.state.isTouchable = true; // 緑の玉を触れられるようにするフラグ
    $.state.handOffset = new Quaternion().setFromEulerAngles(new Vector3(0, 90, 0)); // 実身体の手（コントローラ）とバーチャル手の回転の差を補正するオフセットを設定する
    ```
    
    </details>
    
    <details>
    
    <summary>関数`onConditionChanged`内のコードを置き換える</summary>
    
    ```javascript
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
    
    <summary>関数`tick`内のコードを置き換える</summary>
    
    ```javascript
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
    
    <summary>スクリプトファイルの最後に関数を追加</summary>
    
    ```javascript
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

3. CCKコンポーネントを`TaskManager`ゲームオブジェクトに追加します。図に従って値を設定し、各コンポーネントの追加理由と設定動作を説明します。
    - Global Logic
        - 目的：移動後の玉に触れたら次のステートに遷移するために
        - 動作：`state_triggerTransition`キーを持つシグナル（例：`onTargetTouched`関数の一行目）を受信し、Globalに向けてキー`state_triggerTransition`付きのシグナルを発信
    - On Collide Item Trigger
        - 目的：玉との接触を検知するために
        - 動作：他オブジェクトと衝突時に`isSphereTouched`をtrueに設定し、`TaskManager.js > tick`関数で受け取って処理します

![image](https://github.com/user-attachments/assets/91a095bb-9ca8-4126-adc9-6ff89b3e7d5c)

#### タスク中に操作するオブジェクトを準備する

1. リーチングタスクの目標物（小さな球）
    1. `TaskManager`ゲームオブジェクトの子オブジェクトとしてSphereゲームオブジェクトを作成。スケールを`(0.05, 0.05, 0.05)`に設定し、緑色のマテリアルに変更します。
    2. `TaskManager`の`OnCollideItemTrigger`イベントを有効にし、目標物がバーチャル手に触れるとトリガーされるようにします。`RigidBody`コンポーネントを追加し、`Use Gravity`を無効にし、`Constraints`のすべてのチェックボックスを有効にしてスクリプト以外の移動を防ぎます。

<table>
  <tr>
    <td><img width="613" alt="スクリーンショット 2024-11-06 15 14 00" src="https://github.com/user-attachments/assets/4d4da88b-b620-49ca-8cb4-ebf5abe465b6" width="500"></td>
    <td><img width="549" alt="スクリーンショット 2024-11-06 15 14 45" src="https://github.com/user-attachments/assets/fe1a2302-bf95-4288-8a77-b35261549d0e" width="500"></td>
  </tr>
</table>

2. バーチャル手がユーザーの手の位置を追従するようにする
    1. `TaskManager`ゲームオブジェクトの子オブジェクトとして、`RightHandAnchor`という名前の空のゲームオブジェクトを作成します。
    2. `Luida Editor`の`Objects Manager`タブを開き、ステート`Trial - Task`用に`RightHandWrapper`というゲームオブジェクトを作成します。
    3. シーンにバーチャル手のプレハブを`TaskManager`ゲームオブジェクトの子オブジェクトとして追加します。
    4. 追加した`RightHand`ゲームオブジェクトに`ParentConstraint`コンポーネントを追加し、`Sources`を`RightHandAnchor`ゲームオブジェクトに設定してから、Activateボタンを押します。
    <img width="613" alt="スクリーンショット 2024-11-06 15 22 29" src="https://github.com/user-attachments/assets/b47957a1-3ba3-4fcf-963f-b1f5e69f7157">

#### ステート`Trial - Task`にメッセージパネルを追加

`Luida Editor`の`Objects Manager`タブを開き、ステート`Trial - Task`用にプレハブ`Message`からゲームオブジェクトを作成します。

<details>
    
<summary>テキスト内容</summary>

```text
右手の人差し指で、
目の前に現れた緑の玉に触れてください。
玉は触れられたら30cm先まで移動します。
移動後の玉にもう一度触れてください。
```

</details>

#### ステート`Trial - Questionnaire`で質問パネルと回答ボタンを追加

1. `Luida Editor`の`Objects Manager`タブを開き、ステート`Trial - Questionnaire`用に、プレハブ`Message`から`Question`という名前のゲームオブジェクト、`NextStateButton`から`FasterButton`と`SlowerButton`という名前のゲームオブジェクトをそれぞれ作成します。

https://github.com/user-attachments/assets/b9ae7a08-7f5d-4b1b-889c-0bb5afed96d2

2. `Question`ゲームオブジェクトのテキスト内容を編集します。

    <details>
    
    <summary>質問のテキスト内容</summary>

    ```text
    Q: バーチャル空間の中の手は実身体の手より
    速く動いたか？遅く動いたか？
    分からない場合はどちらかのボタンを押してください
    ```

    </details>

3. `FasterButton`: 位置を`(-0.75, 0.75, -0.5)`に設定し、`Interact Item Trigger`コンポーネントに以下のトリガーを追加します。

    ```text
    Target: Global isFaster
    Value: Bool true
    ----------
    Target: Global exp_recordCustomData
    Value: Signal
    ```

4. `SlowerButton`: 位置を`(0.75, 0.75, -0.5)`に設定し、`Interact Item Trigger`コンポーネントに以下のトリガーを追加します。

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

#### ステート`Trial - Rest`でメッセージを追加

`Message`プレハブからゲームオブジェクトを作成し、テキストを`腕を下ろしてください`に変更します。

#### 試行終了後にデータをアップロードするトリガーを追加

ステート`AfterTrials`用に`NextStateButton`プレハブから`UploadAndNextButton`というゲームオブジェクトを作成します。

`Interact Item Trigger`コンポーネントに次のトリガーを追加します。

```text
Target: Global exp_uploadCustomData
Value: Signal
```

![image](https://github.com/user-attachments/assets/d441f574-50aa-434c-95b8-031bc43d1221)

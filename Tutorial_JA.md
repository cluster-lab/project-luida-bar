# チュートリアル

このチュートリアルでは本実装テンプレートを用いて、LUIDA で実施できるハンドリダイレクションの実験を実装してみましょう。

以下について基礎のみでも勉強しておくことを推奨します：

- Unity
- JavaScript
- [Cluster Creator Kit (CCK)](https://docs.cluster.mu/creatorkit/)

また、チュートリアルを始める前に、以下を用意しておいてください：

1. [cluster アカウント作成](https://help.cluster.mu/hc/articles/115000827112)
2. [cluster に必要なバージョンの Unity のインストール](https://docs.cluster.mu/creatorkit/installation/install-unity/) を行う。
3. 本実装テンプレートを Clone する

---

## 目次

1. [ウェブコンソールで実験情報を登録](#ウェブコンソールで実験情報を登録)
2. [Unity での初期設定](#unity-での初期設定)
3. [実験変数と試行回数を設定する](#実験変数と試行回数を設定する)
4. [ステート遷移を調整する](#ステート遷移を調整する)
5. [チュートリアル用のプレハブ](#チュートリアル用のプレハブ)
6. [各ステートにオブジェクトを追加する](#各ステートにオブジェクトを追加する)
7. [試行セッションを実装する](#試行セッションを実装する)
8. [練習セッションを実装する](#練習セッションを実装する)
9. [データ記録・アップロード用オブジェクトの追加](#データ記録アップロード用オブジェクトの追加)
10. [質問紙の紐づけ](#質問紙の紐づけ)
11. [アップロード前の準備](#アップロード前の準備)
12. [cluster にアップロード](#cluster-にアップロード)
13. [ワールド ID とアバターの登録](#ワールド-id-とアバターの登録)
14. [LUIDA での自動掲載を待つ](#luida-での自動掲載を待つ)

---

## ウェブコンソールで実験情報を登録

1. ウェブコンソールを開く：https://cluster-lab.github.io/project-luida-web-console/
   （GitHub アカウントを持たず、ウェブコンソールを開けない場合、先に GitHub アカウントを作っておく）

2. 登録してログインする

3. 実験の募集情報を登録する： 1. 「Register New Experiment」リンクをクリックして、新しい実験の募集情報を登録します。 2. 下の画像に示したフィールドを埋めます（チュートリアルのため任意の文字列で大丈夫です）。その他のフィールドは空白かデフォルト値のままにします。
<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/3ccc2bb5-cb02-495e-b876-da06d21b4b14" alt="Image 1" width="500"/></td>
    <td><img src="https://github.com/user-attachments/assets/f75c5f36-3030-4f72-bbf4-1259d42dac09" alt="Image 2" width="500"/></td>
  </tr>
</table>

4. Register ボタンを押すと、情報が保存され、次の画面に表示されます。そこで自動的に生成された実験の識別子（eID）を確認します。

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

---

## Unity での初期設定

ダウンロードした実装テンプレートを Unity Hub から起動したら、以下の手順に従ってください。

### cluster アカウントとリンクする

1. トップメニューから Cluster > 外部通信(callExternal)接続先 URL を選択します。
   ![image](https://github.com/user-attachments/assets/9ac1311a-aa09-4e28-95a0-a2081e9883f4)
2. Web でトークン発行ボタンをクリックしてブラウザでトークン発行画面を開きます。そこで「トークン作成」ボタンをクリックし、表示されたトークンをコピーしておきます。
<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/2c0d3212-e301-417d-a594-8ed1f7dec08f" alt="Image 1" width="400"/></td>
    <td><img src="https://github.com/user-attachments/assets/4395e140-b709-48ee-b659-52e6a93009fa" alt="Image 2" width="400"/></td>
    <td><img src="https://github.com/user-attachments/assets/2b5ca13b-14cf-4c77-ae06-ed7ca7792705" alt="Image 2" width="400"/></td>
  </tr>
</table>

3. Unity に戻り、トークンを貼り付けて、「このトークンを使用」ボタンを押します。

![image](https://github.com/user-attachments/assets/df24aab2-684f-4f5b-b287-b451f3c65f6d)

### 外部呼び出しの確認用トークンを取得

1. アカウント紐づけ後に現れる画面で、「URL の登録」に以下の URL を」貼り付けます：`https://script.google.com/macros/s/AKfycbyamdYZGjweG65Dkykdw1oT7MxU4ZXoeqPDT3csW1M2mS3jj8gq9kZzO2iKhSBUOfx0Zg/exec`
2. 表示された「verify 用トークン」をコピーしてどこかに保存しておきます。

![image](https://github.com/user-attachments/assets/e780e28d-4427-426d-9dc6-fde4d12b6120)
![image](https://github.com/user-attachments/assets/8c5bea04-d868-4f8c-a664-ae8ae3abcf52)

### シーンを作成し、実験 ID と外部呼び出しの確認用トークンを登録する

1. トップメニューから`Window > Luida Editor`を開きます。
   ![image](https://github.com/user-attachments/assets/ff78908a-2277-4a07-a37f-3a1502146343)
2. 新しいシーンの名前を入力し、「Create and open scene」ボタンをクリックします。
   ![image](https://github.com/user-attachments/assets/21b1f40a-4367-4448-8277-244593682525)
3. `Experiment Identifiers`タブで、ウェブコンソールに表示された`Experiment ID`を`eID`に入力します。
4. `Experiment Identifiers`タブで、確認用トークンを`Token`に入力します。
   ![image](https://github.com/user-attachments/assets/e7229049-3d8d-4cee-a5d5-19f5f70a2168)

---

## 実験変数と試行回数を設定する

1. `Window > Luida Editor`を開き、`Experiment Variables Editor`タブに切り替えます。
2. 下の画像に従ってフィールドを入力します 1. Name: `gain` 2. Values (カンマ区切り): `0.75,0.8,0.85,0.9,0.95,1,1.05,1.1,1.15,1.2,1.25`
   ![image](https://github.com/user-attachments/assets/15fde214-9fc0-4e27-9d3c-aaab31a46863)
3. 「Apply Updated Variables」ボタンをクリックして変更を保存します。
4. シーンを保存します。

---

## ステート遷移を調整する

`Window > Luida Editor`を開き、`State List Editor`タブに切り替えます。

このチュートリアルでは、以下のようなステート遷移を行います。

`Start` → `Instruction` → `Practice - Task` → `Practice - Questionnaire` → `Practice - Rest` → `Preparation` → `Trial - Task` → `Trial - Questionnaire` → `Trial - Rest` → AfterTrials → `Questionnaire (post-exp)` → `End`

そのうち、

- `Practice - Task` → `Practice - Questionnaire` → `Practice - Rest` は**3 回**繰り返します。
- `Trial - Task` → `Trial - Questionnaire` → `Trial - Rest` は**変数`gain`の値の数 ×2 回**繰り返します。

エディタウィンドウで遷移を編集します。

- Start：`Transit destination state`を`Instruction`に設定します。
- Acclimatization：`Remove`ボタンを押して削除します。
- Questionnaire (pre-exp)：`Remove`ボタンを押して削除します。
- Practice - Rest：
  - `Has Exit Time`にチェックを入れ、`Exit Time`を 5 に設定します（5 秒後に自動的にステート遷移します）。
  - `Is Repeated`にチェックを入れ、`Repeat destination state`を`Practice - Task`に設定し、`Repeat Count`を 3 に設定します（練習セッションを 3 回繰り返します）。
- Trial - Rest：
  - `Has Exit Time`にチェックを入れ、`Exit Time`を 5 に設定します（5 秒後に自動的にステート遷移します）。
  - （`Trial`ステートの繰り返しは`Experiment Variables Editor`タブで設定した変数と試行回数で制御されるため、ここでの追加設定は不要です）

![image](https://github.com/user-attachments/assets/972b97af-3b00-42d8-b0d5-82039d5d39dd)

---

## チュートリアル用のプレハブ

`Assets/_Experiment_/Prefabs/Sample_HR/`内のプレハブを確認します。
これらはサンプルシーン`Sample_HR`用ですが、このチュートリアルでも使用します。

### NextStateButton

クリック時に次のステートに遷移するボタンです。

CCK コンポーネント`Interact Item Trigger`が付いており、このボタンがクリックされると`state_triggerTransition`というキーを持つシグナルがグローバルにブロードキャストされます。
この実装テンプレートはこのシグナルを受信し、次のステートに遷移します。

![image](https://github.com/user-attachments/assets/2ec1b241-9ee3-4b62-8c00-95dc82788118)

### Message

大きなメッセージパネルです。
参加者に指示を与えるのに使用できます。
テキスト内容を変更するには、子オブジェクトの`TextView`コンポーネントを編集します。
![image](https://github.com/user-attachments/assets/b3401797-2c57-4036-971c-fa0bfce4cef5)

### RightHand

`RightHand`フォルダに移動して`RightHand`プレハブを見つけます。
人差し指の先端に小さな球体のコライダーが付いています。

![image](https://github.com/user-attachments/assets/eb3d93d2-2ac9-4811-a503-57d43d81271e)

---

## 各ステートにオブジェクトを追加する

各ステートにゲームオブジェクトを追加するには、`Window > Luida Editor`を開き、`Objects Manager`タブに切り替えます。

以下の仕様に従って各ステートにゲームオブジェクトを追加します。

| Step                         | Action Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Start**                    | NextStateButton × 1 (Change text to `Start`)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| **Instruction**              | NextStateButton × 1 (Change text to `Practice`), Message × 1 (Change instruction text for practice session) <br><details> <summary>Instruction text example</summary> <pre>これからは、右手の人差し指で緑の玉を触って、質問に答える、というタスクを行っていただきます。まずは何回か練習しましょう。準備ができたら、設定画面でコントローラを非表示にし、前を見て「練習」ボタンを押してください。</pre></details>                                                                                                                                                                                                                                                     |
| **Practice - Task**          | 後述                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| **Practice - Questionnaire** | Questionnaire オブジェクトは使わないため削除。他は後述。                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| **Practice - Rest**          | 後述                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| **Preparation**              | NextStateButton × 1 (Change text to `Start`), Message × 1 (Change instruction text for trial session) <br><details> <summary>Instruction text example</summary> <pre>練習（3 回）は以上になります。ここからは本番です。同じ手順でタスクを 22 回行ってください。準備ができたら、前を見て開始ボタンを押してください。</pre></details>                                                                                                                                                                                                                                                                                                                                 |
| **Trial - Task**             | 後述                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| **Trial - Questionnaire**    | Questionnaire オブジェクトは使わないため削除。他は後述。                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| **Trial - Rest**             | 後述                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| **AfterTrials**              | 後述                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| **Questionnaire (post-exp)** | 後述                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| **End**                      | 1. Message × 1 (Change instruction text for experiment completion) <br><details> <summary>Instruction text example</summary> <pre>実験は以上になります。ご参加いただきありがとうございました！謝礼の cluster point は後日に付与します。目の前のゲートに潜って退室してください。</pre></details> <br> 2. ワールドゲートのプレハブ (`Assets/ClusterGAMEWORLDCENTER/Prefabs/WorldGateToClusterLobby.prefab`) × 1.<br>追加後： <br> a. 位置を(0, 0, 1.5)に設定 <br> b. 子ゲームオブジェクト`SignBoard`の削除 <br> c. 子ゲームオブジェクト`WorldGate`のフィールド`World Or Event Id`を`006d765e-f961-435b-a183-77c35a42e241`に設定 (LUIDA 参加者募集ワールドの World ID) |

![image](https://github.com/user-attachments/assets/6cc12262-b05f-468d-ab5d-f266be6bde95)

---

## 試行セッションを実装する

### タスク内容：

1. 試行開始：緑の玉（ターゲット）が原点（頭の下 30 センチ＋前 30 センチ）に戻る
2. リセット：実験参加者が原点にある緑の玉に触れる。すると緑の玉が目標地点（頭の下 30 センチ＋前 60 センチ）に移動する。
3. リダイレクション中のリーチングタスク：参加者が腕を伸ばし、もう一度その玉に触れる。腕を伸ばしている間、バーチャルの手がリダイレクションを受けている（手の位置にゲインがかかっており、実際の手の位置からズレている）
4. 試行後（質問の表示）：「バーチャルの手が実身体の手より速いか？遅いか」の質問文と、二択の回答ボタン「速い」と「遅い」が表示される
5. 次の試行へ：実験参加者がいずれかのボタンを選択したら、数秒の休憩の後、次の試行に移行する

この実験では、各試行でバーチャルの手にかかるゲインを変更する必要があるため、実験条件`gain`の値にアクセスできるオブジェクトが必要です。
実験条件の値にアクセスできるオブジェクトの作成方法は以下の動画で示します：

https://github.com/user-attachments/assets/c58aa9c0-7562-40cb-952a-6c3b767f099d

それでは、以下の手順に従って試行セッションを実装してください。

### ステート`Trial - Task`で、タスクの指示を表示する

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

### ステート`Trial - Task`で、TaskManager オブジェクトを作成する

1. 上記のビデオを参照し、ステート`Trial - Task`で`TaskManager`という名前の condition-dependent オブジェクトを作成します。作成したゲームオブジェクトと、`TaskManager.js`という Cluster スクリプトアセットが生成されることを確認します。
2. `TaskManager.js`を以下のように編集します。

   <details>

   <summary>関数`init`内のコードを置き換える</summary>

   ```javascript
   $.state.timer = 0; // タイマーの初期化
   $.state.isTouchable = true; // 緑の玉を触れられるようにするフラグ
   $.state.handOffset = new Quaternion().setFromEulerAngles(
     new Vector3(0, 90, 0)
   ); // 実身体の手（コントローラ）とバーチャル手の回転の差を補正するオフセットを設定する
   ```

   </details>

   <details>

   <summary>関数`onConditionChanged`内のコードを置き換える</summary>

   ```javascript
   // 変数の値の初期化：プレイヤー、原点、目標地点
   if (!$.state.player || !$.state.originPos || !$.state.targetPos) {
     $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
     $.state.originPos = $.state.player
       .getHumanoidBonePosition(HumanoidBone.Head)
       .clone()
       .add(new Vector3(0, -0.3, 0.3));
     $.state.targetPos = $.state.player
       .getHumanoidBonePosition(HumanoidBone.Head)
       .clone()
       .add(new Vector3(0, -0.3, 0.6));
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
     $.state.originPos.clone().add(
       $.state.player
         .getHumanoidBonePosition(HumanoidBone.RightHand)
         .clone()
         .sub($.state.originPos)
         .multiplyScalar($.state.gain || 1)
     )
   );

   // バーチャル手の回転を実身体の手と同期させる
   $.subNode("RightHandAnchor").setRotation(
     $.state.player
       .getHumanoidBoneRotation(HumanoidBone.RightHand)
       .clone()
       .multiply($.state.handOffset)
   );

   if (!$.state.isTouchable) {
     // 緑の玉が触れられたばかりで、しばらく触れても反応させてはいけない場合
     $.state.timer = $.state.timer + 1; // タイマー + 1

     if ($.state.timer > 10) {
       //　緑の玉が触れられた時点から10フレーム経ったら
       $.state.isTouchable = true; // 次のフレームから緑の玉を再び触れられるようにする
       $.state.timer = 0; // タイマーを0に戻す
     } else {
       $.setStateCompat("this", "isSphereTouched", false); // 緑の玉が触れられたと検知するフラグをfalseに固定させる
     }
   } else if ($.getStateCompat("this", "isSphereTouched", "boolean")) {
     // 緑の玉が触れられたと検知したら（trueになったら）
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
   function onOriginTouched() {
     $.subNode("Sphere").setPosition($.state.targetPos); // 緑の玉を目標地点に動かし
     $.state.gain = $.state.currentCondition["gain"]
       ? parseFloat($.state.currentCondition["gain"])
       : 1; // この試行におけるゲインの値を設定する
     $.state.isReaching = true; // リーチング（目標地点まで手を伸ばす）フラグをtrueにする
   }
   ```


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

3. スクリプトの編集が終わったら、`Luida Editor`の`Objects Manager`タブで`Update Script`ボタンを押します。
   <img width="1179" alt="スクリーンショット 2024-11-06 20 30 20" src="https://github.com/user-attachments/assets/a6eca71d-a67f-45f1-a103-8d4258f8c4f6">

4. 次の CCK コンポーネントを`TaskManager`ゲームオブジェクトに追加します。

   **Global Logic**

   追加目的：移動後にターゲットに触れたら次のステートに遷移するため。

   <details>

   <summary>コンポーネントの設定</summary>

   ```text
   Target: Item
   Key: state_triggerTransition
   Item: TaskManagerゲームオブジェクト自身
   ----------
   Global state_triggerTransition Signal
   = Constant Bool true
   ```

   説明：自分自身から state_triggerTransition キーを持つシグナルを受信し（例：TaskManager.js の onTargetTouched 関数の一行目）、グローバルに向けて state_triggerTransition キーを持つシグナルを発信します。

   </details>

   **On Collide Item Trigger**

   追加目的：ターゲットとの接触を検知するため。

   <details>

   <summary>コンポーネントの設定</summary>

   ```text
   Collision Event Type: Enter
   Collision Type: Collision

   Triggers
   ----------
   Target: This isSphereTouched
   Value: Bool true
   ```

   説明：他のオブジェクトと衝突した際に、isSphereTouched を true に設定し、TaskManager.js の tick 関数で if (\$.getStateCompat("this", "isSphereTouched", "boolean"))を使用して処理します。

   </details>

![image](https://github.com/user-attachments/assets/91a095bb-9ca8-4126-adc9-6ff89b3e7d5c)

### ステート`Trial - Task`で、タスク中に操作するオブジェクトを準備する

1. リーチングタスクの目標物（小さな球）
   1. `TaskManager`ゲームオブジェクトの子オブジェクトとして Sphere ゲームオブジェクトを作成。スケールを`(0.05, 0.05, 0.05)`に設定し、緑色のマテリアルに変更します。
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
   3. シーンにバーチャル手のプレハブを`RightHandWrapper`ゲームオブジェクトの子オブジェクトとして追加します。
   4. 追加した`RightHand`ゲームオブジェクトに`ParentConstraint`コンポーネントを追加し、`Sources`を`RightHandAnchor`ゲームオブジェクトに設定してから、Activate ボタンを押します。
      <img width="613" alt="スクリーンショット 2024-11-06 15 22 29" src="https://github.com/user-attachments/assets/b47957a1-3ba3-4fcf-963f-b1f5e69f7157">

### ステート`Trial - Questionnaire`で、質問パネルと回答ボタンを追加する

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

### ステート`Trial - Rest`でメッセージを追加する

`Message`プレハブからゲームオブジェクトを作成し、テキストを`腕を下ろしてください`に変更します。

### ステート`AfterTrials`で、試行終了後にデータをアップロードするトリガーを追加する

ステート`AfterTrials`用に`NextStateButton`プレハブから`UploadAndNextButton`というゲームオブジェクトを作成します。

`Interact Item Trigger`コンポーネントに次のトリガーを追加します。

```text
Target: Global exp_uploadCustomData
Value: Signal
```

![image](https://github.com/user-attachments/assets/d441f574-50aa-434c-95b8-031bc43d1221)

---

## 練習セッションを実装する

試行セッションと大体一緒ですが、ゲインを `0.75 (最小値), 1, 1.25 (最大値)` のみにします。また、実験条件へのアクセスもデータの記録も行いません。

以下の手順に従って練習セッションを実装してください。

### ステート`Trial - Task`で、タスクの指示を表示する

`Luida Editor`の`Objects Manager`タブを開き、ステート`Practice - Task`用にプレハブ`Message`からゲームオブジェクトを作成します。

<details>

<summary>指示内容の例文</summary>

```text
右手の人差し指で、
目の前に現れた緑の玉に触れてください。
玉は触れられたら30cm先まで移動します。
移動後の玉にもう一度触れてください。
```

</details>

### 試行セッションのゲームオブジェクトを練習セッションに複製（下記の動画を参照）

1. ステート`Trial - XXX`の以下に挙げられたゲームオブジェクトを、ステート`Practice - XXX`に複製します（`States > Trial - XXX > Objects`の子ゲームオブジェクトを`States > Practice - XXX > Objects`に子ゲームオブジェクトとして複製します）：
   - `Trial - Task`の`Message`、`RightHandWrapper`　 → 　`Practice - Task`
   - `Trial - Questionnaire`の`Question`、`FasterButton`、`SlowerButton`　 → 　`Practice - Questionnaire`
   - `Trial - Rest`の`Message`　 → 　`Practice - Rest`
2. `Luida Editor`の`Objects Manager`タブを開き（開いている場合は一回閉じてから開く）、ステート`Practice - XXX`で`Fix state_id`ボタンを全て押します。

https://github.com/user-attachments/assets/14c40fdc-b412-4990-b994-32bae7aaffe9

### ステート`Practice - Task`で、PracticeTaskManager オブジェクトを作成する

1. 下記のビデオを参照し、`Luida Editor`の`Objects Manager`タブを開き、ステート`Practice - Task`用に`PracticeTaskManager`というスクリプト付きのゲームオブジェクトを作成します。作成したゲームオブジェクトと、`PracticeTaskManager.js`という Cluster スクリプトアセットが生成されることを確認します。

https://github.com/user-attachments/assets/4937f73b-7eec-4c86-9183-1251c2b22c52

2. `PracticeTaskManager.js`を以下のように編集します。

<details>

<summary>スクリプトファイルの先頭に追加</summary>

```javascript
const gains = [1, 0.75, 1.25]; // 練習時にバーチャル手にかけるゲイン
```

</details>

<details>

<summary>関数OnStateEnterの置き換え</summary>

```javascript
// 変数の値の初期化：プレイヤー、原点、目標地点
if (!$.state.player || !$.state.originPos || !$.state.targetPos) {
  $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
  $.state.originPos = $.state.player
    .getHumanoidBonePosition(HumanoidBone.Head)
    .clone()
    .add(new Vector3(0, -0.3, 0.3));
  $.state.targetPos = $.state.player
    .getHumanoidBonePosition(HumanoidBone.Head)
    .clone()
    .add(new Vector3(0, -0.3, 0.6));
}

$.state.gain = 1; // 原点に触れる前はゲインの値が1のまま
$.subNode("Sphere").setPosition($.state.originPos); // 緑の玉を原点に動かす
```

</details>

<details>

<summary>関数DuringStateの置き換え</summary>

```javascript
if (!$.state.player || !$.state.originPos) return;

// バーチャル手の位置を計算する：原点からの実身体の手（コントローラ）の相対位置×ゲイン
$.subNode("RightHandAnchor").setPosition(
  $.state.originPos.clone().add(
    $.state.player
      .getHumanoidBonePosition(HumanoidBone.RightHand)
      .clone()
      .sub($.state.originPos)
      .multiplyScalar($.state.gain || 1)
  )
);

// バーチャル手の回転を実身体の手と同期させる
$.subNode("RightHandAnchor").setRotation(
  $.state.player
    .getHumanoidBoneRotation(HumanoidBone.RightHand)
    .clone()
    .multiply($.state.handOffset)
);

if (!$.state.isTouchable) {
  // 緑の玉が触れられたばかりで、しばらく触れても反応させてはいけない場合
  $.state.timer = $.state.timer + 1; // タイマー + 1

  if ($.state.timer > 10) {
    //　緑の玉が触れられた時点から10フレーム経ったら
    $.state.isTouchable = true; // 次のフレームから緑の玉を再び触れられるようにする
    $.state.timer = 0; // タイマーを0に戻す
  } else {
    $.setStateCompat("this", "isSphereTouched", false); // 緑の玉が触れられたと検知するフラグをfalseに固定させる
  }
} else if ($.getStateCompat("this", "isSphereTouched", "boolean")) {
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

<summary>スクリプトファイルの最後に追加</summary>

```javascript
// 原点にある緑の玉が触れられる時に実行される
function onOriginTouched() {
  $.subNode("Sphere").setPosition($.state.targetPos);
  $.state.gain = gains[$.state.gainId];
  $.state.isReaching = true;
}

// 目標地点にある緑の玉が触れられる時に実行される
function onTargetTouched() {
  $.state.gainId = $.state.gainId + 1;
  $.sendSignalCompat("this", "state_triggerTransition");
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

3. スクリプトの編集が終わったら、`Luida Editor`の`Objects Manager`タブで`Update Script`ボタンを押します。
   <img width="1203" alt="スクリーンショット 2024-11-06 20 29 50" src="https://github.com/user-attachments/assets/f603a55c-08db-45af-ba40-076b7868b545">

### ステート`Practice - Task`で、複製されたバーチャル手の追従設定を直す

1. `PracticeTaskManager`の子オブジェクトとして、`RightHandAnchor`という名前の空のゲームオブジェクトを作成します。
2. `RightHandWrapper`の子ゲームオブジェクト`RightHand`で、`ParentConstraint`コンポーネントの`Sources`を、`PracticeTaskManager`の子オブジェクト`RightHandAnchor`ゲームオブジェクトに設定してから、Activate ボタンを押します。

<img width="620" alt="スクリーンショット 2024-11-06 19 18 26" src="https://github.com/user-attachments/assets/2dcbe85e-1659-4eff-83f7-e8f607f640b4">

### ステート`Practice - Questionnaire`で、複製された回答ボタンの設定を直す

`FasterButton`と`SlowerButton`の`Interact Item Trigger`コンポーネントで、キー`isFaster`と`exp_recordCustomData`を含んだトリガー 2 つを削除します。

---

## データ記録・アップロード用オブジェクトの追加

1. `Luida Editor`の`Data Recorders List`タブで`Add Custom Data Recorder`ボタンを押します。ゲームオブジェクトとスクリプトが生成されたか確認します。
   ![create-data-recorder](https://github.com/user-attachments/assets/06a8a5bd-c475-4f3a-b86d-5005393ad1c0)
2. そのスクリプトファイルを開いて、関数`calculateData`の中身を以下の内容に置き換えます。

<details>

<summary>関数`calculateData`の中身を置き換える</summary>

```javascript
let fileName = "reachingTaskAnswers";
let returnData = $.state.customData;

const newRecord = {
  g: $.state.currentCondition["gain"], // 現在のゲイン条件
  a: $.getStateCompat("global", "isFaster", "boolean") ? "F" : "S", // 「速い」「遅い」のどちらを選んだか
};

if (fileName in returnData && Array.isArray(returnData[fileName])) {
  returnData[fileName].push(newRecord);
} else {
  returnData[fileName] = [newRecord];
}

return returnData;
```

説明：試行ごとの条件である「ゲイン（gain）」の値と、参加者が選択した isFaster の値を保存します。「速い」を選択した場合は isFaster = true、「遅い」を選択した場合は isFaster = false としてデータが記録されます。

</details>

3. スクリプトの編集が終わったら、`Luida Editor`の`Data Recorders List`タブで`Update Script`ボタンを押します。

<img width="1209" alt="スクリーンショット 2024-11-06 20 45 59" src="https://github.com/user-attachments/assets/e153eaff-38d2-453f-9c18-375cc954bcee">

---

## 質問紙の紐づけ

実験が実際に cluster 上で実行される際、ウェブコンソールに登録された質問紙の質問文と回答選択肢が自動的に生成されます。
ただし、まず本実装テンプレートでシーン内の質問紙オブジェクトとリンクさせる必要があります。以下の手順に従ってください：

1. ウェブコンソールに登録した質問紙の qID の値を確認します。
   ![image](https://github.com/user-attachments/assets/0e8c0d75-4673-4bec-8689-888abc9c628c)
2. `Luida Editor`の`Objects Manager`タブを開きます。
3. ステート`Questionnaire (post-exp)`で既に Questionnaire オブジェクトがあれば、qID をウェブコンソールに登録されたものに変えます。
   https://github.com/user-attachments/assets/9afd89be-2726-4467-84d7-7d08c901d05c
4. ステート`Questionnaire (post-exp)`で Questionnaire オブジェクトがなければ、一つ作成して qID をウェブコンソールに登録されたものに設定します。
   ![create-questionnaire](https://github.com/user-attachments/assets/3013b077-206b-448e-bcd1-f916dfce8cb7)
5. 質問紙の位置調整：Questionnaire オブジェクトの位置を`(0, 1.5, 1)`に設定します。

---

## アップロード前の準備

1. ベータ機能を有効にします。
   ![image](https://github.com/user-attachments/assets/af786e5e-07fe-4126-b350-1ed7c0401ecd)
2. `Window > かおもラボ > CSCombiner`開いて`全更新`ボタンを押します。
   ![image](https://github.com/user-attachments/assets/12cd1c5e-0dcc-4d91-b340-900ed0a35041)
3. シーンをセーブします。
4. ローカルでテストプレイ：Unity エディターのプレイボタンを押して、実験が予想通りに回るか確認します
   - **本実装テンプレートは cluster 用の Unity プロジェクトで、現状ではローカルでのテストプレイはデスクトップモードでしか行えません。VR 機能でないとテストが難しい場合、後述の cluster の「テスト用スペース」機能を活用してください。**
   - **ローカルでのテストプレイでは、cluster を介した外部への呼び出しができないため、質問紙オブジェクトの自動生成や、データのアップロードは機能しません。もし質問紙オブジェクトが自動生成されず、次のステートに移行できなくなった場合、お手数ですが、いったんその質問紙オブジェクトを削除してからプレイし、cluster にアップロードする前に戻してください。**

ローカルでテストプレイの様子：
https://github.com/user-attachments/assets/c3c6c913-a70a-4397-8852-6ffb08f3fb4c

---

## cluster にアップロード

1. [こちらの手順](https://creator.cluster.mu/2020/03/28/%E5%88%B6%E4%BD%9C%E3%81%97%E3%81%9F%E3%83%AF%E3%83%BC%E3%83%AB%E3%83%89%E3%82%92%E3%80%8Ccluster%E3%80%8D%E3%81%AB%E3%82%A2%E3%83%83%E3%83%97%E3%83%AD%E3%83%BC%E3%83%89%E3%81%99%E3%82%8B/)に従って、cluster のワールドとしてアップロードしてます。アップロードできたら自ら入室し、実験の流れを一回一通り体験し、動作を確認します。
   - この時点では、アバターがまだ自由に選択できますが、正式に実験が公開されたら透明アバターしか選べなくなります。
2. もしアップロード後に何かバグが見つかり、修正して動作を再確認したい場合、「テスト用アップロード」を行い、「[テスト用スペース](https://creator.cluster.mu/2024/05/24/testspace/)」で確認することができます。

- バグの修正ができたら、再びのテストではない方のアップロードを忘れずに

3. 最後まで一通り体験できたら、ウェブコンソールから質問紙への回答とアップロードしたデータ（ターゲットの座標・サイズごとのタスク時間）が表示されているか確認します。
   ![image](https://github.com/user-attachments/assets/a8a75569-b631-4f56-8fbe-ed2137ee699f)

---

## ワールド ID とアバターの登録

### ワールド ID の登録

1. ブラウザーから cluster ウェブサイトの「マイコンテンツ」にアクセスし、自作のワールド一覧画面に実験ワールドが出てきたか確認します。
2. 該当ワールドを選択し、そのワールドのページの URL の後ろにある英数字の文字列はワールド ID です。そのワールド ID をウェブコンソールの実験募集情報編集画面で登録します。
   ![image](https://github.com/user-attachments/assets/44821568-fa20-4f75-9cf1-c49f38b9d4e5)
   ![image](https://github.com/user-attachments/assets/e69ff5cc-f22d-41bc-a519-d810a33cbeb7)
   ![image](https://github.com/user-attachments/assets/b33e8c1c-fbda-483a-ac23-f98a193725a2)

### アバターを非表示にする

この実験では、見えるアバターは必要ないため、参加者が実験中に指定された透明なアバターを強制的に使用するよう、以下の手順に従ってください。

1. LUIDA のウェブコンソールを開き、登録した実験ページを開いて、`Set Avatar by World`ボタンを押します。

![image](https://github.com/user-attachments/assets/8d1c30d1-2292-4979-873d-f27679c6d05e)

2. `Add World-Avatar Set`を押します。次に、ワールド ID を入力し、`Hide avatar`のチェックボックスをオンにして、`Submit`を押します。

![image](https://github.com/user-attachments/assets/1b8b048b-4316-4c7b-a077-8001db0f814b)

---

## LUIDA での自動掲載を待つ

数日後\*にあなたが本実装テンプレートで作成したこの実験は、LUIDA の参加者募集ワールドに掲載されます。しばらくお待ちください。

\*当日に掲載できるように LUIDA の機能を改善する予定です。

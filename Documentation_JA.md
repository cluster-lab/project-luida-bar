# LUIDA用実験系の実装テンプレート ドキュメント

## 実験変数と試行回数の設定

`LUIDA > Configure experiment automation > Experiment Variables`画面（左図）実験の参加者内/参加者間変数を登録すると。それに基づいてシステムが自動的に試行の数と各試行における実験条件を決定します（右図）。

<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/d697ecb9-555c-4a65-90fe-1cc295cabc8d" alt="variables-config-ui" width="600"/></td>
    <td><img src="https://github.com/user-attachments/assets/d888c793-3c0c-4215-94a3-2eb5d95db4e6" alt="variables-config-example" width="600"/></td>
  </tr>
</table>

各設定項目の説明：
* `Variables for Within-Subject Conditions`：参加者内変数の設定フォーム
* `Variables for Between-Subject Conditions`：参加者間変数の設定フォーム
* `Name`：変数の名前
* `Values`：変数の値のリスト。カンマ（`,`）区切りで複数の値を指定します。
* `isRandom`：ランダム順で施行されるか。チェックしない場合はValueの値の順番通りに施行されます。
  * 現在、参加者間変数では`isRandom`をtrueにしか設定できません。つまりある参加者に割り当てられる参加者間条件はランダムになっています。今後は参加者の事前アンケートの回答などに基づいて参加者間条件を割り当てる仕組みを実装する予定です。
* `Trials Count per Condition`：各条件（変数の組み合わせ）あたりで行う試行の回数を入力します。

## 実験進行の設定（ステート遷移）と質問紙の紐づけ

`LUIDA > Configure experiment automation > State Machine`画面（左図）で、実験の進行フローを「ステート」と呼ばれる単位で設定すると、右図のように実験が自動的に進行します。
各ステートは実験の一区切り（例：説明、タスク実行、休憩、質問紙回答など）を表し、それらがどのように遷移するかをここで定義できます。

![state-transition-example](https://github.com/user-attachments/assets/69ce4439-a2da-4912-8344-feba222e1347)

<!-- ![Luida Editor Experiment Statesタブ](https://github.com/user-attachments/assets/fbbc7f52-af5b-43db-adf7-4d579fed23b3) -->

各設定項目の説明：
-   **Move state to**: リスト内でのステートの遷移順を上下に移動させます。
-   **Has Exit Time**: このオプションを有効にすると、指定した時間（`Exit time (seconds)` フィールドに入力）が経過すると自動的に現在のステートが終了し、次のステートへ遷移します。
-   **Is Repeated**: このオプションを有効にすると、ステート終了時に次のステートへは遷移せず、指定した `Repeat Destination` ステートへ戻ります。これを `Repeat Count` で指定した回数繰り返します。指定回数を超えると、通常どおり次のステートへ遷移します。
    -   上の図の場合：`CalculationTask`終了後に`CalculationTask`へ戻る動作を5回繰り返し、6回目の`CalculationTask`の終了後には次の`Trial - Start`へ進みます。
-   **Questionnaire**: このステート中に表示するアンケートを設定します。
    -   **qID**: LUIDAウェブコンソールで登録したアンケートのID（登録順を示す1から始まる番号）を入力します。例えば、下のウェブコンソールのスクショの中で、2番目に登録した「IPQ プレゼンス質問票」をこのステートで表示したい場合は、`qID` に `2` を設定します。　<img width="500" alt="ウェブコンソールでのアンケートID確認例" src="https://github.com/user-attachments/assets/39d122b1-d725-4dfc-8f6e-d9e2d279d622" />

## 実験進行に合わせたオブジェクトの挙動の設定

`LUIDA > Configure experiment automation > State-listening Items`では、実験の進行（ステートの遷移）に合わせてオブジェクトの動作を制御できます。

動作の例：自身や子オブジェクトの表示/非表示、自身や子オブジェクトの位置・回転の設定、テキスト内容の変更、コントローラを振動させる、数秒間待機、カスタムな動作（ご自身でコーディング）など。

_*特定のステートに依存しないオブジェクトや動作は、この機能ではなく、通常のUnityおよびCluster Creator Kit（CCK）の方法でも作成・設定できます。_

![Luida Editor State-listening Itemsタブ](https://github.com/user-attachments/assets/1937e67a-8137-482e-95c5-6ce359d00259)

各設定項目の説明：
1.   **New Item Name** フィールドに管理用の名前を入力し、「Create New Listening Item」ボタンをクリックすると、新しいGameObjectがシーンの中で作成され、設定画面にも新しい列が追加されます。
2.   各列の上部に`Custom Implementation not listening to any state`の枠があり、ステート遷移に依存しない定数、関数やコールバック（掴まれた時、衝突が起きた時など）はここで定義可能
3.   オブジェクト（列）と、動作を紐付けたいステート（行）が交差するセルで「Add Listener」ボタンをクリックします。するとステートの遷移に応じて動作するリスナーが作成されます。
4.   リスナーの中に、以下のタイミングで実行する処理（Action）を設定します。
  -  タイミング
      -   **On State Start**: このステートが開始されたときに一度だけ実行される
      -   **During State**: このステートがアクティブな間、毎フレーム実行される
      -   **On State Exit**: このステートが終了するときに一度だけ実行される
  -   Action
      -   Show Item, Hide Item, Set Text, Set Positionなどの選べられる項目。追加できる項目は設定画面の右側に列挙されています。
          - <img width="200" alt="Screenshot 2025-05-27 at 18 49 05" src="https://github.com/user-attachments/assets/2e1ff4ae-a776-47ee-a6f6-7aa110b51838" />
      -  Customized Actionにすると、コードブロックが表示されます。そこで自前のClusterScriptを書くことができます。使える関数は[専用ドキュメント](/Assets/Doc/LUIDA-StateListeningItemScriptDoc.md)をご参照ください（設定画面の右側からもアクセスできます↓）。
          - <img width="848" height="170" alt="image" src="https://github.com/user-attachments/assets/fa0344e6-0205-4b9f-b027-65c58162df0c" />

## カスタム形式で記録したいデータの定義

カスタムな形式で記録したいデータは、この画面で定義できます。

シーンの中にあるゲームオブジェクト `LUIDA-DataCollector` のコンポーネント `Luida Data Collector` の `Script Asset`には、記録するデータを前処理してから保存する形式を定義するスクリプトを記述できます。
Inspectorからその`Script Asset`をダブルクリックすると、スクリプトの編集画面が開きます（下の図を参照）。

<img width="636" height="347" alt="data-collector" src="https://github.com/user-attachments/assets/80ca5514-99ae-4802-9520-08f493d92577" />

スクリプトの例：

<img width="777" height="212" alt="image" src="https://github.com/user-attachments/assets/bfee9490-ffd5-438e-a326-b335521680b4" />


以下はDataCollectorでスクリプトを書く際の注意事項です：
- `CONDITION['変数の名前']`で、`Process and save collected data`が実行される際の実験変数の値を参照できます。
- `PARTICIPANTS[1や1以上の整数]`で、1-based indexingを使用しています。たとえば、`PARTICIPANTS[1]`は最初の参加者のPlayerHandleを参照し、`PARTICIPANTS[2]`は2番目の参加者のPlayerHandleを参照します。
- 必ず `return { ... };` のような形式で、JavaScriptのObjectを一つ返すようにしてください。たとえば：
```JavaScript
return {
  label1: value1,
  label2: value2,
};
```
- returnする前に、Cluster Scriptを使って何かの計算や前処理を行うことができます。

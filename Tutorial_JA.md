# LUIDA 実装テンプレート チュートリアル

このチュートリアルでは、本実装テンプレートを用いて LUIDA 上で動く実験系を実装する方法を、実際に簡単な実験を一つ作ってもらいながら紹介します。

## 本実装テンプレートの大まかな使用手順

0.  インストール・アカウント作成・CCK などの勉強
1.  LUIDAウェブコンソールで実験を新規作成し、必要な情報を記入
2.  本実装テンプレート（Unity）の初期設定
3.  本実装テンプレートの Luida Editor を用いた実験内容の詳細設定
    1.   実験変数の設定（参加者内変数、参加者間変数）→試行回数が自動的に算出される
    2.   実験進行（ステート遷移）の設計
    3.   質問紙とステートの紐づけ
    4.   ステートに連動するオブジェクトの挙動設定
    5.   カスタム形式で記録したいデータの定義
4.  Unity & CCK を用いるその他の実装
5.  ローカル（Unityのエディター上）でテスト
6.  cluster へのワールドアップロードと動作確認
7.  LUIDA ウェブコンソールでの最終設定（ワールド ID の記入やアバターの設定）
8.  公開待ち

---

## はじめに：インストール・アカウント作成・CCK などの勉強

このチュートリアルを進めるにあたり、以下の基礎知識を事前に学習しておくことを推奨します：

-   Unity
-   JavaScript
-   [Cluster Creator Kit (CCK)](https://docs.cluster.mu/creatorkit/)

また、チュートリアルを開始する前に、以下の準備を完了させてください：

1.  [cluster アカウントの作成](https://help.cluster.mu/hc/articles/115000827112)
2.  [cluster に必要なバージョンの Unity のインストール](https://docs.cluster.mu/creatorkit/installation/install-unity/)
3.  [LUIDAのウェブコンソール](https://luida-web-next.vercel.app/)で初回ログインを行い、承認リクエストを送信してください。承認リクエスト送信後、担当者（y.hu@cluster.mu）までご連絡をお願いします。

---

## 1. ウェブコンソールで実験情報を登録

1.  [LUIDAのウェブコンソール](https://luida-web-next.vercel.app/)を開きます。
2.  **実験を新規作成**：「＋新規実験」をクリックし、実験の基本情報を以下の値で登録します。
    1. タイトル：`ストループ効果の実験`
    2. 参加条件：`色覚異常のない方`
    3. 報酬：`0`
    4. 画像URL：任意の文字列（例: `https://example.com/image.png`）
    5. ワールドID：一旦無視
    6. ルーム定員：`1`
    7. ステータス：`テスト中`

| 新規実験ボタン | 実験基本情報登録画面 |
| :---: | :---: |
| ![質問紙追加ボタン](https://github.com/user-attachments/assets/cc1cc6c5-b0c9-4a48-bf08-daf5d345e04a) | ![テンプレートから選択](https://github.com/user-attachments/assets/a1c19c68-bed3-4b46-8d6e-5abb034f623b) |

3.  **実験詳細ページへ移動**：実験を登録できたら、ホームページからその実験の行をクリックして、その実験の詳細ページにアクセスします。そこで先ほど登録した情報を確認します。
4.  **質問紙の作成**：下にスクロールして、質問紙の登録フォームで以下の操作をします：
    1. 「質問紙追加」ボタン→「テンプレート選択」から「IPQ」を選択→「追加」ボタン
    2. 追加された質問紙の「質問一覧」ボタンをクリックし、追加済みの質問一覧を確認します。
    ![質問紙の作成](https://github.com/user-attachments/assets/f468f40b-83ab-4646-83ad-a28c212a9ec4)
<!--
| 質問紙追加ボタン | テンプレートから選択 | 質問紙一覧 | 選択された質問紙の質問一覧 |
| :---: | :---: | :---: | :---: |
| ![質問紙追加ボタン](https://github.com/user-attachments/assets/484d8882-7d9e-4f7a-bf76-487e565e81e9) | ![テンプレートから選択](https://github.com/user-attachments/assets/f497bca7-5cb8-4000-ac6a-2dc8a562f067) | ![質問紙一覧](https://github.com/user-attachments/assets/40566274-f861-4427-9b50-8dbc4638f3cb) | ![選択された質問紙の質問一覧](https://github.com/user-attachments/assets/bd590c3c-1e27-4288-a3a4-54dec286a529) |
-->
5.  **実験IDをコピー**：実験詳細ページの上部に表示される「実験ID」をコピーしておきます。このIDは後ほどUnityプロジェクトで使用します。
    ![実験IDの確認箇所](https://github.com/user-attachments/assets/20780d32-15ce-4588-a377-415b6d0fef40)

<!--
詳細ページ内の操作（手順3~4）のデモ動画はこちら：https://github.com/user-attachments/assets/06e67729-6fa2-4a23-9b49-0b6cf7b4b45c

| 実験詳細編集フォーム | 質問紙登録フォーム |
| :---: | :---: |
| ![実験詳細ページの編集画面例](https://github.com/user-attachments/assets/53ec6252-f623-415c-ba50-4d6a27d7a86b) | ![アンケート登録画面例](https://github.com/user-attachments/assets/f32d0b65-9551-482d-a7ad-09ecfba822df) |
-->
---

## 2. 実装テンプレート(Unity)のダウンロードと初期設定

1.  [最新リリースの実装テンプレート](https://github.com/cluster-lab/project-luida-bar/releases)をダウンロードします。
2.  ダウンロードしたUnityプロジェクトをUnity Hubから開きます。プロジェクトを開いた際にコンソールにエラーが表示されることがありますが、まずは無視して進み、以下の必須パッケージをUnityにインポートしてください。
    *   [**CSCombiner: Cluster Scriptを Unity Editor 上で結合するツール**](https://vkao.booth.pm/items/5924956) (ver1.01 推奨)
    *   [**CSEmulator: Cluster Scriptを Unity Editor 上で再生できるようにするツール**](https://vkao.booth.pm/items/5111235) (最新バージョンを推奨)
3.  Unityのメニューバーから `Window > Luida Editor` を開きます。初回起動時など、以下の画面が表示された場合は、まずご自身が実装する実験用のシーン名（例: `MyExperimentScene`）を入力してください。
    ![Luida Editor 初期画面（シーン名入力）](https://github.com/user-attachments/assets/be969afc-0dc8-43a3-995b-ae8f420a5e5b)
4.  次に、clusterのアクセストークンを発行し、Unityプロジェクトに登録します。詳細な手順は以下の画像を参照してください。
    ![clusterアクセストークン登録手順](https://github.com/user-attachments/assets/c06f43c6-3412-4462-92a9-ac3576252e99)
5.  続いて、clusterの外部通信機能用のURLを登録し、生成されたトークンを本実装テンプレートに登録します。手順は以下の画像を参照してください。
    ![cluster外部通信URL登録手順](https://github.com/user-attachments/assets/f64e75df-93f2-4b1a-9b3a-36216405feb7)
6.  `Window > Luida Editor` を開き、`Experiment Identifiers` セクション（またはタブ）に、先ほどウェブコンソールでコピーした実験IDを `Experiment ID` フィールドに入力します。
    ![Luida Editor 実験ID登録画面](https://github.com/user-attachments/assets/9d216be0-5c45-41f2-9129-f46546f940ae)

---

## 3. 実験変数と試行回数を設定する

![実験変数と試行回数の設定](https://github.com/user-attachments/assets/3cc430a4-1af1-40b0-8eab-050c0a397ece)

この画面では、実験の参加者内/参加者間変数を登録できます。それに基づいて、システムが自動的に試行の数と各試行における実験条件を決定します。

<details>
    
<summary> **各設定項目の説明** </summary>

* `Variables for Within-Subject Conditions`は参加者内変数の設定フォームで、`Variables for Between-Subject Conditions`は参加者間変数の設定フォームです。
* 各フォームの`Length`：登録する参加者内/間変数の数。その数だけ変数を設定する行が下に現れます。
* 変数設定の行の各項目：
    * `Name`：変数の名前
    * `Values`：変数の値のリスト。カンマ（`,`）区切りで複数の値を指定します。
    * `isRandom`：ランダム順で施行されるか。チェックしない場合はValueの値の順番通りに施行されます。
        * 現在、参加者間変数では`isRandom`をtrueにしか設定できません。つまりある参加者に割り当てられる参加者間条件はランダムになっています。今後は参加者の事前アンケートの回答などに基づいて参加者間条件を割り当てる仕組みを実装する予定です。
* `Trials Count per Condition`：各条件（変数の組み合わせ）あたりで行う試行の回数を入力します。
    ![variable-settings](https://github.com/user-attachments/assets/1cf257e1-71f7-49de-95a9-d38e3661b655)

</details>

以下の手順で登録してください。

1.  Unityのメニューバーから `Window > Luida Editor` を開き、`Experiment Variables` タブに切り替えます。「Create New Variables Asset」ボタンをクリックして、実験変数設定用の新しいアセットファイルを作成します。
2.  **参加者内変数の設定**
    1.  `Variables for Within-Subject Conditions`の下の`Length`に`3`を入力します。
    2.  下に現れた3つの行に以下の値を順番通りに入力します。
        -  Name: `request`　　Values: `material,text`　　isRandom: false
        -  Name: `font`　　Values: `R,B`　　isRandom: true
        -  Name: `text`　　Values: `Red,Blue`　　isRandom: true
3.  **参加者間変数の設定**
    1.  `Variables for Between-Subject Conditions`の下の`Length`に`1`を入力します。
    2.  下に現れた1つの行に以下の値を入力します。
        -  Name: `depth`　　Values: `near,far`
4.  `Trials Count per Condition` に、`2`を入力します。すると各条件（変数の組み合わせ）を持つ試行は2回ずつになります。
5.  最後にUnityのシーンを保存します (`File > Save Scenes` または `Ctrl/Cmd + S`)。

---

## 4. 実験進行の設定（ステート遷移）と質問紙の紐づけ

![Luida Editor Experiment Statesタブ](https://github.com/user-attachments/assets/fbbc7f52-af5b-43db-adf7-4d579fed23b3)

ここでは、実験の進行フローを「ステート」と呼ばれる単位で設定します。各ステートは実験の一区切り（例：説明、タスク実行、休憩、質問紙回答など）を表し、それらがどのように遷移するかを定義します。

`Window > Luida Editor` を開き、`States List (& Questionnaires)` タブに切り替えると、いくつかのデフォルトステートが設定されているのが確認できます。

<details>
    
<summary> **各設定項目の説明** </summary>

-   **Move state to**: リスト内でのステートの遷移順を上下に移動させます。
-   **Has Exit Time**: このオプションを有効にすると、指定した時間（`Exit time (seconds)` フィールドに入力）が経過すると自動的に現在のステートが終了し、次のステートへ遷移します。
-   **Is Repeated**: このオプションを有効にすると、ステート終了時に次のステートへは遷移せず、指定した `Repeat Destination` ステートへ戻ります。これを `Repeat Count` で指定した回数繰り返します。指定回数を超えると、通常どおり次のステートへ遷移します。
    -   上の図の場合：`CalculationTask`終了後に`CalculationTask`へ戻る動作を5回繰り返し、6回目の`CalculationTask`の終了後には次の`Trial - Start`へ進みます。
-   **Questionnaire**: このステート中に表示するアンケートを設定します。
    -   **qID**: LUIDAウェブコンソールで登録したアンケートのID（登録順を示す1から始まる番号）を入力します。例えば、下のウェブコンソールのスクショの中で、2番目に登録した「IPQ プレゼンス質問票」をこのステートで表示したい場合は、`qID` に `2` を設定します。　<img width="500" alt="ウェブコンソールでのアンケートID確認例" src="https://github.com/user-attachments/assets/39d122b1-d725-4dfc-8f6e-d9e2d279d622" />

</details>

上の図の設定になるように、以下の手順に従って設定を行ってください。

1. **最初のステートを3秒で自動的に飛ばす**：`Start`の行の`Has Exit Time`をチェックし、`Exit Time`に`3`と入力します。
2. **説明のステートを10秒で自動的に飛ばす**：`Intro`の行の`Has Exit Time`をチェックし、`Exit Time`に`10`と入力します。
3. **計算タスクのステートを追加し、5回繰り返す**：
    1. `Add State Before Trials`ボタンを押し、ステートを追加します。`CalculationTask`と名前を変えてあげます。
    2. `CalculationTask`の行の`Is Repeated`をチェックし、`Repeat Destination`で`CalculationTask`を選択し、`Repeat Count`に`5`と入力します。
4. **試行の休憩ステートを3秒で自動的に飛ばす**：`Trial - Rest`の行の`Has Exit Time`をチェックし、`Exit Time`に`3`と入力します。
5. **試行終了後のステートを10秒で自動的に飛ばす**：`Outro`の行の`Has Exit Time`をチェックし、`Exit Time`に`10`と入力します。
6. **質問紙のステートを追加して質問紙と紐付ける**：
    1. `Add State After Trials`ボタンを押し、ステートを追加します。`Questionnaire`と名前を変えてあげます。
    2. `Add Questionnaire`ボタンを押し、`qID`に`1`と入力します。
7. 最後にUnityのシーンを保存します。

---

## 5. 実験進行に合わせたオブジェクトの挙動の設定（書き換え中）

![Luida Editor State-listening Itemsタブ](https://github.com/user-attachments/assets/1937e67a-8137-482e-95c5-6ce359d00259)

実験の進行（特定のステート）に合わせてオブジェクトの表示/非表示や動作を制御したい場合は、このセクションの設定を行います。特定のステートに依存しないオブジェクトは、通常のUnityおよびCluster Creator Kit（CCK）の方法で作成・設定してください。

`Window > Luida Editor` を開き、`State-listening Items` タブに切り替えます。上の図と以下各項目の説明を合わせて参照しながら設定を行ってください。

1.   **New Item Name** フィールドに管理用の名前を入力し、「Create New Listening Item」ボタンをクリックすると、新しいGameObjectがシーンの中で作成され、設定画面にも新しい列が追加されます。
2.   各列の上部に`Custom Implementation not listening to any state`の枠があり、ステート遷移に依存しない定数、関数やコールバック（掴まれた時、衝突が起きた時など）はここで定義可能
3.   オブジェクト（列）と、動作を紐付けたいステート（行）が交差するセルで「Add Listener」ボタンをクリックします。するとステートの遷移に応じて動作するリスナーが作成されます。
4.   リスナーの中に、以下のタイミングで実行する処理（Action）を設定します。
  - タイミング
    -   **On State Start**: このステートが開始されたときに一度だけ実行される
    -   **During State**: このステートがアクティブな間、毎フレーム実行される
    -   **On State Exit**: このステートが終了するときに一度だけ実行される
  -   Action
    -   Show Item, Hide Item, Set Text, Set Positionなどの選べられる項目。追加できる項目は設定画面の右側に列挙されています。
    -   Customized Actionにすると、コードブロックが表示されます。そこで自前のClusterScriptを書くことができます。使える関数は設定画面の右側に列挙されています。
<img width="200" alt="Screenshot 2025-05-27 at 18 49 05" src="https://github.com/user-attachments/assets/7873d106-a28e-4d34-aa66-da11c6d805c7" />


設定変更後は、Unityのシーンを保存してください。

---

## 6. UnityとCCKを使ったその他の実装（書き換え中）

Unityの Collider や Rigidbody などの標準コンポーネントは使用可能です。
ただし、Cluster上で動作させる関係上、C#スクリプトは使用できません。その代わり、CCKの各種コンポーネントやClusterScriptを活用して開発を行います。

また、先ほどLuida Editorで作成した「実験進行に合わせて動作するオブジェクト」は、デフォルトでは空のGameObjectです。必要に応じて子オブジェクト（Colliderを持つMeshやUIなど）を追加することで、掴む（Grab） や クリックする（Click） といったインタラクションが可能になります。

また、Luida専用のCCK Gimmickとしては以下のものが用意されています：
`Luida To Next State Gimmick`、`Luida Record Custom Data Gimmick`、`Luida Upload Custom Data Gimmick`
_※ `Luida State Listening Item`は自分で**追加しない**でください_

<img width="242" alt="Screenshot 2025-05-27 at 14 11 22" src="https://github.com/user-attachments/assets/8ef65351-3146-4809-b08c-dc20afd31842" />

これにより、例えば、Luida Editorの「State-listening Items」タブでオブジェクトを1つ作成し、ステート遷移に応じて表示・非表示を切り替えるように設定します。
その後、Editorを閉じたあとに、該当オブジェクトに `Interact Item Trigger` と `Luida To Next State Gimmick` を追加し、`Interact Item Trigger` から `Luida To Next State Gimmick` を呼び出すように設定します。
さらに、子オブジェクトとして Collider を持つオブジェクト（例：Cubeなど）を追加すれば、クリックで次のステートに遷移するボタンが完成します。

---

## 7. アップロード前の準備

1.  Unityのメニューバーから `Cluster > 設定` を開き、「ベータ機能を利用する」にチェックを入れます。
    ![ベータ機能の有効化](https://github.com/user-attachments/assets/af786e5e-07fe-4126-b350-1ed7c0401ecd)
2.  Unityのメニューバーから `Window > かおもラボ > CSCombiner` を開き、「全更新」ボタンをクリックします。これにより、プロジェクト内のCluster Script (`.cs.js`ファイル) が正しく結合・処理されます。
    ![CSCombiner 全更新](https://github.com/user-attachments/assets/12cd1c5e-0dcc-4d91-b340-900ed0a35041)
3.  Unityのシーンを保存します。
4.  **ローカルテストプレイ**: Unityエディタの再生ボタンを押して、実験全体の流れや各ステートの動作、オブジェクトの挙動などが意図した通りかを入念に確認します。

---

## 8. cluster にアップロード

1.  clusterの公式ドキュメント [ワールドをアップロードする手順](https://creator.cluster.mu/2020/03/28/%E5%88%B6%E4%BD%9C%E3%81%97%E3%81%9F%E3%83%AF%E3%83%BC%E3%83%AB%E3%83%89%E3%82%92%E3%80%8Ccluster%E3%80%8D%E3%81%AB%E3%82%A2%E3%83%83%E3%83%97%E3%83%AD%E3%83%BC%E3%83%89%E3%81%99%E3%82%8B/) に従って、作成したシーンをclusterのワールドとしてアップロードします。
    *   アップロード後、実際にそのワールドに入室し、実験参加者として一通り体験して動作を確認してください。
    *   この時点では、通常のアバター選択が可能ですが、後述のアバター設定をLUIDAウェブコンソールで行い、実験が正式に公開されると、参加者は指定されたアバター（またはアバター非表示）で実験に参加することになります。
2.  アップロード後に不具合が見つかった場合は、Unityプロジェクトで修正後、「テスト用アップロード」機能を利用して [テスト用スペース](https://creator.cluster.mu/2024/05/24/testspace/) で動作確認を行うことを推奨します。これにより、公開中のワールドに影響を与えることなく修正内容をテストできます。
    *   修正が完了し、テスト用スペースで問題ないことが確認できたら、再度通常のワールドアップロードを行ってください。
3.  clusterワールドでの一通りの動作確認と合わせて、LUIDAウェブコンソール上で、実験データ（質問紙への回答、その他収集設定したログデータなど）が正しく記録・表示されているかを確認してください。
    ![ウェブコンソールでのデータ確認例](https://github.com/user-attachments/assets/9db65b18-7a6e-412d-8908-54a2995bfdb9)

---

## 9. ワールド ID の登録

1.  ウェブブラウザでclusterの公式サイトにログインし、「マイコンテンツ」ページ（または「ワールド」管理画面）で、アップロードした実験ワールドが一覧に表示されていることを確認します。
2.  該当するワールドを選択し、ワールド詳細ページを開きます。そのページのURLの末尾にある英数字の文字列がワールドIDです（例: `https://cluster.mu/w/XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX` の `XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX` の部分）。このワールドIDをコピーします。
    ![clusterマイコンテンツ画面でのワールド確認](https://github.com/user-attachments/assets/44821568-fa20-4f75-9cf1-c49f38b9d4e5)
3.  LUIDAウェブコンソールの該当する実験情報編集画面を開き、コピーしたワールドIDを所定のフィールド（例: 「ワールドID」）に登録・保存します。
    ![ウェブコンソールでのワールドID登録](https://github.com/user-attachments/assets/c5003a53-3ea0-4b72-aa92-ea37e1e2a1d9)

---

## 10. アバターの設定

実験参加者のアバターを非表示にしたい場合や、実験専用の特定アバターを使用させたい場合は、以下の手順で設定します。

1.  LUIDAウェブコンソールで、該当の実験設定ページ内にある `Avatar Settings` セクションで、「Add World-Avatar Set」ボタンをクリックします。
2.  `World ID` フィールドに、先ほど登録した実験ワールドのIDを入力します。
3.  以下のいずれかの設定を行い、「Submit」ボタンをクリックして保存します：
    *   **アバターを隠したい場合**: `Hide Avatar` のみにチェックを入れます。他のフィールドは空のままで構いません。
    *   **特定のアバターを指定したい場合**: `Assign Avatar` のみにチェックを入れ、以下の情報を入力・アップロードします。
        *   `Avatar Name`: アバターの名前（管理用）
        *   `VRM Version`: 使用するVRMモデルのバージョン（例: `0.x` または `1.0`）
        *   `Upload VRM`: アバターのVRMファイル
        *   `Upload Thumbnail (PNG)`: アバターのサムネイル画像（PNG形式推奨）

    ![LUIDAウェブコンソールでのアバター設定画面](https://github.com/user-attachments/assets/4af44d34-eb78-4080-9241-09459874c1a6)

---

## 11. LUIDA での自動掲載を待つ

上記までのすべての設定が完了すると、あなたが作成した実験はLUIDAの参加者募集ワールドに掲載される準備が整います。掲載まで数日程度お待ちいただく場合があります。

\*注：将来的には、新たに登録された実験がより迅速に（例：1日以内など）公開されるよう、LUIDAの実験情報更新プロセスを改善していく予定です。

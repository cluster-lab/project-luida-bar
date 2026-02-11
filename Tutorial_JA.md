# LUIDA 実装テンプレート チュートリアル

このチュートリアルでは、本実装テンプレートを用いて LUIDA 上で動く実験系を実装する方法を、実際に簡単な実験を一つ作ってもらいながら紹介します。

## 本実装テンプレートの大まかな使用手順

0.  インストール・アカウント作成・CCK などの勉強
1.  LUIDA ウェブコンソールで実験を新規作成し、必要な情報を記入
2.  本実装テンプレート（Unity）の初期設定
3.  シーン内のオブジェクトにデータ収集機能を設定
4.  ローカル（Unity のエディター上）でテスト & アップロード前の準備
5.  cluster へのワールドアップロードと動作確認
6.  LUIDA ウェブコンソールでの最終設定（ワールド ID の記入やアバターの設定）
7.  公開待ち

---

## はじめに：インストール・アカウント作成・CCK などの勉強

このチュートリアルを進めるにあたり、以下の基礎知識を事前に学習しておくことを推奨します：

- Unity
- JavaScript
- [Cluster Creator Kit (CCK)](https://docs.cluster.mu/creatorkit/)
  - [トリガーとギミック](https://docs.cluster.mu/creatorkit/world/trigger-gimmick/)だけでも目を通しておく

また、チュートリアルを開始する前に、以下の準備を完了させてください：

1.  [cluster アカウントの作成](https://help.cluster.mu/hc/articles/115000827112)
2.  [cluster に必要なバージョンの Unity のインストール](https://docs.cluster.mu/creatorkit/installation/install-unity/)
3.  [LUIDA のウェブコンソール](https://luida.cluster.mu/experiments)で初回ログインを行い、承認リクエストを送信してください。承認リクエスト送信後、担当者（y.hu@cluster.mu）までご連絡をお願いします。

---

## 0. 本チュートリアルで実装する実験計画の確認

本実験では、プライミング効果を検証します。参加者はまず、「高齢者が歩いている動画」または「若者が走っている動画」のいずれかをランダムに視聴します。動画の視聴後、安全そうな橋と危険そうな橋の 2 つが出現し、参加者はどちらかの橋を渡ります。本実験では、視聴した動画の内容が参加者の橋の選択に影響を与えるかどうかを検証します。

- 実験手順
  - 開始 → デモグラフィクス質問紙 → 動画視聴（ランダムに選ばれる） → 橋の選択 → 終了
- 記録するデータ
  - どちらの動画が提示されたか（`isElderVideo`: true / false）
  - どちらの橋を渡ったか（`isSafeRoad`: true / false）
- 質問紙
  - デモグラフィクス（年齢）

<img width="902" height="503" alt="スクリーンショット 2026-02-12 022027" src="https://github.com/user-attachments/assets/f519c528-9af6-48ea-8cf6-1c23ce2b19e8" />

---

## 1. ウェブコンソールで実験情報を登録

<details>
<summary><h3>こちらの手順に従って登録・設定してください</h3></summary>

1.  [LUIDA のウェブコンソール](https://luida.cluster.mu/experiments)を開きます。
2.  **実験を新規作成**：「＋新規実験」をクリックし、実験の基本情報を以下の値で登録します。
    1. タイトル：`プライミング効果の実験`
    2. 参加条件：（任意）
    3. 報酬：`0`
    4. 画像 URL：任意の文字列（例: `https://example.com/image.png`）
    5. ワールド ID：一旦無視
    6. ルーム定員：`1`
    7. ステータス：`テスト中`

|                                            新規実験ボタン                                            |                                           実験基本情報登録画面                                           |
| :--------------------------------------------------------------------------------------------------: | :------------------------------------------------------------------------------------------------------: |
| ![質問紙追加ボタン](https://github.com/user-attachments/assets/cc1cc6c5-b0c9-4a48-bf08-daf5d345e04a) | ![テンプレートから選択](https://github.com/user-attachments/assets/a1c19c68-bed3-4b46-8d6e-5abb034f623b) |

3.  **実験詳細ページへ移動**：実験を登録できたら、ホームページからその実験の行をクリックして、その実験の詳細ページにアクセスします。そこで先ほど登録した情報を確認します。
4.  **質問紙の作成**：下にスクロールして、質問紙の登録フォームで以下の操作をします：
    1. 「質問紙追加」ボタン → テンプレートを選択せず、タイトルを `Demographics` と入力 →「追加」ボタン
    2. 追加された質問紙の「質問一覧」ボタンをクリックし、以下の質問項目を追加します：
       - タイトル：`Age`、質問タイプ：`Text Input`、必須：はい

<img width="2096" height="963" alt="LUIDA-sample-questionnaire-configuration" src="https://github.com/user-attachments/assets/e7133ff1-ba19-44d3-8192-fc1c1d5f0c75" />

5.  **実験 ID をコピー**：実験詳細ページの上部に表示される「実験 ID」をコピーしておきます。この ID は後ほど Unity プロジェクトで使用します。
    ![実験IDの確認箇所](https://github.com/user-attachments/assets/20780d32-15ce-4588-a377-415b6d0fef40)

</details>

---

## 2. 実装テンプレート(Unity)のダウンロードと初期設定

<details>
<summary><h3>こちらの手順に従って登録・設定してください</h3></summary>
＊こちらの手順の一部は今後自動化する予定です。現状はお手数ですが一通りおこなってください。

1.  [最新リリースの実装テンプレート](https://github.com/cluster-lab/project-luida-bar/releases)をダウンロードします。
2.  ダウンロードした Unity プロジェクトを Unity Hub から開きます。プロジェクトを開いた際にコンソールにエラーが表示されることがありますが、まずは無視して進み、以下の必須パッケージを Unity にインポートしてください。
    - [**CSCombiner: Cluster Script を Unity Editor 上で結合するツール**](https://vkao.booth.pm/items/5924956) (ver1.01 推奨)
    - [**CSEmulator: Cluster Script を Unity Editor 上で再生できるようにするツール**](https://vkao.booth.pm/items/5111235) (現状は V3 ではなく**V2**でお願いします\*\*)
3.  サンプルシーン `Assets/_Experiment_/Scenes/Sample/Priming_incomplete.unity` を開き、Unity エディタの再生ボタンを押して、まずはシーンの様子を確認してみましょう。
4.  確認が済んだら、Unity のメニューバーから `LUIDA > Scene > Duplicate current scene` を選択し、表示されるダイアログでご自身の名前を入力して新しいシーンを作成します。以降はこの新しいシーン上で作業を進めます。

    <img width="1038" height="301" alt="スクリーンショット 2026-02-12 023657" src="https://github.com/user-attachments/assets/77a1c3af-2863-43eb-9768-75dc473f07ff" />
    <img width="323" height="131" alt="スクリーンショット 2026-02-12 023710" src="https://github.com/user-attachments/assets/ddef3341-d53b-42c8-a950-eefbad3ee3dc" />

5.  次に、cluster アカウントと Unity プロジェクトを連携します。手順は以下の通りです。

    1.  Unity のメニューバーから `LUIDA > Configure experiment identifiers` を選択し、ID設定用の画面を開きます。
    2.  `Create Access Token`ボタンをクリックすると、Web ブラウザで cluster のアクセストークン発行ページが開きます。
    3.  ブラウザ上で `Create Token`ボタンをクリックして表示されたトークンをコピーし、先ほど開いた Unity 側のID設定用画面の `Access Token` フィールドに貼り付けます。

    ![clusterアクセストークン登録手順](https://github.com/user-attachments/assets/e3f23566-e1b0-459b-8586-58ee897b7616)

6.  ID設定用画面が開いたまま、先ほどウェブコンソールでコピーした実験 ID を `Experiment ID` フィールドに入力します。

    ![Luida Editor 実験ID登録画面](https://github.com/user-attachments/assets/45afbd6b-502b-40ef-a1f7-b2c2c8ef9c30)

7. Verify Token がまだ生成されていなければ `Generate a new verify token` のボタンを押します。

   ![Verify Token 生成](https://github.com/user-attachments/assets/f4fcf341-d44a-4ad1-ab31-147f01a3dde0)

8. ID設定用画面を閉じます。

</details>

---

## 3. 実験の完成（データ収集機能の設定）

新しいシーンで、データを収集して LUIDA のバックエンドにアップロードできるように、シーン内のオブジェクトにコンポーネントを設定していきます。

<details>

<summary><h3>StartVideoButton の設定</h3></summary>

シーン内のゲームオブジェクト `StartVideoButton` を選択し、コンポーネント `Global Trigger Lottery` を確認します。

1. `+` ボタンを 2 回押して、2 つの選択肢を追加します。
2. それぞれの選択肢に以下のトリガーを設定します：
   - 1 つ目：`Global` トリガー、Key = `isElderVideo`、Value Type = `Bool`、Value = `true`
   - 2 つ目：`Global` トリガー、Key = `isElderVideo`、Value Type = `Bool`、Value = `false`

これにより、ボタンを押した際にどちらの動画が再生されるかがランダムに決まり、その結果が `isElderVideo` として記録されます。

<img width="786" height="695" alt="スクリーンショット 2026-02-12 020601" src="https://github.com/user-attachments/assets/a7ce183a-be0a-46ec-86cb-b7412f6cbcc0" />

</details>

<details>

<summary><h3>SafeRoadCheckPoint の設定</h3></summary>

シーン内のゲームオブジェクト `SafeRoadCheckPoint` を選択し、コンポーネント `On Collide Item Trigger` を確認します。

1. `+` ボタンを 3 回押して、トリガーを 3 つ追加し、以下の図の通りに値を設定します。
2. さらに、以下の 2 つのコンポーネントを追加します：
   - `Luida Process Data And Save To Collection Gimmick`
   - `Luida Upload Collected Data Gimmick`

設定後、以下の図のようになります：

<img width="779" height="826" alt="スクリーンショット 2026-02-12 020954" src="https://github.com/user-attachments/assets/bc1761f1-f780-400b-ab68-f7fe9875b360" />

図の解説：
- **赤枠**：データ収集の設定 — このチェックポイントを通過したら `isSafeRoad=true` を記録する
- **黄枠**：データ保存のトリガーとギミック — このチェックポイントを通過したらデータをコレクションに保存する
- **緑枠**：データアップロードのトリガーとギミック — このチェックポイントを通過したら収集したデータをアップロードする

</details>

<details>

<summary><h3>DangerousRoadCheckPoint の設定</h3></summary>

シーン内のゲームオブジェクト `DangerousRoadCheckPoint` を選択し、`SafeRoadCheckPoint` と同じ手順でコンポーネントとトリガーを設定します。

**唯一の違い**：`isSafeRoad` の値を `false` に設定してください。

<img width="779" height="834" alt="スクリーンショット 2026-02-12 020959" src="https://github.com/user-attachments/assets/e36ea3ac-1dc5-41da-8984-252c96614924" />

</details>

<details>

<summary><h3>LUIDA-DataCollector のスクリプト編集</h3></summary>

シーン内のゲームオブジェクト `LUIDA-DataCollector` を選択し、Inspector のコンポーネントにある `Script Asset` をダブルクリックします（下の図を参照）。するとスクリプトの編集画面が開きます。

<img width="668" height="350" alt="スクリーンショット 2026-02-12 030242" src="https://github.com/user-attachments/assets/3ba4959d-0be6-4398-a622-7355fbe3d18f" />

中身を以下のスクリプトに置き換えてファイル保存します：

```JavaScript
return {
    video: $.getStateCompat("global", "isElderVideo", "boolean") ? "Elder" : "Young", // どちらの動画が提示されたか
    road: $.getStateCompat("global", "isSafeRoad", "boolean") ? "Safe" : "Danger", // どちらの橋を渡ったか
};
```

このスクリプトは、収集したデータの形式を定義しています。CCK のコンポーネントから Global 宛てに送られた `isElderVideo` と `isSafeRoad` の値を `$.getStateCompat` で取得しています。

</details>

<details>

<summary><h3>質問紙の追加</h3></summary>

1. Hierarchy ウィンドウで右クリックし、`LUIDA > Questionnaire` を選択します。
2. 表示される画面で `qID` に `1` を入力し、`Create` ボタンを押します。

すると質問紙のゲームオブジェクトが生成されます。

3. 生成された質問紙オブジェクトをクリックし、Inspector で以下のコンポーネントを追加します：
   - `Global Logic`
   - `Set Game Object Active Gimmick`
4. 追加したコンポーネントの値を以下の図の通りに設定します。

<img width="730" height="623" alt="スクリーンショット 2026-02-12 022700" src="https://github.com/user-attachments/assets/aad277ef-24ea-4b72-8fc6-47d530896143" />

この設定により、参加者が質問紙への回答を終えた後に、質問紙が自動的に非表示になります。

</details>

---

## 4. アップロード前の準備

<details>
<summary>以下の手順に従って設定を行ってください</summary>

1.  Unity のメニューバーから `Cluster > 設定` を開き、「ベータ機能を利用する」にチェックを入れます。
    ![ベータ機能の有効化](https://github.com/user-attachments/assets/af786e5e-07fe-4126-b350-1ed7c0401ecd)
2.  **ローカルテストプレイ**: Unity エディタの再生ボタンを押して、実験全体の流れ（動画の再生、橋の選択、質問紙の表示など）が意図した通りかを確認します。
3.  シーンをセーブします。

</details>

---

## 5. cluster にアップロード

<details>
<summary>以下の手順に従って設定を行ってください</summary>

1.  cluster の公式ドキュメント [ワールドをアップロードする手順](https://creator.cluster.mu/2020/03/28/%E5%88%B6%E4%BD%9C%E3%81%97%E3%81%9F%E3%83%AF%E3%83%BC%E3%83%AB%E3%83%89%E3%82%92%E3%80%8Ccluster%E3%80%8D%E3%81%AB%E3%82%A2%E3%83%83%E3%83%97%E3%83%AD%E3%83%BC%E3%83%89%E3%81%99%E3%82%8B/) に従って、作成したシーンを cluster のワールドとしてアップロードします。
    - アップロード後、実際にそのワールドに入室し、実験参加者として一通り体験して動作を確認してください。
    - この時点では、通常のアバター選択が可能ですが、後述のアバター設定を LUIDA ウェブコンソールで行い、実験が正式に公開されると、参加者は指定されたアバター（またはアバター非表示）で実験に参加することになります。
2.  アップロード後に不具合が見つかった場合は、Unity プロジェクトで修正後、「テスト用アップロード」機能を利用して [テスト用スペース](https://creator.cluster.mu/2024/05/24/testspace/) で動作確認を行うことを推奨します。これにより、公開中のワールドに影響を与えることなく修正内容をテストできます。
    - 修正が完了し、テスト用スペースで問題ないことが確認できたら、再度通常のワールドアップロードを行ってください。
3.  cluster ワールドでの一通りの動作確認と合わせて、LUIDA ウェブコンソール上で、実験データ（質問紙への回答、その他収集設定したログデータなど）が正しく記録・表示されているかを確認してください。
    ![ウェブコンソールでのデータ確認例](https://github.com/user-attachments/assets/9db65b18-7a6e-412d-8908-54a2995bfdb9)

</details>

---

## 6. ワールド ID の登録

<details>
<summary>以下の手順に従って設定を行ってください</summary>

1.  ウェブブラウザで cluster の公式サイトにログインし、「マイコンテンツ」ページ（または「ワールド」管理画面）で、アップロードした実験ワールドが一覧に表示されていることを確認します。
2.  該当するワールドを選択し、ワールド詳細ページを開きます。そのページの URL の末尾にある英数字の文字列がワールド ID です（例: `https://cluster.mu/w/XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX` の `XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX` の部分）。このワールド ID をコピーします。
    ![clusterマイコンテンツ画面でのワールド確認](https://github.com/user-attachments/assets/44821568-fa20-4f75-9cf1-c49f38b9d4e5)
3.  LUIDA ウェブコンソールの該当する実験情報編集画面を開き、コピーしたワールド ID を所定のフィールド（例: 「ワールド ID」）に登録・保存します。
    ![ウェブコンソールでのワールドID登録](https://github.com/user-attachments/assets/c5003a53-3ea0-4b72-aa92-ea37e1e2a1d9)

</details>

---

## 7. アバターの設定

<details>
<summary>以下の手順に従って設定を行ってください</summary>

このチュートリアルで作る実験では、実験参加者のアバターを設定する必要はないが、
もしアバターを非表示にしたい場合や、実験専用の特定アバターを使用させたい場合は、以下の手順で設定します。

1.  LUIDA ウェブコンソールで、該当の実験設定ページ内にある `Avatar Settings` セクションで、「Add World-Avatar Set」ボタンをクリックします。
2.  `World ID` フィールドに、先ほど登録した実験ワールドの ID を入力します。
3.  以下のいずれかの設定を行い、「Submit」ボタンをクリックして保存します：

    - **アバターを隠したい場合**: `Hide Avatar` のみにチェックを入れます。他のフィールドは空のままで構いません。
    - **特定のアバターを指定したい場合**: `Assign Avatar` のみにチェックを入れ、以下の情報を入力・アップロードします。
      - `Avatar Name`: アバターの名前（管理用）
      - `VRM Version`: 使用する VRM モデルのバージョン（例: `0.x` または `1.0`）
      - `Upload VRM`: アバターの VRM ファイル
      - `Upload Thumbnail (PNG)`: アバターのサムネイル画像（PNG 形式推奨）

    ![LUIDAウェブコンソールでのアバター設定画面](https://github.com/user-attachments/assets/4af44d34-eb78-4080-9241-09459874c1a6)

</details>

---

## 8. LUIDA での自動掲載を待つ

上記までのすべての設定が完了すると、あなたが作成した実験は LUIDA の参加者募集ワールドに掲載される準備が整います。掲載まで数日程度お待ちいただく場合があります。

\*注：将来的には、新たに登録された実験がより迅速に（例：1 日以内など）公開されるよう、LUIDA の実験情報更新プロセスを改善していく予定です。

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
    -   [トリガーとギミック](https://docs.cluster.mu/creatorkit/world/trigger-gimmick/)だけでも目を通しておく

また、チュートリアルを開始する前に、以下の準備を完了させてください：

1.  [cluster アカウントの作成](https://help.cluster.mu/hc/articles/115000827112)
2.  [cluster に必要なバージョンの Unity のインストール](https://docs.cluster.mu/creatorkit/installation/install-unity/)
3.  [LUIDAのウェブコンソール](https://luida-web-next.vercel.app/)で初回ログインを行い、承認リクエストを送信してください。承認リクエスト送信後、担当者（y.hu@cluster.mu）までご連絡をお願いします。

---

## 0. 本チュートリアルで実装する実験計画の確認

本研究では、VR空間においてストループ効果（色と文字の意味が異なる場合に、文字の色を認識するのに時間がかかる現象）を再現し、
まずは計算課題を繰り返し、参加者に認知負荷をかけた後に、
刺激提示の奥行き条件（近／遠）が反応時間および正答率に与える影響を検証する。
- 実験手順
    - 開始 → 説明 → 計算課題の繰り返し → メインタスクを条件ごとに2回ずつ繰り返す → プレゼンスに関する質問紙 → 終了
- （メインタスクにおける）実験条件
    - 参加者間条件：奥行き（近／遠）
    - 参加者内条件：
        - 回答対象：まずフォントの色を回答し、次にテキストの意味を回答する
        - フォントの色：赤または青（ランダム呈示）
        - テキストの意味：「Red」または「Blue」（ランダム呈示）
    - 各条件は2回施す
- （メインタスクにおける）評価指標：
    - 各条件における回答結果（後に正答率を算出可能）
    - 各条件における回答時間

実験の様子（開始から質問紙が始まるまで）：
https://github.com/user-attachments/assets/852259cb-f871-4589-bdf1-cb37cefde213

---

## 1. ウェブコンソールで実験情報を登録

<details>
<summary><h3>こちらの手順に従って登録・設定してください</h3></summary>

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
    1. 実験前質問紙
        1. 「質問紙追加」ボタン→テンプレートを選択せず、任意のタイトルと説明を入力→「追加」ボタン
        2. 追加された質問紙の「質問一覧」ボタンをクリックし、フォームに入力して「＋質問追加」ボタンを押すことで質問項目を増やします。質問項目例：
            -  タイトル：`性別`、回答候補：`男性,女性,その他`、質問タイプ：`Radio Button`
            -  タイトル：`年齢`、質問タイプ：`Text Input`
            -  タイトル：`VRを頻繁に体験している`、説明：`1: まったく当てはまらない, 2: 当てはまらない, 3: どちらかと言えば当てはまらない, 4: どちらでもない, 5: どちらかと言えば当てはまる, 6: 当てはまる, 7: 完全に当てはまる`、回答候補：`1,2,3,4,5,6,7`、質問タイプ：`Linear Scale`
    2. 実験後質問紙 
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

</details>

---

## 2. 実装テンプレート(Unity)のダウンロードと初期設定

<details>
<summary><h3>こちらの手順に従って登録・設定してください</h3></summary>
＊こちらの手順の一部は今後自動化する予定です。現状はお手数ですが一通りおこなってください。


1.  [最新リリースの実装テンプレート](https://github.com/cluster-lab/project-luida-bar/releases)をダウンロードします。
2.  ダウンロードしたUnityプロジェクトをUnity Hubから開きます。プロジェクトを開いた際にコンソールにエラーが表示されることがありますが、まずは無視して進み、以下の必須パッケージをUnityにインポートしてください。
    *   [**CSCombiner: Cluster Scriptを Unity Editor 上で結合するツール**](https://vkao.booth.pm/items/5924956) (ver1.01 推奨)
    *   [**CSEmulator: Cluster Scriptを Unity Editor 上で再生できるようにするツール**](https://vkao.booth.pm/items/5111235) (最新バージョンを推奨)
3.  Unityのメニューバーから `LUIDA > Scene > Create New Scene` のウィンドウを開き、ご自身が実装する実験用のシーン名（例: `MyExperimentScene`）を入力した上でOKボタンを押します。
       <img width="482" height="120" alt="image" src="https://github.com/user-attachments/assets/78e2852f-3fd0-4655-a9c5-7a5efa189eba" />
        <img width="322" height="138" alt="image" src="https://github.com/user-attachments/assets/925099e3-3089-4a92-80ef-29e7d7200e1e" />

4.  次に、clusterのアクセストークンを発行し、Unityプロジェクトに登録します。詳細な手順は以下の画像を参照してください。
    ![clusterアクセストークン登録手順](https://github.com/user-attachments/assets/c06f43c6-3412-4462-92a9-ac3576252e99)
5.  続いて、clusterの外部通信接続先URLを登録し、そこで生成された識別子（Endpoint ID）とVerify Tokenを、本実装テンプレートに登録します。手順は以下の画像を順番に参照してください。

| 1. 外部通信接続先URLの設定画面を開く | 2. 外部通信接続先URLを登録する：`https://luida-web-next.vercel.app/api/cluster` | 3. 外部通信用Verify Tokenを生成させる |
| :---: | :---: | :---: |
| ![registerTokenStep1](https://github.com/user-attachments/assets/a207e2d5-7c40-4a39-b910-b22218d8054c) | ![registerTokenStep2](https://github.com/user-attachments/assets/fa658c8d-9c98-48ee-a322-93b60e067d6c) | ![registerTokenStep3](https://github.com/user-attachments/assets/584d1acd-5571-4c9a-89ba-b721200d7365) |

| 4. LUIDA > Configure experiment identifiers を開く | 5. 先ほど登録したURLの識別子（Endpoint ID）を`callExternal Endpoint ID`に、Verify Tokenを`Verify Token`にコピーする |
| :---: | :---: |
| ![registerTokenStep4](https://github.com/user-attachments/assets/2315ce7e-67ad-4989-861a-d2950d9f0617) | ![registerTokenStep5](https://github.com/user-attachments/assets/24f5b63f-61dd-4f60-a389-0269f3543f7e) |

6.  `Configure experiment identifiers` の画面に残り、先ほどウェブコンソールでコピーした実験IDを `Experiment ID` フィールドに入力したうえで、画面を閉じます。
    ![Luida Editor 実験ID登録画面](https://github.com/user-attachments/assets/d5e9efbd-9309-458e-9997-5cda7a4f2904)

7. `Window > かおもラボ > CSEmulator`の`callExternal用Endpoint`設定フィールドに、先ほどの `Endpoint ID` と `https://luida-web-next.vercel.app/api/cluster`で埋めます。
    <img width="928" height="290" alt="image" src="https://github.com/user-attachments/assets/31b160e0-e1cd-4dbf-a8fd-8c907de48015" />

</details>

---

## 3. 実験変数と試行回数を設定する

`LUIDA > Configure experiment automation > Experiment Variables`画面（左図）で、実験の参加者内/参加者間変数を登録すると、それに基づいてシステムが自動的に試行の数と各試行における実験条件を決定します（右図）。

各設定項目の詳細は[ドキュメント](/Documentation_JA.md#実験変数と試行回数の設定)をご参照ください。

<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/d697ecb9-555c-4a65-90fe-1cc295cabc8d" alt="variables-config-ui" width="600"/></td>
    <td><img src="https://github.com/user-attachments/assets/d888c793-3c0c-4215-94a3-2eb5d95db4e6" alt="variables-config-example" width="600"/></td>
  </tr>
</table>

<details>
<summary><h3>こちらの手順に従って設定してください</h3></summary>

1.  Unityのメニューバーから `LUIDA > Configure experiment automation` ウィンドウを開き、ボタン`Activate Experiment Automation Feature`をクリックします

    <img width="767" height="207" alt="Configure experiment automation window" src="https://github.com/user-attachments/assets/79dffcb5-7f2e-4f4b-8f20-fd824558c3e3" />

2.  `Experiment Variables` タブが開いているのを確認します。
3.  **参加者内変数の設定**：`Variables for Within-Subject Conditions`に3つの項目を追加し、以下の通りに設定します：
    -  Name: `request`　　Values: `font, meaning`　　isRandom: □
    -  Name: `font`　　Values: `R,B`　　isRandom: ☑
    -  Name: `text`　　Values: `Red,Blue`　　isRandom: ☑
4.  **参加者間変数の設定**：`Variables for Between-Subject Conditions`に1つの項目を追加し、以下の通りに設定します：
    -  Name: `depth`　　Values: `near,far`
5.  `Trials Count per Condition` に、`2`を入力します。すると各条件（変数の組み合わせ）を持つ試行は2回ずつになります。

設定後、画面が下の図の通りになっているか確認しましょう。

<img width="954" height="327" alt="image" src="https://github.com/user-attachments/assets/ff61673a-d7a2-423d-b0fb-a636c5d7d285" />

</details>

---

## 4. 実験進行の設定（ステート遷移）と質問紙の紐づけ

`LUIDA > Configure experiment automation > State Machine`画面（左図）で、実験の進行フローを「ステート」と呼ばれる単位で設定すると、右図のように実験が自動的に進行します。
各ステートは実験の一区切り（例：説明、タスク実行、休憩、質問紙回答など）を表し、それらがどのように遷移するかをここで定義できます。

各設定項目の詳細は[ドキュメント](/Documentation_JA.md#実験進行の設定ステート遷移と質問紙の紐づけ)をご参照ください。

![state-transition-example](https://github.com/user-attachments/assets/69ce4439-a2da-4912-8344-feba222e1347)

<!-- ![Luida Editor Experiment Statesタブ](https://github.com/user-attachments/assets/fbbc7f52-af5b-43db-adf7-4d579fed23b3) -->

<details>
<summary><h3>こちらの手順に従って設定してください</h3></summary>

1. **最初のステートを質問紙と紐付ける**：`Start`の行で
    1. `Add Questionnaire`ボタンを押し、`qID`に`1`と入力します。
2. **説明のステートを30秒で自動的に飛ばす**：`Intro`の行の`Has Exit Time`をチェックし、`Exit Time`に`30`と入力します。
3. **計算タスクのステートを追加し、5回繰り返す**：
    1. `Add State Before Trials`ボタンを押し、ステートを追加します。`CalculationTask`と名前を変えてあげます。
    2. `CalculationTask`の行の`Is Repeated`をチェックし、`Repeat Destination`で`CalculationTask`を選択し、`Repeat Count`に`5`と入力します。
        - するとこのステートは次に進む前は5回繰り返されます。
        - もっと計算させたい場合は数字を増やしてください。
4. **試行のステート**：`Trial - Start`の行は特に設定なし。
5. **試行の休憩ステートを3秒で自動的に飛ばす**：`Trial - Rest`の行の`Has Exit Time`をチェックし、`Exit Time`に`3`と入力します。
6. **試行終了後のステートを質問紙と紐付ける**：`Outro`の行で
    1. `Add Questionnaire`ボタンを押し、`qID`に`2`と入力します。

設定後、画面が下の図の通りになっているか確認しましょう。

<img width="1110" height="1025" alt="image" src="https://github.com/user-attachments/assets/95355e8a-f512-45d5-9d66-5d98898187ef" />

</details>

---

## 5. 実験進行に合わせたオブジェクトの挙動の設定

`LUIDA > Configure experiment automation > State-listening Items`では、実験の進行（ステートの遷移）に合わせてオブジェクトの動作を制御できます。

動作の例：自身や子オブジェクトの表示/非表示、自身や子オブジェクトの位置・回転の設定、テキスト内容の変更、コントローラを振動させる、数秒間待機、カスタムな動作（ご自身でコーディング）など。

_*特定のステートに依存しないオブジェクトや動作は、この機能ではなく、通常のUnityおよびCluster Creator Kit（CCK）の方法でも作成・設定できます。_

各設定項目の詳細は[ドキュメント](/Documentation_JA.md#実験進行に合わせたオブジェクトの挙動の設定)をご参照ください。

![Luida Editor State-listening Itemsタブ](https://github.com/user-attachments/assets/1937e67a-8137-482e-95c5-6ce359d00259)

### 以下の手順に従って、それぞれのオブジェクトの作成と挙動の設定を行ってください

<details>

<summary><h3>指示用のオブジェクト</h3></summary>

0. `LUIDA > Configure experiment automation > State-listening Items`を開きます。
1. `New Item Name`に`Instruction`と名前を入れて`Add state-listening item`ボタンをクリックします。
   - すると下のテーブルに列が追加され、シーンの中にも`Instruction`という名前を持つオブジェクトが作成されます。
2. `Instruction`の列と、`Intro`、`Trial - Start`、`Outro`の行が交差するセルで、`Add Listener`ボタンをクリックし、以下のテーブルにある図の通りに設定します。

| Intro | Trial - Start | Trial - Rest |
| :---: | :---: | :---: |
| ![Instruction at Intro](https://github.com/user-attachments/assets/dadb05d3-727e-4c9d-a5a7-7fc31d259787) | ![Instruction at Trial - Start](https://github.com/user-attachments/assets/d06ccd02-2873-4bc1-b3ec-0db15056198a) | ![image](https://github.com/user-attachments/assets/b5c1d8d9-592c-4cae-bb64-80dcb625881d) |
| Introステートに入ったら表示され、テキスト内容が本実験の説明文に切り替わる。出るときには非表示になる。 | Trial - Startステートに入ったら表示され、テキスト内容が切り替わる (↓の「Trial - Start の時のInstructionのSet Text動作について」を見てください)。 | Trial - Restステートに入ったら、テキスト内容が切り替わる: `Take a break for 5 seconds`。出るときには非表示になる。 |

<details>

<summary>Trial - Start の時のInstructionのSet Textについて</summary>

- On State Start 1つ目のSet Text
    1. **if**ボタンをクリック
    2. Var Name：`request`
    3. Is Value: `font`
    4. Text：`Click the button that matches the text's font color.`
- On State Start 2つ目のSet Text
    1. **if**ボタンをクリック
    2. Var Name：`request`
    3. Is Value: `meaning`
    4. Text：`Click the button that matches the text's meaning.`

これにより、実験条件`request`に応じて、この指示オブジェクトのテキスト内容が異なる。

</details>

3. シーンの中にあるInstructionオブジェクトを特定し、下の図の通りに位置を調整します。
4. 子オブジェクトのTextを特定し、下の図の通りに`TextView`を設定します。

| Instruction | Text子オブジェクト |
| :---: | :---: |
| ![Instruction GameObject](https://github.com/user-attachments/assets/6a8c2664-7cd2-4c9d-a8ca-375900f214f4) | ![Instruction GameObject's Text child](https://github.com/user-attachments/assets/e7cc8c1c-21a9-4463-8dc7-a36d5d9f6c1e) |

</details>

<details>

<summary><h3>タスクの題目テキスト</h3></summary>

0. `LUIDA > Configure experiment automation > State-listening Items`を開きます。
1. **赤いフォントを持つテキスト**：`New Item Name`に`Text_RedFont`と名前を入れて`Add state-listening item`ボタンをクリックします。
2. **青いフォントを持つテキスト**：`New Item Name`に`Text_BlueFont`と名前を入れて`Add state-listening item`ボタンをクリックします。
3. `Text_RedFont`と`Text_BlueFont`の列と、`Trial - Start`の行が交差するセルで、`Add Listener`ボタンをクリックし、以下の図の通りに設定します。
    - _設定の説明_
        - _`Trial - Start`ステート中にしか表示されない_
        - _該当試行の実験条件`font`=`R`の場合は**赤いフォントのみ**を表示し、`font`=`B`の場合は**青いフォントのみ**を表示する_
        - _該当試行の実験条件`text`=`Red`の場合は両方ともテキスト内容を**Red**にし、`text`=`Blue`の場合は両方ともテキスト内容を**Blue**にする_
        - _該当試行の実験条件`depth`=`near`の場合は両方とも**z=1**にし、`depth`=`far`の場合は両方とも**z=3**にする_
<img width="499" alt="Screenshot 2025-05-28 at 15 51 20" src="https://github.com/user-attachments/assets/dcb72791-f7d0-4f05-a241-10fd421994a3" />

4. シーンの中にあるText_RedFontとText_BlueFontオブジェクトを特定し、下の図の通りに位置を調整します。
5. それぞれに`MovableItem`コンポーネントを追加し、`RigidBody`の`Use Gravity`を外し、`Is Kinematic`を入れる
6. それぞれの子オブジェクトのTextを特定し、下の図の通りに`TextView`を設定します。

| Text_RedFont / Text_BlueFont | Text子オブジェクト |
| :---: | :---: |
| ![Instruction GameObject](https://github.com/user-attachments/assets/144e8368-318b-4ed1-8450-6d09502640b7) | ![Instruction GameObject's Text child](https://github.com/user-attachments/assets/293b280e-bffb-4a62-b779-f470b70d21c6) |

</details>

<details>

<summary><h3>タスクの回答用ボタン</h3></summary>

0. `LUIDA > Configure experiment automation > State-listening Items`を開きます。
1. **赤と回答するボタン**：`New Item Name`に`Answer_Red`と名前を入れて`Add state-listening item`ボタンをクリックします。
2. **青と回答するボタン**：`New Item Name`に`Answer_Blue`と名前を入れて`Add state-listening item`ボタンをクリックします。
3. `Answer_Red`と`Answer_Blue`の列と、`Trial - Start`の行が交差するセルで、`Add Listener`ボタンをクリックし、以下の図の通りに設定します。
    - 設定の説明：`Trial - Start`の間にのみ表示されるようになります。

<img width="506" alt="Screenshot 2025-05-28 at 16 11 54" src="https://github.com/user-attachments/assets/7a9dc11d-6518-4026-87ad-6e71b8f71628" />

4. **ボタンの見た目**：シーンの中にあるAnswer_RedとAnswer_Blueオブジェクトを特定し、子オブジェクトとしてCubeを作成し、位置とマテリアルを調整します。

| Answer_RedのText子オブジェクト | Answer_BlueのText子オブジェクト |
| :---: | :---: |
| ![Instruction GameObject's Text child](https://github.com/user-attachments/assets/1838857f-5add-44a3-9b32-ad2ca4b7bff4) | ![Instruction GameObject](https://github.com/user-attachments/assets/db444f9a-e571-40e3-90f7-ee784c495cd5) |
| isRed：チェックを入れる | isRed：チェックを外す |

5. **クリックしたら回答を送ってステート遷移させるように設定**：Answer_RedとAnswer_Blueオブジェクト自体を特定し、それぞれにコンポーネント`Interact Item Trigger`と`Luida To Next State Gimmick`を追加し、以下の図の通りに設定します。
    - Trigger（トリガー）とGimmick（ギミック）の関係はCCKのドキュメントをご参照ください：https://docs.cluster.mu/creatorkit/world/trigger-gimmick/
        - 予想される動作（例えばAnswer_Red場合）：
            1. クリックされたら`Interact Item Trigger`が動作し、`LUIDA-DataCollector`（データ記録用のオブジェクト、詳細は後述）宛に`isRed=true`のメッセージ、次にThis（自分自身）宛に`toNextState`というメッセージが送信されます。
                -  ゲームオブジェクト `LUIDA-DataCollector` が存在しない場合、Hierarchyで右クリックして `LUIDA > Data Collector` を選ぶことで `LUIDA-DataCollector` を作成してください：<img width="471" height="752" alt="image" src="https://github.com/user-attachments/assets/66a310c7-6cc6-49df-90fa-875c4820b22d" />
            2. This（自分自身）宛に`toNextState`というメッセージが届いたら`Luida To Next State Gimmick`が動作し、ステート遷移を発火させます。
    - _`Luida To Next State Gimmick`はLuida専用のCCKギミックです。他にも`Luida Process Data And Save To Collection Gimmick`と`Luida Update Collected Data Gimmick`があります。_

| Answer_Red | Answer_Blue |
| :---: | :---: |
| ![Instruction GameObject](https://github.com/user-attachments/assets/631c765c-8daf-4e7d-8ceb-d5ba3543bdd6) | ![Instruction GameObject's Text child](https://github.com/user-attachments/assets/49386e8f-539a-465a-b2cd-a4b53f29870a) |

</details>

<details>

<summary><h3>タイマー（タスクにかかった時間を数える）オブジェクト</h3></summary>

0. `LUIDA > Configure experiment automation > State-listening Items`を開きます。
1. `New Item Name`に`TimeRecorder`と名前を入れて`Add state-listening item`ボタンをクリックします。
2. `TimeRecorder`の列と、`Functions, events, variables not listening to the state machine`、`Trial - Start`、`Outro`の行が交差するセルで、`Add Listener`ボタンをクリックし、以下のテーブルにある図の通りに設定します。

| Functions, events, variables not listening to the state machine | Trial - Start | Outro |
| :---: | :---: | :---: |
| ![Instruction GameObject](https://github.com/user-attachments/assets/47dbd2d3-e382-45f2-bc0a-9ceb410e0db6) | ![TimerRecorder-TrialStart](https://github.com/user-attachments/assets/837fab63-e31c-47d3-a62a-a24a8bdcd5bf) | ![TimeRecorder-Outro](https://github.com/user-attachments/assets/ea250246-4c3a-4aca-b94b-7feecde4c41e) |
| スクリプトとその説明は、下の`▶ Functions, events, variables not listening to the state machine`にあります | スクリプトとその説明は↓の`▶ Trial - Start`にあります | 全試行が終了した後、記録してきたデータをアップロードさせます。 |

<details>

<summary>Functions, events, variables not listening to the state machine</summary>

タイマーを初期化し、試行中であればタイマーをかける：
```javascript
function Start() { $.state.timer = 0; }
function Update(deltaTime) {
  if ($.state.isInTrial) $.state.timer += deltaTime;
}
```

</details>

<details>

<summary>Trial - Start</summary>

On State StartのCustomized Action:
タイマーを初期化し、試行中フラグを立てる
```javascript
$.state.isInTrial = true;
$.state.timer = 0;
```

On State EndのCustomized Action:
試行中フラグを下し、タイマーの値を送信（setStateCompatについてはCCKのドキュメントを参照）
```javascript
$.state.isInTrial = false;
SendDataToCollector("timer", $.state.timer); // 回答に使った時間をLUIDAのデータ記録機能を持つData Collector（詳細は後述する）に送る
```

また、`Process and save collected data` アクションで、Luidaのデータ記録機能を発火させます。データの保存形式は後述します。

</details>

</details>

<details>

<summary><h3>認知負荷をかけるための計算タスク用オブジェクト</h3></summary>

1. `LUIDA > Configure experiment automation > State-listening Items`を開きます。
2. `New Item Name`に`CalculationTextInput`と名前を入れて`Add state-listening item`ボタンをクリックします。
3. `CalculationTextInput`の列の`Functions, events, variables not listening to the state machine`のコードブロックに、以下のスクリプトをコピペします：
```javascript
function getRandomInt(max) { // 乱数の整数を生成する関数を定義する
  return Math.floor(Math.random() * max);
}
$.onTextInput((text, meta, status) => {
  if (status === TextInputStatus.Success) {
    ToNextState(); // 参加者からのテキスト入力を受け付けたら次のステートへ遷移させる
    // メモ：ただ計算させるだけなので、正解かどうかを確認しない。確認したい場合はご自身でスクリプトを編集してください。
  }
});
```
4. `CalculationTextInput`の列と、`CalculationTask`の行が交差するセルで、`Add Listener`ボタンをクリックし、Customized Action動作を一つ追加し、コードブロックに以下のスクリプトをコピペします
```javascript
// PARTICIPANTS[1]で一人目の参加者（1人しかいないけど）を特定する
PARTICIPANTS[1].requestTextInput(
  "ask_to_calculate",
  getRandomInt(100) + "+" + getRandomInt(100) + "=?"
);
```

| Functions, events, variables not listening to the state machine | Trial - Start |
| :---: | :---: |
| ![calculation-task-code](https://github.com/user-attachments/assets/891b2e76-1fc2-4e44-9c7d-54e3989549ec) | ![calculation-task-code-during-task](https://github.com/user-attachments/assets/dc5343c8-d54a-4f6e-93bf-5bfb7c342bc6) |

</details>

---

## 6. カスタム形式で記録したいデータの定義

LUIDAでは、カスタム形式で記録したいデータ（例：回答ボタンをクリックする際に送られるisRedの値、試行が終わるたびに送られるタイマーの値、そして試行ごとの実験条件）の定義や計算ができます。

各設定項目の詳細は[ドキュメント](/Documentation_JA.md#カスタム形式で記録したいデータの定義)をご参照ください。

<img width="787" height="219" alt="image" src="https://github.com/user-attachments/assets/893571d5-30c0-441a-aff7-c237ce926ce1" />


以下の記述に従って設定してください。

1. シーンの中からゲームオブジェクト `LUIDA-DataCollector` を特定し、そのInspectorから `Script Asset` をダブルクリックします（下の図を参照）。すると該当するスクリプトの編集画面が開きます。
        <img width="636" height="347" alt="data-collector" src="https://github.com/user-attachments/assets/b36ae892-08b8-4ce7-aa89-c0f405faad8b" />

2. 以下のスクリプトをコピペしてファイル保存します：
```JavaScript
return {
  d: CONDITION['depth'], // 該当試行のdepth条件（参加者間変数depthの該当試行における値）
  req: CONDITION['request'], // 該当試行のrequest条件（参加者内変数requestの該当試行における値）
  font: CONDITION['font'], // 該当試行のfont条件（参加者内変数fontの該当試行における値）
  text: CONDITION['text'], // 該当試行のtext条件（参加者内変数textの該当試行における値）
  ans: $.getStateCompat('this', 'isRed', 'boolean') ? "R" : "B",　// 回答（赤か青）
  time: COLLECTED_DATA['timer']　// 答えるのに使った時間
};
```
  - CCKのコンポーネントでLUIDA-DataCollector宛てに送られてきたisRedの値は、`$.getStateCompat('this', 'isRed', 'boolean')`で取得する
  - 関数`SendDataToCollector("timer", $.state.timer);`で送られてきたタイマーの値は、`COLLECTED_DATA['timer']`で取得する
  - 現在の試行の実験条件は`CONDITION[変数名]`で取得する

---

## 7. アップロード前の準備

<details>
<summary>以下の手順に従って設定を行ってください</summary>

1.  Unityのメニューバーから `Cluster > 設定` を開き、「ベータ機能を利用する」にチェックを入れます。
    ![ベータ機能の有効化](https://github.com/user-attachments/assets/af786e5e-07fe-4126-b350-1ed7c0401ecd)
2.  **ローカルテストプレイ**: Unityエディタの再生ボタンを押して、実験全体の流れや各ステートの動作、オブジェクトの挙動などが意図した通りかを確認します。
3.  シーンをセーブします。

</details>

---

## 8. cluster にアップロード

<details>
<summary>以下の手順に従って設定を行ってください</summary>

1.  clusterの公式ドキュメント [ワールドをアップロードする手順](https://creator.cluster.mu/2020/03/28/%E5%88%B6%E4%BD%9C%E3%81%97%E3%81%9F%E3%83%AF%E3%83%BC%E3%83%AB%E3%83%89%E3%82%92%E3%80%8Ccluster%E3%80%8D%E3%81%AB%E3%82%A2%E3%83%83%E3%83%97%E3%83%AD%E3%83%BC%E3%83%89%E3%81%99%E3%82%8B/) に従って、作成したシーンをclusterのワールドとしてアップロードします。
    *   アップロード後、実際にそのワールドに入室し、実験参加者として一通り体験して動作を確認してください。
    *   この時点では、通常のアバター選択が可能ですが、後述のアバター設定をLUIDAウェブコンソールで行い、実験が正式に公開されると、参加者は指定されたアバター（またはアバター非表示）で実験に参加することになります。
2.  アップロード後に不具合が見つかった場合は、Unityプロジェクトで修正後、「テスト用アップロード」機能を利用して [テスト用スペース](https://creator.cluster.mu/2024/05/24/testspace/) で動作確認を行うことを推奨します。これにより、公開中のワールドに影響を与えることなく修正内容をテストできます。
    *   修正が完了し、テスト用スペースで問題ないことが確認できたら、再度通常のワールドアップロードを行ってください。
3.  clusterワールドでの一通りの動作確認と合わせて、LUIDAウェブコンソール上で、実験データ（質問紙への回答、その他収集設定したログデータなど）が正しく記録・表示されているかを確認してください。
    ![ウェブコンソールでのデータ確認例](https://github.com/user-attachments/assets/9db65b18-7a6e-412d-8908-54a2995bfdb9)

</details>

---

## 9. ワールド ID の登録

<details>
<summary>以下の手順に従って設定を行ってください</summary>

1.  ウェブブラウザでclusterの公式サイトにログインし、「マイコンテンツ」ページ（または「ワールド」管理画面）で、アップロードした実験ワールドが一覧に表示されていることを確認します。
2.  該当するワールドを選択し、ワールド詳細ページを開きます。そのページのURLの末尾にある英数字の文字列がワールドIDです（例: `https://cluster.mu/w/XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX` の `XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX` の部分）。このワールドIDをコピーします。
    ![clusterマイコンテンツ画面でのワールド確認](https://github.com/user-attachments/assets/44821568-fa20-4f75-9cf1-c49f38b9d4e5)
3.  LUIDAウェブコンソールの該当する実験情報編集画面を開き、コピーしたワールドIDを所定のフィールド（例: 「ワールドID」）に登録・保存します。
    ![ウェブコンソールでのワールドID登録](https://github.com/user-attachments/assets/c5003a53-3ea0-4b72-aa92-ea37e1e2a1d9)

</details>

---

## 10. アバターの設定

<details>
<summary>以下の手順に従って設定を行ってください</summary>

このチュートリアルで作る実験では、実験参加者のアバターを設定する必要はないが、
もしアバターを非表示にしたい場合や、実験専用の特定アバターを使用させたい場合は、以下の手順で設定します。

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

</details>

---

## 11. LUIDA での自動掲載を待つ

上記までのすべての設定が完了すると、あなたが作成した実験はLUIDAの参加者募集ワールドに掲載される準備が整います。掲載まで数日程度お待ちいただく場合があります。

\*注：将来的には、新たに登録された実験がより迅速に（例：1日以内など）公開されるよう、LUIDAの実験情報更新プロセスを改善していく予定です。

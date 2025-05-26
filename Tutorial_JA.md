# チュートリアル

このチュートリアルでは、本実装テンプレートを用いてLUIDA上で実験系を実装するやり方を紹介します。

以下について基礎だけでも勉強しておくことを推奨します：

- Unity
- JavaScript
- [Cluster Creator Kit (CCK)](https://docs.cluster.mu/creatorkit/)

また、チュートリアルを始める前に、以下を用意しておいてください：

1. [cluster アカウント作成](https://help.cluster.mu/hc/articles/115000827112)
2. [cluster に必要なバージョンの Unity のインストール](https://docs.cluster.mu/creatorkit/installation/install-unity/) を行います。
3. [LUIDAのウェブコンソール](https://luida-web-next.vercel.app/)で初回はログインし、承認リクエストを送ったら担当者（y.hu@cluster.mu）に連絡してください。

---

## ウェブコンソールで実験情報を登録

以下の手順2-3は [こちらの動画](https://drive.google.com/file/d/1D3TxoWqSrvJkEMVik8WquZu4GjI-tHbM/view?usp=sharing) の「1. Researchers register experiment information...」でも参照できます。

1. [LUIDAのウェブコンソール](https://luida-web-next.vercel.app/)を開きます。
2. 「＋新規実験」をクリックし、実験の基本情報（「タイトル」「参加条件」「画像URL」は必須。画像URLは一旦任意の文字列でも）を登録します
![image](https://github.com/user-attachments/assets/d1814ffb-4552-47cf-a60e-2575acbfc570)
3. 登録した実験の詳細ページで追加編集を行ったり、アンケートを登録したりします
<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/53ec6252-f623-415c-ba50-4d6a27d7a86b" alt="Image 1" width="600"/></td>
    <td><img src="https://github.com/user-attachments/assets/f32d0b65-9551-482d-a7ad-09ecfba822df" alt="Image 2" width="600"/></td>
  </tr>
</table>
4. 実験IDをコピーしておきます。

![image](https://github.com/user-attachments/assets/20780d32-15ce-4588-a377-415b6d0fef40)

---

## 実装テンプレート(Unity)のダウンロードと初期設定

1. [最新リリース](https://github.com/cluster-lab/project-luida-bar/releases)からダウンロードします。
2. ダウンロードしたUnityプロジェクトを立ち上げ、**最初はエラーを無視し、立ち上げたら以下のパッケージをインポートします**。
    - [**CSCombiner: Cluster Scriptを Unity Editor 上で結合するツール**](https://vkao.booth.pm/items/5924956) (ver1.01)
    - [**CSEmulator: Cluster Scriptを Unity Editor 上で再生できるようにするツール**](https://vkao.booth.pm/items/5111235) (最新バージョン)
3. Window > Luida Editor を開き、以下の画面が表示された場合、まずはご自身が実装する実験のシーン名を入力します。
![image](https://github.com/user-attachments/assets/be969afc-0dc8-43a3-995b-ae8f420a5e5b)
4. 以下の図に従い、アクセストークンを発行し、Unityプロジェクトに登録します（下の画像に示された通りに行ってください）。
![cluster-access-token-registration-jp](https://github.com/user-attachments/assets/c06f43c6-3412-4462-92a9-ac3576252e99)
5. 以下の図に従い、clusterの外部通信機能用のURLを登録し、生成されたトークンを本実装テンプレートに登録します（下の画像に示された通りに行ってください）。
![register-call-external-url](https://github.com/user-attachments/assets/f64e75df-93f2-4b1a-9b3a-36216405feb7)
6. Window > Luida Editor > Experiment Identifiers が開いたまま、 `Experiment ID` に先ほどコピーした実験IDを入力します。最後に「Save」ボタンをお忘れなく。
![image](https://github.com/user-attachments/assets/9d216be0-5c45-41f2-9129-f46546f940ae)

---

## 実験変数と試行回数を設定する

1. `Window > Luida Editor`を開き、`Experiment Variables`タブに切り替え、「Create New Variables Asset」をクリックします。
2. Variables for Within-Subject Conditions > Length に、参加者内変数の数を入力します。
   1. 数が1以上の場合、変数の名前と値をフィールドに入力します
      1. 例えば以下の画像のように：1. Name: `gain` 2. Values (カンマ区切り): `0.75,0.8,0.85,0.9,0.95,1,1.05,1.1,1.15,1.2,1.25`
   ![image](https://github.com/user-attachments/assets/15fde214-9fc0-4e27-9d3c-aaab31a46863)
4. Variables for Between-Subject Conditions > Length に、参加者間変数の数を入力します。
   1. 以上と同様
5. Trials Count per Conditionに、同じ条件を持つ試行が行われる回数を入力します（特になければ1で）。
6. **（変数の数が0であっても）** 最後に「Apply Updated Variables」ボタンをクリックして変更を保存します。
7. シーンを保存します。

---

## 実験進行の設定（ステート遷移）と質問紙の紐づけ

実験の進行がいくつかのステージに分けられ、この設定画面がそのステージを設定するためのものだと思ってもらえればと思います。

`Window > Luida Editor`を開き、`Experiment Variables`タブに切り替えると、既にいくつかのステートがデフォルトで設定されていることが分かります。

特に変える必要がなければそのまま放置し、変える場合は以下の説明を参照してください。

![image](https://github.com/user-attachments/assets/31adc227-c7de-448e-91cd-ac2f13eae12d)

- Transit destination state：このステートが終了したら次になるステート（デフォルト：一つ下のステート）
- Move state to：ステートの順番の調整
- Has Exit Time：このステートが何秒（Exit time）経つと自動的に終了するか
- Is Repeated：このステートが終了したらTransit destination stateではなく、ある前のステート（Repeat Destination）に何回（Repeat Count）戻るか
- Questionnaire：このステートで表示されるアンケート。
   - qID：ウェブコンソール上に登録した質問紙の順番。例えば↓の「実験前アンケート」をこのステートに表示させたいの場合、qIDを1に設定します。![image](https://github.com/user-attachments/assets/39d122b1-d725-4dfc-8f6e-d9e2d279d622)


設定が終わったらシーンのセーブを忘れずに。

---

## 実験進行に合わせたオブジェクトの挙動の設定

特に実験進行に合わせる挙動がなければ、普通のUnity＋CCKのやりかたでオブジェクトを作成・設定できますが、
実験進行に合わせる挙動が必要な場合（例：このタイミングでオブジェクトを表示する、説明が終わったらタスクを始める、etc.）
`Window > Luida Editor`を開き、`State-listening Items`タブに切り替え、以下の図と各項目の説明を参照しながら設定を行ってください。

- New Item Nameを埋めてボタンをクリックすると新しいオブジェクトが作成されます。
- このオブジェクト（列）に動作させたいステート（行）の箇所に、Add Listenerボタンをクリックします。
- このステートに合わせてさせたい挙動を設定します。
   - On State Start：ステート開始時に一回行う動作
   - During State：ステート中に毎フレーム行う動作
   - On State Exit：ステート終了時に一回行う動作

![image](https://github.com/user-attachments/assets/47eb65f1-596b-4c6d-b72d-eecd3afe0bd3)

---

## アップロード前の準備

1. ベータ機能を有効にします。
   ![image](https://github.com/user-attachments/assets/af786e5e-07fe-4126-b350-1ed7c0401ecd)
2. `Window > かおもラボ > CSCombiner`開いて`全更新`ボタンを押します。
   ![image](https://github.com/user-attachments/assets/12cd1c5e-0dcc-4d91-b340-900ed0a35041)
3. シーンをセーブします。
4. ローカルでテストプレイ：Unity エディターのプレイボタンを押して、実験が予想通りに回るか確認します

---

## cluster にアップロード

1. [こちらの手順](https://creator.cluster.mu/2020/03/28/%E5%88%B6%E4%BD%9C%E3%81%97%E3%81%9F%E3%83%AF%E3%83%BC%E3%83%AB%E3%83%89%E3%82%92%E3%80%8Ccluster%E3%80%8D%E3%81%AB%E3%82%A2%E3%83%83%E3%83%97%E3%83%AD%E3%83%BC%E3%83%89%E3%81%99%E3%82%8B/)に従って、cluster のワールドとしてアップロードしてます。アップロードできたら自ら入室し、実験の流れを一回一通り体験し、動作を確認します。
   - この時点では、アバターがまだ自由に選択できますが、アバターの設定を終え（後述）、正式に実験が公開されたら、ユーザがこの実験ワールドに入室する際に透明アバターしか選べなくなります。
2. もしアップロード後に何かバグが見つかり、修正して動作を再確認したい場合、「テスト用アップロード」を行い、「[テスト用スペース](https://creator.cluster.mu/2024/05/24/testspace/)」で確認することができます。

- バグの修正ができたら、再びのテストではない方のアップロードを忘れずに

3. 最後まで一通り体験できたら、ウェブコンソールから質問紙への回答やアップロードしたデータなどが表示されているか確認します。
   ![image](https://github.com/user-attachments/assets/9db65b18-7a6e-412d-8908-54a2995bfdb9)

---

## ワールド ID の登録

1. ブラウザーから cluster ウェブサイトの「マイコンテンツ」にアクセスし、自作のワールド一覧画面に実験ワールドが出てきたか確認します。
2. 該当ワールドを選択し、そのワールドのページの URL の後ろにある英数字の文字列はワールド ID です。そのワールド ID をウェブコンソールの実験募集情報編集画面で登録します。
   ![image](https://github.com/user-attachments/assets/44821568-fa20-4f75-9cf1-c49f38b9d4e5)
   ![image](https://github.com/user-attachments/assets/c5003a53-3ea0-4b72-aa92-ea37e1e2a1d9)

---

## アバターの設定

アバターを隠したい場合や、指定したアバターのみで実験に参加させたい場合は、こちらの手順に従って設定してください。

1. LUIDA のウェブコンソールで、`Avatar Settings` の箇所に `Add World-Avatar Set` ボタンを押します。
2. `World ID` にワールドIDを登録します。
3. 以下のどれかを行ったら「Submit」ボタンをクリックします：
- アバターを隠したい場合：Hide Avatarのみをチェックします。
- アバターを指定したい場合：Assign Avatarのみをチェックし、 `Avatar Name` を埋め、`VRM Version` を指定し、 `Upload VRM` にアバターのVRMモデルをアップロードし、 `Upload Thumbnail (PNG)` にアバターのスクショをアップロードします。

![image](https://github.com/user-attachments/assets/4af44d34-eb78-4080-9241-09459874c1a6)

---

## LUIDA での自動掲載を待つ

数日後\*にあなたが本実装テンプレートで作成したこの実験は、LUIDA の参加者募集ワールドに掲載されます。しばらくお待ちください。

\*新たに登録された実験が1日で公開できるように、LUIDAに毎日更新の仕組みを実装する予定です。

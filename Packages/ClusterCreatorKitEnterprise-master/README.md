# ClusterCreatorKitEnterprise
社内向けの機能を含む Creator Kit 用の Addon
Creator Kit Enterprise Addonとは社内向けの機能を含む Creator Kit 用の Addonです

- [Enterprise Addon のセットアップ](#AddonSetup)
    - [ダウンロード](#Download)
    - [セットアップ](#Setup)
- [機能の使い方](#HowToUse)
    - [エリアカウント機能](#LoggingArea)
    - [外部URL機能](#ExternalUrl)
    - [グッズ販売機能](#Goods)
    - [アバター販売機能](#AvatarProduct)
    - [アバター販売機能用アバターディスプレイ](#ProductAvatarDisplay)
    - [ウォーターマーク機能](#Watermark)
    - [行き先可変ワールドゲート](#RoutingWorldGate)
    - [同時に複数持てないアイテム](#SingleGrabRestrictor)
    - [アイテムを持つ手の位置を調整する](#GrabPointOffset)
    - [アバターメイカーの起動トリガー](#AvatarMakerTrigger)
    - [ShaderにinstanceIDを設定する機能](#MPBInstanceIDSetter)
    - [UGC商品が購入されたときに発火するトリガー](#OnProductPurchasedPlayerTrigger)
    - [Live Streaming機能](#MediaPlayer)
    - [プラットフォーム制限GameObject](#RemoveIf)
    - [アバターチケット引換](#ExchangeTicketForAvatar)
    - [ロビーピックアップ商品ディスプレイ](#LobbyPickUpProductDisplay)
      - [クラフトアイテム/アクセサリーディスプレイ](#LobbyItemDisplay)
      - [アバターディスプレイ](#LobbyAvatarDisplay)
    - [ScriptからGlobalStateに書き込めるアイテム](#UnsafeStateUsableItem)
    - [操作ガイド/チュートリアルワールド移動エリア](#ControlTutorialArea)
    - [inroomイベントピックアップ表示](#InroomEventPickup)
    - [オンボーディング用Anchor](#OnboardingAnchor)
    - [降りられない乗り物](#ProhibitedGetOffItem)
    - [VRのみ/NonVRのみでActiveになるオブジェクト](#VROrNonVROnlyObject)
    - [チュートリアルサーバー強制ワールドゲート](#TutorialServerWorldGate)
    - [Analytics送信ギミック](#SendAnalyticsPlayerGimmick)
- [ClusterCreatorKitEnterpriseのリリース手順](#ReleaseProcedure)
    - [追加・変更をClusterONEにマージ](#AddForClusterONE)
    - [ClusterCreatorKitEnterpriseにPush](#ReleaseCCKE)
    - [EnterpriseのCGTeamに共有](#ShareToEnterpriseCGTeam)
    
<a id="AddonSetup"></a>
## Enterprise Addon のセットアップ
<a id="Download"></a>
### ダウンロード
- https://github.com/ClusterVR/ClusterCreatorKitEnterprise
    - ↑のReleaseからダウンロード
<a id="Setup"></a>
### セットアップ
通常のCreator Kit(cck 1.6.3以降に対応)に**追加**で読み込んで利用します
1. ダウンロードしたcreator-kit-enterpriseのzipを展開します
1. 会場のProject内のPackagesディレクトリに1で展開したものを配置します  
![image](https://user-images.githubusercontent.com/42376597/177166335-5add9469-2b99-4ce9-b9a6-143c37086826.png)
    - バージョンアップの場合は元あったものを削除して置き直すのが安全です  

もしくはmanifest.jsonに
`mu.cluster.cluster-creator-kit-enterprise": "ssh://git@github.com/ClusterVR/ClusterCreatorKitEnterprise.git,`
を追加
- バージョンアップの場合は(書いてあれば)そのバージョンを書き換えればOK
    - `ssh://git@github.com/ClusterVR/ClusterCreatorKitEnterprise.git#v1.2.0`の`#v1.2.0`の部分を書き換える
        - https://github.com/ClusterVR/ClusterCreatorKitEnterprise/releases で指定したいバージョンを探してそのバージョンで書き換えてください（最新なら一番上）  

<a id="HowToUse"></a>
## 機能の使い方
<a id="LoggingArea"></a>
### エリアカウント機能
- コライダーの付いたGameObjectに`LoggingArea`コンポーネントをアタッチする
    - AreaIdを指定する
        - 任意の分かりやすい文字列。同じAreaIdのコライダーはまとめて領域判定を行われる
    - ユーザーがルーム内でそのエリアに入ったときとエリアから出たときにログが送信される
        - 自動更新はされないので、右下の青三角を押して更新して下さい
            - dev: https://redash.fictbox.com/queries/1901
            - stg: https://redash.fictbox.com/queries/1935
            - prd: https://redash.fictbox.com/queries/2107
    - こっちも確認に便利
        - https://scrapbox.io/clustervr/エリアカウント確認用redash

<a id="ExternalUrl"></a>
### 外部URL機能
- コライダーの付いたGameObjectに`ExternalUrl`コンポーネントをアタッチする
    - URL・タイトルを指定する
    - ユーザーがルーム内でクリックするとこういうダイアログが出る
    ![image](https://user-images.githubusercontent.com/42376597/177323201-82b15e2a-6b9d-4ded-8e66-0e232f981f6d.png)
- 制作向けの注意点
    - 表示出来る文字数はウィンドウサイズによって変わる
        - 全角19文字までは表示出来そう？
            - https://clustervr.slack.com/archives/CT0N6M1BQ/p1599031851069400?thread_ts=1599031043.068200&cid=CT0N6M1BQ
    - タイトルが未入力だとダイアログにURLが表示されない
    - 前後に空白が入っているとiosで飛べない

<a id="Goods"></a>
### グッズ販売機能
- コライダーの付いたGameObjectに`GoodsDisplay`コンポーネントをアタッチする
    - Idの欄に商品のId, StoreDomainUrlの欄にストアのUrlを入力する
      - 商品IDの取得方法は下記資料にて
        - https://docs.google.com/presentation/d/1ctUK_2bxYdlstXQOarlrwav7KnExmmatRyGOhwYVwqQ/edit#slide=id.g25c49103752_0_156
      - Store Domain Urlも商品IDと同じく先方から共有いただく 
      - 入力形式は下記例のような形に整形する
        - Id: `gui://shopify/Product/1234567890`
        - StoreDomainUrl: `clustershop.my.shopify.com
        <img src="https://github.com/ClusterVR/ClusterONE/assets/53528506/cfc2c071-b3b7-4a0e-9147-07617ca61efd" width="400">
    - ユーザーがルーム内でGoodsDisplayコンポーネントをアタッチしたGameObjectをクリックすると下記UIが出る  
    <img src="https://github.com/ClusterVR/ClusterONE/assets/53528506/b1f97946-1886-4901-8ef5-e956ca8efaa6" width="400">
- 制作向けの注意点
    - アウトラインなどは付かないのでデザインで押せるよアピールする必要がある
      - インタラクト範囲はGoodsDisplayコンポーネント以下のInteractableExhibitレイヤーのコライダーになる
    - UnityEditor上でのログ表示などは特にない

<a id="AvatarProduct"></a>
### アバター販売機能
- コライダーの付いたGameObjectに`AvatarProduct`コンポーネントをアタッチする
    - Idの欄に事前にエンプラ担当者がサーバー班に頼んで作ってもらったIdを指定する  
    ![image](https://user-images.githubusercontent.com/42376597/177326400-5fe71138-1c49-4216-8a7d-7c0ef707b301.png)
    - ユーザーがルーム内でAvatarProductコンポーネントをアタッチしたGameObjectをクリックするとこういうUIが出る  
    ![image](https://user-images.githubusercontent.com/42376597/177326512-a4fcc915-45b0-49c6-a11e-991010da7507.png)
- 制作向けの注意点
    - アウトラインなどは付かないのでデザインで押せるよアピールする必要があります
    - UnityEditor上ではログしかでません  
    ![image](https://user-images.githubusercontent.com/42376597/177326639-fdad35a1-ae50-49a5-ba97-672b14f07180.png)

<a id="ProductAvatarDisplay"></a>
### アバター販売機能用アバターディスプレイ
- GameObjectに`ProductAvatarDisplay`コンポーネントをアタッチする
    - Product Avatar Idに[アバター販売機能](#AvatarProduct)で使用している商品のIdを指定する
    - Avatar Display Rootに3Dモデルを表示したい場所を指定するTransformを指定する
      - nullの場合は自分自身のTransformが使用される
      - アバターのLayerは、このTransformのLayerに自動で変更される
    - Avatar Display Boundsに`AvatarDisplayBounds`を指定する（後述）
    - Avatar Display Poseにアバターの姿勢を指定するAnimationClipを指定する
    - Avatar Display Facial Expression Typeにアバターの表情を指定する
    - Temporary Objectに`AvatarDisplayTemporaryObject`を指定する（後述）
- 新規にGameObjectを作成して`AvatarDisplayBounds`コンポーネントをアタッチする
    - BoxColliderが自動でアタッチされる
    - カメラがBoxColliderの中にあるときにアバターが表示されるので、アバターが見えてほしい場所を覆うようにBoxColliderを調整する
- アバターが非表示またはロード中のときに表示しておいてほしいGameObjectを用意し、それに`AvatarDisplayTemporaryObject`コンポーネントをアタッチする
  - アバターが表示されている間は非アクティブになる
- 制作向けの注意点
    - `AvatarDisplayBounds`で指定したBoxColliderが沢山重なった場所に入るとアバターが大量にロードされる可能性があります。BoxColliderを大きくする場合は数に気を付けてください。
    - トリガーなどで展示アバターを切り替えたい場合は`AvatarDisplayBounds`のついたGameObjectのアクティブ・非アクティブを切り替えることで実現できます。

<a id="LobbyPickUpProductDisplay"></a>
### ロビーピックアップ商品ディスプレイ

<a id="LobbyItemDisplay"></a>
#### クラフトアイテム/アクセサリーディスプレイ
* `AutoAssignableProductDisplayItem`コンポーネントを利用する
  * 使い方は`creator-kit-enterprise/PackageResources/Prefabs/AutoAssignableProductDisplayItem.prefab`を参考にしてください
* GameObjectの組み方は[通常の商品ディスプレイ](https://creator.cluster.mu/2023/03/06/productdisplayitem/#1%E3%81%8B%E3%82%89%E8%A8%AD%E5%AE%9A%E3%81%97%E3%81%A6%E5%95%86%E5%93%81%E3%83%87%E3%82%A3%E3%82%B9%E3%83%97%E3%83%AC%E3%82%A4%E3%82%A2%E3%82%A4%E3%83%86%E3%83%A0%E3%82%92%E3%81%A4%E3%81%8F%E3%82%8B)と変わりなし
* `AutoAssignableProductDisplayItem`固有の設定は2点
  * `Order`でピックアップ商品の何番目を表示するかを設定する
  * `Display Content`でアクセサリーかクラフトアイテムかを設定する

<a id="LobbyAvatarDisplay"></a>
#### アバターディスプレイ
[アバター販売機能](#AvatarProduct)と異なりアバター表示部分引換え機能部分が1コンポーネントにまとまっている。

* コライダーの付いたGameObjectに`AutoAssignedTicketExchangeableAvatarDisplay`コンポーネントをアタッチする
* 設定の大部分は[アバター販売機能用アバターディスプレイ](#ProductAvatarDisplay)と同じで、固有の設定は2点
  * `Display Avatar Index`にピックアップ商品の何番目を表示するかを設定する
  * `Avatar Display Pose List`にアバターの姿勢を指定するAnimationClipの一覧を指定する
    * ピックアップアバターの設定側で「何番目のポーズを取らせるか」を数値で指定するので、AnimationClipの順番に注意する
    * 使い方は`creator-kit-enterprise/PackageResources/Prefabs/AutoAssignableProductDisplayItem.prefab`を参考にしてください

<a id="Watermark"></a>
### ウォーターマーク機能
- 適当なGameObjectに`Watermark`コンポーネントをアタッチする
    - 表示するテクスチャを設定する  
    ![image](https://user-images.githubusercontent.com/42376597/177328657-1e90f27e-457d-4857-bf7a-1657559af0c3.png)
    - ユーザーがルーム内で写真を撮るとこういう感じで右下に表示されます  
    ![image](https://user-images.githubusercontent.com/42376597/177328678-853c4606-218d-4bbc-9114-ebeca0fd00f9.png)
- 制作向けの注意点
    - テクスチャのCompression設定がNoneではない場合モバイル端末で圧縮され正方形テクスチャになるため写るウォーターマークのかたちが正方形になってしまう  
    ![image](https://user-images.githubusercontent.com/42376597/177328749-5f2588fa-3643-4a23-b2d4-965d9fa73692.png)
    - のでテクスチャを正方形にするか、Compression設定をNoneにしましょう。
    - 幅1920のときに表示されるサイズ比率で各端末の写真に表示されるので参考にしてください

<a id="RoutingWorldGate"></a>
### 行き先可変ワールドゲート
- コライダーコンポーネントがついているGameObjectに`RoutingWorldGate`コンポーネントをアタッチする
    - ![image](https://github.com/ClusterVR/ClusterONE/assets/37761378/4c1096dc-19db-4917-827d-ce9b45fd005b)
        - RoutingKeyにはJenkinsで設定した、ワールドIdに対応した`routing_key`を入れてください
        - KeyにはWorldGateと同様にスポーンポイントに対応したKeyを入れてください（スポーンポイントの指定がない場合は空欄で大丈夫です）
        - ConfirmTransitionにチェックを入れると、ワールドを移動する前に以下のような確認ダイアログが表示されるようになります
          ![image](https://github.com/ClusterVR/ClusterONE/assets/37761378/689d3cef-9a27-43b0-810d-1fd896756fdd)

<a id="SingleGrabRestrictor"></a>
### 同時に複数持てないアイテム
- GrababbleItemの付いたGameObjectに`SingleGrabRestrictor`コンポーネントをアタッチする
    - ユーザーがルーム内でこの設定がされたアイテムAを掴んでいる状態で、この設定がされたアイテムBを別の手で掴もうとすると、アイテムAを手放してアイテムBを掴む
        - この際AのOnReleaseItemTriggerは(エラーが無ければ)実行される
        - この設定がされていないアイテムと同時に掴むことは可能
- 制作向けの注意点
    - このコンポーネントはエディタのプレビューでは動作しない

<a id="GrabPointOffset"></a>
### アイテムを持つ手の位置を調整する
- GrababbleItemの付いたGameObjectに`GrabPointOffset`コンポーネントをアタッチする
    - アイテムを持ったときの手の位置 (eye ローカル空間) に offset を加える
    - NonVR のみ
- 制作向けの注意点
    - このコンポーネントはエディタのプレビューでは動作しない
    - FGO(ワールド内で使われるアバターの体型がほぼ同じ)前提に設計
    - アイテム側の機能追加で不都合が生まれたら粉砕したくなりそうなので積極的には使ってほしくない

<a id="AvatarMakerTrigger"></a>
### アバターメイカーの起動トリガー
- コライダーの付いたGameObjectに`AvatarMakerTrigger`コンポーネントをアタッチする
    - このObjectをクリックするとアバターメイカーの起動トリガーとなります

<a id="MPBInstanceIDSetter"></a>
### ShaderにinstanceIDを設定する機能
- Rendererが自身か子についているGameObjectに`MPBInstanceIDSetter`コンポーネントをアタッチする
    - その後TimelineにVRMBlendShapeClipを配置することでBlendShapeを動かすことができる
- アタッチしたGameObject以下にあるRendererの`_RandomSeed`というMaterialPropertyにアタッチしたGameObjectのinstanceIDが設定される
    - Shaderにランダムな値を入れるために使う

<a id="OnProductPurchasedPlayerTrigger"></a>
### UGC商品が購入されたときに通知するトリガー
- 適当な GameObject に`OnProductPurchasedPlayerTrigger`コンポーネントをアタッチする
  - 購入を購読したい商品の商品Idを `ProductId プロパティ`に指定する  
  ![image](https://user-images.githubusercontent.com/37761378/190602248-141ae0a4-a03b-4f38-b04e-0d10457bd978.png)
    - 商品Idとは、商品詳細のURLの `https://cluster.mu/account/products/items/` 以降の文字列など
  - 商品が購入されたときに通知するトリガーをTriggers プロパティに設定する
    - TriggerのTargetは `SpecifiedItem`, `Player`, `Global`が指定可能
  - [ProductDisplayItem](https://docs.cluster.mu/creatorkit/item-components/product-display-item/) 等から起動されるストア機能で商品が購入された際、購入された商品の商品Idと一致する `ProductId` が設定されている`OnProductPurchasedPlayerTrigger`のトリガーが通知される
- 制作向けの注意点
    - エディタのプレビューではトリガーと同様の ProductId が設定されている [ProductDisplayItem](https://docs.cluster.mu/creatorkit/item-components/product-display-item/) にインタラクトした際に通知される
    - パッケージが購入された場合にはパッケージに含まれる全ての商品の商品ID、およびパッケージ自体の商品IDに紐づくトリガーが通知される
    - ゴーストやグループビューイングでは動作しない

<a id="MediaPlayer"></a>
### Live Streaming機能
- 適当な GameObject に`MediaPlayer`コンポーネントをアタッチする
  ![image](https://user-images.githubusercontent.com/4690128/213610669-2a110de5-52de-4e54-904a-bb492debc8a2.png)

  - 再生するメディアのURLを `Source Url` に指定する
  - 以下のいずれか、または両方を設定する
    - 動画の表示に使用されるRendererを `Target Renderers` に設定する
       - 必要に応じて、Rendererが使用するMaterialの中から、Textureを変更したいMaterialのproperty名を `Texture Property` に設定する
       - 複数のMaterialが同じ名前のpropertyを持つ場合、最初に見つかったMaterialのproperty名が設定される
    - 動画の表示に使用されるMaterialを `Target Materials` に設定する
       - 必要に応じて、Textureを変更したいMaterialのproperty名を `Texture Property` に設定する
- 制作向けの注意点
  - エディタのプレビューと実際の環境では、動作が異なる可能性があります
  - Inspectorで `Texture Property` を選択した時、フォーカスが外れずに、Inspectorでの操作を続けられなくなることがあります
     - その場合、Console Windowなどをクリックすると、フォーカスが外れます

<a id="RemoveIf"></a>
### プラットフォーム制限GameObject
- GameObject名を `[remove_if 除外したいプラットフォーム名]` から始めることで、そのプラットフォームからのみ除去することが出来る
  - 大文字小文字は問わない
  - プラットフォーム名は [ここ](https://docs.unity3d.com/ja/2019.4/ScriptReference/BuildTarget.html) にあるもの
    - WindowsのVRモードとWindowsのDesktopモードは区別できない
  - 複数指定することが出来る
    - 例: `[remove_if StandaloneWindows StandaloneOSX]` で始めると、WindowsとMacでのみワールドに存在しないようにできる
- 厳密にはCCK-eの機能ではなくCCKに入っているが、ユーザーには公開していない隠し機能

<a id="ExchangeTicketForAvatar"></a>
### アバターチケット引換
[アバター販売機能](#AvatarProduct)と[アバター販売機能用アバターディスプレイ](#ProductAvatarDisplay)を流用して実現しており、アバターストアが出てくるまでの暫定機能として用意した。

- チケット交換の導線を出すためのクリック対象としてコライダーの付いたGameObjectに`TicketExchangeableAvatar`コンポーネントをアタッチする
    - ProductUgcIdの欄に事前にエンプラ担当者がサーバー班に頼んで作ってもらったIdを指定する
- アバターの見た目を表示したい場合GameObjectに`TicketExchangeableAvatarDisplay`コンポーネントをアタッチする
    - ProductUgcIdの欄に事前にエンプラ担当者がサーバー班に頼んで作ってもらったIdを指定する
    - その他の項目は[アバター販売機能用アバターディスプレイ](#ProductAvatarDisplay)と同じように設定する

<a id="UnsafeStateUsableItem"></a>
### ScriptからGlobalStateに書き込めるアイテム
- アイテムに UnsafeStateUsableItem コンポーネントをつけることで、そのアイテムのスクリプトのsetStateCompat関数およびsendSignalCompat関数のtargetとして"global"を使えるようになります
- 制作向けの注意点
  - 特定の案件専用として用意しており、案件終了後には廃止する予定です

<a id="ControlTutorialArea"></a>
### 操作ガイド/チュートリアルワールド移動エリア

- チュートリアル関連ワールドでのみ使う機能
- Colliderに`ShowControlTutorialArea`コンポーネントをアタッチする
    - 何らかの基本操作のガイドを表示する場合、`GuideType`を選択し、かつ`CustomId`は空にする
        - 値は定義されているが未サポートの機能もある(後述)
    - チュートリアル関連ワールド間を移動するために使う場合、`GuideType`は`Custom`とし、`CustomId`に所定の文字列を入れる(後述)
- 基本操作ガイドの完了時にトリガーを発火させたい場合、何らかのオブジェクトに`OnTutorialGuideAchievedPlayerTrigger`をアタッチしてワールドに配置する
    - GuideTypeには、どのガイドが完了したとき発火してほしいかを指定する
        - 例えば見回し操作のガイドが終わったときTriggerを発火したい場合、`LookAround`という文字列を指定する
    - エディタ上でこのトリガーが発火した事にしたい場合、実行時のInspector上でボタンを押すと発火させられる

clientがサポートしている操作ガイド:

- 全PFでサポート
    - Jump
    - LookAround
    - Ride
    - GrabAndUseItem,
    - ReleaseItem,
- 全PFでサポートしているが、VR/NonVRで挙動が大きく異なる
    - Move
        - VRの場合、テレポート移動のon/offを尋ねる処理を含む
    - Emote
        - VRの場合、ハンドサインとエモーションの操作をこの順で説明する
- NonVRのみでサポートしており、VRではエリア進入後ただちに完了扱いになる
    - PersonView

チュートリアル関連ワールド間を移動するために指定できる`CustomId`:

- `GotoBasicControlTutorialWorld`: 基本操作チュートリアルワールドに移動
- `GotoLobbyPrepareWorld`: ロビー準備室ワールドに移動
- `GotoCraftTutorial`: クラフトチュートリアルに移動
- `GotoLobby`: ロビーに移動


<a id="InroomEventPickup"></a>
### inroomイベントピックアップ表示

- ロビーでのみ使う想定の機能
- 開催中のイベント情報を空間中に配置したいとき、その掲示する位置、向き、サイズをワールド側で指定できる。
    - ※ワールド側ではイベントピックアップ表示のGameObjectのon/offを制御できない

使い方

- 空のオブジェクトに`EventPickupObjectAnchor`をアタッチ
- オブジェクトの位置と向きはサムネイルを表示したい場所に合わせる。+Z方向がサムネイルの正面向きとなる。
- `Width`として、サムネイルの横幅をワールド上の実寸で指定する。
    - サムネイルのアスペクト比は16:9で固定のため、高さは指定できない。
    - Widthはあくまでワールド上でのサイズであり、Hierarchyのスケールは考慮されない。

<a id="OnboardingAnchor"></a>
### オンボーディング用Anchor

- チュートリアル関連ワールドでのみ使う機能
    - すべて特化型のコンポーネントに差し替わって移行 & 廃止する可能性があるので、チュートリアル関連ワールド以外では使わないで下さい。

- 使い方1: アカウント作成フローでアバターを配置したい場所を指定する
    - アバターを配置したい位置 + 向きに空のオブジェクトを置いて`OnboardingComponentAnchor`をアタッチ
    - `AnchorType`は`Custom`、CustomIdに`FirstSetupSpawnPoint`を指定
- 使い方2: チュートリアルワールド上で、アカウント作成中かつVRのときだけ表示したいオブジェクトを指定する
    - 対象オブジェクトに`OnboardingComponentAnchor`をアタッチ
    - `AnchorType`は`Custom`、かつCustomIdに`VROnlyObject`を指定する
- 使い方3: チュートリアルワールド上で、アカウント作成中は無効になるような音源を設置する
    - 対象オブジェクトに`OnboardingComponentAnchor`をアタッチ
    - `AnchorType`は`Custom`、かつCustomIdに`DisabledObjectOnAccountSetup`を指定する

<a id="ProhibitedGetOffItem"></a>
### 降りられない乗り物

- チュートリアル関連ワールドでのみ使う前提の機能
- `RidableItem`があるオブジェクトに`ProhibitedGetOffItem`をアタッチすると、ユーザーによるボタンの長押し操作では降りられなくなる
- 特にNonVRでは、このコンポーネントがアタッチしてあり、かつユーザーが何も操作しないと乗り物の正面付近にカメラが向き直る挙動も入る
- ギミックで降りることは禁止されない

<a id="VROrNonVROnlyObject"></a>
### VRまたはNonVRのみでアクティブになるオブジェクト

- チュートリアル関連ワールドでのみ使う前提の機能
- GameObjectに`VROrNonVRActiveObject`をアタッチ
    - `IsObjectForVR`をオフにすると、NonVR環境でのみ対象オブジェクトがアクティブになり、VR環境では非アクティブになる
    - `IsObjectForVR`をオンにすると、VR環境では対象オブジェクトがアクティブになり、NonVR環境では非アクティブになる
    - どちらのケースであっても、ロード後に`SetActive`が1回呼ばれる
    - Gimmick等で同じオブジェクトに対してSetActiveを呼ぶと後勝ちになってしまうので注意
- ワールドに静的に配置されたオブジェクトにアタッチして用いる


<a id="TutorialServerWorldGate"></a>
### チュートリアルサーバー強制ワールドゲート

- ロビーからチュートリアル専用ワールドに移動するワールドゲートでのみ使う前提の機能
- WorldGateを通常の方法で設定済みのGameObjectに対して、追加で`TutorialServerWorldGate`をアタッチすることで、移動先ワールドがチュートリアル専用サーバーになるのが保証される
    - このコンポーネントをアタッチしてよいのは、移動先がトラベラーズルーム、または基本操作チュートリアルワールドのWRSであるのを作業者が知っている場合のみ
 
<a id="SendAnalyticsPlayerGimmick"></a>
### Analytics送信ギミック

- 起動時に分析用のデータをサーバーに送信するギミックコンポーネント

使い方
- GameObjectに`SendAnalyticsPlayerGimmick`を追加する
- Inspectorからギミックの設定を行う
- ![8ccbb2827205c391caa596bbae9b1ad5](https://github.com/ClusterVR/ClusterONE/assets/37761378/903ff9b6-ed38-4afe-aa52-1387655fc1c0)
    - Target：どの種類の対象への(トリガーが通知した)メッセージを読み取るかを指定する
      - NOTE：ギミックが起動したアプリ個別に分析用データを送信するため、Target = Global等ユーザー間で同期されるようなメッセージを送信すると、ルーム内の全ユーザーから分析用データが送信される。
    - Key：メッセージの識別名を指定する
    - Analytics Id：分析用データの識別子を指定する


<a id="ReleaseProcedure"></a>
## ClusterCreatorKitEnterpriseのリリース手順

<a id="AddForClusterONE"></a>
### 追加・変更をClusterONEにマージ
1. ClusterONE/Alefgard/以下にあるcreator-kit-enterpriseのフォルダ内に対して必要な追加・変更を行う
    - 新規にasmdefを切った際に、 [link.xml](https://github.com/ClusterVR/ClusterONE/blob/master/Alefgard/unityproject/Assets/link.xml) に追記しないとモバイル版でstripしてしまうので注意が必要
        - link.xmlに追記しない
            - Interfaceを定義しているやつ
            - Editor用のasmdef
        - link.xmlに追記する
            - .Implementで終わっているやつ (unityprojectのコードからは直接参照されないがABのロードに必要であるため)
1. creator-kit-enterprise内にあるpackage.jsonのversionを変更する
    - 機能の追加なら0.1、修正なら0.0.1上げる
1. Readmeの機能の使い方に追加した機能の導入や使い方を追記
    - 修正のみならここはスキップ
1. 既存のClusterONEへのmerge手順と同じようにPRを出してClusterONEにマージする

<a id="ReleaseCCKE"></a>
### ClusterCreatorKitEnterpriseのリリース
1. ClusterCreatorKitEnterpriseのRepositoryをcloneする
    - https://github.com/ClusterVR/ClusterCreatorKitEnterprise
1. ClusterONE/Alefgard/以下にあるcreator-kit-enterpriseのフォルダの中身をこのRepositoryのフォルダ内に上書きする
    - 削除がある場合はClusterCreatorKitEnterpriseのRepositoryのフォルダ内の該当フォルダ・ファイルを削除
1. masterにcommitしてそのcommitに "v" + package.jsonで変更したversion でtagを付ける(`v1.2.3`という感じに)
1. そのままmasterにpush
1. ClusterCreatorKitEnterpriseのRepositoryのReleasesにreleaseを追加する
    - 先程作ったtagを使い、タイトルはtagと同じで追加された機能を書いて追加します
<a id="ShareToEnterpriseCGTeam"></a>
### EnterpriseのCGTeamに共有
1. slack の #general で @here を使い、追加された機能の実装を通知する
    - リリース予定バージョン、あるいはリリース予定がなければどのバージョンを元にしたかの情報を添える
    - 実装されたアプリがリリースされたタイミングでも通知する
    - リリースではないが告知をしたい場合はTAチームにメンションする
        - 2023/07 現在は @M.Ibara @acchi

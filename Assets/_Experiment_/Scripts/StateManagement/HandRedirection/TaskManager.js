function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 6) {
    $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
$.state.handOffset = new Quaternion()
  .setFromEulerAngles(new Vector3(0, 90, 0)); // 実身体の手（コントローラ）とバーチャル手の回転の差を補正するオフセット

$.subNode("StartingPoint").setEnabled(true); // 原点を表示する
$.subNode("TargetPoint").setEnabled(false); // ターゲットを非表示にする

// 原点を手前30cmの位置に置く
$.subNode("StartingPoint").setPosition(
  $.state.player.getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.3)));

// ターゲットを手前60cmの位置に置く
$.subNode("TargetPoint").setPosition(
  $.state.player.getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.6)));
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 6) {
    if (!$.state.player || !$.state.handOriginalPos) return;

// バーチャル手の位置を計算する：実身体の手（コントローラ）の移動量×ゲイン
$.subNode("RightHandAnchor").setPosition(
  $.state.handOriginalPos.clone()
    .add($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand).clone()
      .sub($.state.handOriginalPos)
      .multiplyScalar($.state.isReaching ? $.state.gain : 1)));
  
// バーチャル手の回転を実身体の手と同期させる
$.subNode("RightHandAnchor")
  .setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand)
    .clone().multiply($.state.handOffset));
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}


$.onCollide(collision => {
  if ($.state.isReaching) {
    $.state.isReaching = false; // リーチング（目標地点まで手を伸ばす）フラグをfalseにする
    ToNextState(); // 次のステート（質問に回答するフェーズ）に遷移させる
  } else {
    $.subNode("StartingPoint").setEnabled(false);
    $.subNode("TargetPoint").setEnabled(true);
    $.state.gain = parseFloat(CONDITION["gain"]); // この試行におけるゲインの値を設定する
    $.state.handOriginalPos = $.subNode("RightHandAnchor").getPosition().clone();
    $.state.isReaching = true; // リーチング（目標地点まで手を伸ばす）フラグをtrueにする
  }
});

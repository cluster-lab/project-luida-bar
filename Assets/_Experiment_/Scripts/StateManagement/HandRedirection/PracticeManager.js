function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 2) {
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

  if (STATE_ID === 2) {
    if (!$.state.player || !$.state.handOriginalPos) return;

$.subNode("RightHandAnchor").setPosition(
  $.state.handOriginalPos.clone()
    .add($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand).clone()
      .sub($.state.handOriginalPos)
      .multiplyScalar($.state.isReaching ? $.state.gains[$.state.gainID] : 1)));

$.subNode("RightHandAnchor")
  .setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand)
    .clone().multiply($.state.handOffset));
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}


$.onStart(() => {
  $.state.gains = [1, 0.75, 1.25];
  $.state.gainID = 0;
});

$.onCollide(collision => {
  if ($.state.isReaching) {
    $.state.isReaching = false;
    $.state.gainID = $.state.gainID + 1;
    ToNextState();
  } else {
    $.subNode("StartingPoint").setEnabled(false);
    $.subNode("TargetPoint").setEnabled(true);
    $.state.handOriginalPos = $.subNode("RightHandAnchor").getPosition().clone();
    $.state.isReaching = true;
  }
});

function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 4) {
    if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 4) {
    // プレイヤーの右手の位置と同期させる
$.setPosition($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand).clone());

// プレイヤーの右手の回転と同期させる
$.setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand).clone());
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}




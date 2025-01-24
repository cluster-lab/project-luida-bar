function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 3) {
    if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
    // // プレイヤーの左手の位置と同期させる
$.setPosition($.state.player.getHumanoidBonePosition(HumanoidBone.LeftHand).clone());

// // プレイヤーの左手の回転と同期させる
$.setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.LeftHand).clone());
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}




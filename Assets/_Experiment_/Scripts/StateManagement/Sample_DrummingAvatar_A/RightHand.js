function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 2) {
    $.setStateCompat('this', 'exp_showItem', true);
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 2) {
    if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
// プレイヤーの手の位置と同期させる
$.setPosition($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand));
$.setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand));
  }
  if (STATE_ID === 3) {
    if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
// プレイヤーの手の位置と同期させる
$.setPosition($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand));
$.setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand));
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', true);
    if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}


function Update(deltaTime) {
  if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
  // プレイヤーの手の位置と同期させる
  $.setPosition($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand).clone());
  $.setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand).clone());
}

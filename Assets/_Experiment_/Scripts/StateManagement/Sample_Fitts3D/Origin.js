function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 3) {
    // 原点オブジェクトを頭の正面から50cm離れたところまで動かす
if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
let originPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head)
.clone().add(new Vector3(0, 0, 0.5));
$.setPosition(originPos);
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 7) {
    // 原点オブジェクトを頭の正面から50cm離れたところまで動かす
if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
let originPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head)
.clone().add(new Vector3(0, 0, 0.5));
$.setPosition(originPos);
    $.setStateCompat('this', 'exp_showItem', true);
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 7) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




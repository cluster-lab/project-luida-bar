function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 3) {
    // カーソルを参加者の手前に配置する
if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
let originPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head)
.clone().add(new Vector3(0, -0.2, 0.4));
$.setPosition(originPos);
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 7) {
    // カーソルを参加者の手前に配置する
if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
let originPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head)
.clone().add(new Vector3(0, -0.2, 0.4));
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

  if (STATE_ID === 4) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 8) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




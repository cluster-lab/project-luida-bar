function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 4) {
    if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];

// 原点（頭の正面から50cm）まで動かす
let originPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head)
  .clone().add(new Vector3(0, 0, 0.5));
$.setPosition(originPos);

// 色を半透明に
$.material("mat").setBaseColor(1, 1, 0, 0.7);
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 9) {
    if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];

// 原点（頭の正面から50cm）まで動かす
let originPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head)
  .clone().add(new Vector3(0, 0, 0.5));
$.setPosition(originPos);

// 色を半透明に
$.material("mat").setBaseColor(1, 1, 0, 0.7);
    $.setStateCompat('this', 'exp_showItem', true);
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 4) {
    // ホバー時は不透明にし、非ホバー時は半透明にする
if ($.state.isHovered !== $.getStateCompat("this", "isHovered", "boolean")) {
  $.material("mat").setBaseColor(1, 1, 0, $.state.isHovered ? 0.7 : 1);
  $.state.isHovered = !$.state.isHovered;
}
  }
  if (STATE_ID === 9) {
    // ホバー時は不透明にし、非ホバー時は半透明にする
if ($.state.isHovered !== $.getStateCompat("this", "isHovered", "boolean")) {
  $.material("mat").setBaseColor(1, 1, 0, $.state.isHovered ? 0.7 : 1);
  $.state.isHovered = !$.state.isHovered;
}
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 4) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 9) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




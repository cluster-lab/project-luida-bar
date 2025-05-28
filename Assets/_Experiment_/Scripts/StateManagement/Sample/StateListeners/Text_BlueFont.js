function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
    if (CONDITION['depth'] === 'near') {
  $.setPosition(new Vector3(0, 1.5, 1))
}
    if (CONDITION['depth'] === 'far') {
  $.setPosition(new Vector3(0, 1.5, 3))
}
    if (CONDITION['font'] === 'B') {
  $.setStateCompat('this', 'exp_showItem', true);
}
    if (CONDITION['text'] === 'Red') {
  $.subNode('Text').setText(`Red`);
}
    if (CONDITION['text'] === 'Blue') {
  $.subNode('Text').setText(`Blue`);
}
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




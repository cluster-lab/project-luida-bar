function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 1) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode('Text').setText(`This experiment...`);
  }
  if (STATE_ID === 3) {
    if (CONDITION['request'] === 'font') {
  $.subNode('Text').setText(`Click the button that matches the text's font color.`);
}
    if (CONDITION['request'] === 'meaning') {
  $.subNode('Text').setText(`Click the button that matches the text's meaning.`);
}
  }
  if (STATE_ID === 5) {
    $.subNode('Text').setText(`Well done!`);
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 1) {
  }
  if (STATE_ID === 3) {
  }
  if (STATE_ID === 5) {
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 1) {
  }
  if (STATE_ID === 3) {
    $.subNode('Text').setText(`Take a break for 3 seconds`);
  }
  if (STATE_ID === 5) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




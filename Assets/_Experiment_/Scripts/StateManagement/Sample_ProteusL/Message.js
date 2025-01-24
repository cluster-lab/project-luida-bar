function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`
Start
`);
  }
  if (STATE_ID === 2) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`
Instruction
`);
  }
  if (STATE_ID === 4) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`
Rest
`);
  }
  if (STATE_ID === 5) {
    $.subNode("Text").setText(`
Next world
`);
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 2) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




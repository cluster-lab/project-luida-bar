function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
    $.setStateCompat('this', 'exp_showItem', true);
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




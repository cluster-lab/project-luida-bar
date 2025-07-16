function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 4) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.sendSignalCompat('this', 'exp_recordCustomData');
  }
  if (STATE_ID === 2) {
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 7) {
    $.sendSignalCompat('this', 'exp_uploadCustomData');
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
    $.sendSignalCompat('this', 'exp_recordCustomData');
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 5) {
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 10) {
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 12) {
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

  if (STATE_ID === 5) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 10) {
    $.setStateCompat('this', 'exp_showItem', false);
    $.sendSignalCompat('this', 'exp_recordCustomData');
  }
}




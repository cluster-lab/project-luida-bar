function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 6) {
    $.state.taskTime = 0;
  }
  if (STATE_ID === 9) {
    $.sendSignalCompat('this', 'exp_uploadCustomData');
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 6) {
    if ($.getStateCompat("global", "isInTask", "boolean")) {
  $.state.taskTime = $.state.taskTime + deltaTime;
}
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 6) {
    $.setStateCompat("owner", "taskTime", $.state.taskTime);
    $.sendSignalCompat('this', 'exp_recordCustomData');
  }
}




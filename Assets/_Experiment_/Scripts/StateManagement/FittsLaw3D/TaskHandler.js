function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 5) {
    $.state.taskTime = 0;
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 5) {
    if ($.getStateCompat("global", "isInTask", "boolean")) {
  $.state.taskTime = $.state.taskTime + deltaTime;
}
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 5) {
    $.setStateCompat("owner", "taskTime", $.state.taskTime);
    $.sendSignalCompat('this', 'exp_recordCustomData');
  }
}




function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
    $.state.isInTrial = true;
  }
  if (STATE_ID === 5) {
    $.sendSignalCompat('this', 'exp_uploadCustomData');
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
  }
  if (STATE_ID === 5) {
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
    $.state.isInTrial = false;
$.setStateCompat(
  "owner", "timer", $.state.timer);
    $.sendSignalCompat('this', 'exp_recordCustomData');
  }
  if (STATE_ID === 5) {
  }
}


function Start() { $.state.timer = 0; }
function Update(deltaTime) {
  if ($.state.isInTrial)
    $.state.timer += deltaTime;
}

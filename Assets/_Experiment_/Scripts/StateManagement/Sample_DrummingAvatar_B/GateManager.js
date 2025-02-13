function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 6) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("GateL").setEnabled(CONDITION["avatar"] === "L");
$.subNode("GateD").setEnabled(CONDITION["avatar"] === "D");
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}




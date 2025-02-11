function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 2) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode('Text').setText(`開始`);
  }
  if (STATE_ID === 7) {
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 12) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode('Text').setText(`次へ`);
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
  if (STATE_ID === 7) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 12) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




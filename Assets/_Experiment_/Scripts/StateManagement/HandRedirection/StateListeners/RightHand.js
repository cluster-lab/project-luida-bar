function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;
  const PARTICIPANTS = $.groupState.participants;

  if (STATE_ID === 2) {
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 3) {
  }
  if (STATE_ID === 5) {
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;
  const PARTICIPANTS = $.groupState.participants;

  if (STATE_ID === 2) {
    // Always follows participant's 
real right hand
  }
  if (STATE_ID === 3) {
    // Always follows participant's
 real right hand * gain
  }
  if (STATE_ID === 5) {
    // Always follows participant's 
real right hand
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;
  const PARTICIPANTS = $.groupState.participants;

  if (STATE_ID === 2) {
  }
  if (STATE_ID === 3) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 5) {
  }
}


// function Start() { }
// function Update(deltaTime) { }
// $.onCollide((collision) => { });
// $.onGrab((isGrab, isLeftHand, player) => { });
// $.onInteract((player) => { });
// $.onUse((isDown, player) => { });
// $.onPhysicsUpdate((deltaTime) => { });
// $.onReceive((messageType, arg, sender) => { });


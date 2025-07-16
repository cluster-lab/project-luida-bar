function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;
  const PARTICIPANTS = $.groupState.participants;

  if (STATE_ID === 1) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode('Text').setText(`In this experiment,
you will be asked to repeat touching a green ball.
After touching it once, the ball moves forward, and you need to reach your arm to touch it again.
Then, you should answer whether you hand moved faster or slower than your real hand during the reaching.
Press the button to get started.`);
  }
  if (STATE_ID === 2) {
    $.subNode('Text').setText(`Touch the green ball in front of you.`);
  }
  if (STATE_ID === 3) {
    $.subNode('Text').setText(`Reach your arm to touch the green ball again.`);
  }
  if (STATE_ID === 5) {
    $.subNode('Text').setText(`Put your hand down`);
  }
  if (STATE_ID === 7) {
    $.subNode('Text').setText(`Thank you for your participation!`);
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;
  const PARTICIPANTS = $.groupState.participants;

  if (STATE_ID === 1) {
  }
  if (STATE_ID === 2) {
  }
  if (STATE_ID === 3) {
  }
  if (STATE_ID === 5) {
  }
  if (STATE_ID === 7) {
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;
  const PARTICIPANTS = $.groupState.participants;

  if (STATE_ID === 1) {
  }
  if (STATE_ID === 2) {
  }
  if (STATE_ID === 3) {
  }
  if (STATE_ID === 5) {
  }
  if (STATE_ID === 7) {
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


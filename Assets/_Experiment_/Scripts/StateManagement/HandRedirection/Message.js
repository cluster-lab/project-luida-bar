const stateEnterActions = {
    1: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`You will be asked to repeat touching a green ball.
            After touching it once, the ball moves forward, 
            and you need to reach your arm to touch it again.
            Then, you should answer whether you hand moved 
            faster or slower than your real hand during the reaching.
            Press the button to get started.`);
        } }
    ],
    2: [
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`Touch the green ball in front of you.`);
        } }
    ],
    3: [
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`Reach your arm to touch the green ball again.`);
        } }
    ],
    4: [
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`Do you feel the virtual hand moved 
            faster or slower than your real hand?`);
        } }
    ],
    5: [
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`Put your hand down`);
        } }
    ],
    7: [
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`Thank you for your participation!`);
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
};


// function Start() { }
// function Update(deltaTime) { }
// $.onCollide((collision) => { });
// $.onGrab((isGrab, isLeftHand, player) => { });
// $.onInteract((player) => { });
// $.onUse((isDown, player) => { });
// $.onPhysicsUpdate((deltaTime) => { });
// $.onReceive((messageType, arg, sender) => { });
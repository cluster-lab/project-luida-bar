const stateEnterActions = {
    1: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`This experiment...`);
        } }
    ],
    3: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: (deltaTime) => {
            if (CONDITION['request'] === 'font') {
              $.subNode('Text').setText(`Click the button that
              matches the text's font color`);
            }
        } },
        { type: "exec", action: (deltaTime) => {
            if (CONDITION['request'] === 'meaning') {
              $.subNode('Text').setText(`Click the button that
              matches the text's meaning`);
            }
        } }
    ],
    4: [
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`Take a break for 3 seconds`);
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
    1: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', false);
        } }
    ],
    4: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', false);
        } }
    ]
};


// function Start() { }
// function Update(deltaTime) { }
// $.onCollide((collision) => { });
// $.onGrab((isGrab, isLeftHand, player) => { });
// $.onInteract((player) => { });
// $.onUse((isDown, player) => { });
// $.onPhysicsUpdate((deltaTime) => { });
// $.onReceive((messageType, arg, sender) => { });
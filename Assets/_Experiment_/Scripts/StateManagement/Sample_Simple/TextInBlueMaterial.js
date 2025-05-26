const stateEnterActions = {
    2: [
        { type: "exec", action: () => {
            if (CONDITION['material'] === 'B') {
              $.setStateCompat('this', 'exp_showItem', true);
            }
        } },
        { type: "exec", action: () => {
            if (CONDITION['material'] === 'R') {
              $.setStateCompat('this', 'exp_showItem', false);
            }
        } },
        { type: "exec", action: () => {
            if (CONDITION['text'] === 'R') {
              $.subNode('Text').setText(`Red`);
            }
        } },
        { type: "exec", action: () => {
            if (CONDITION['text'] === 'B') {
              $.subNode('Text').setText(`Blue`);
            }
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
    2: [
        { type: "exec", action: () => {
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
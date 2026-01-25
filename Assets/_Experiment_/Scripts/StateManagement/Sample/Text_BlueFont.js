const stateEnterActions = {
    3: [
        { type: "exec", action: (deltaTime) => {
            if (CONDITION['depth'] === 'near') {
              $.setPosition(new Vector3(0, 1.5, 1))
            }
        } },
        { type: "exec", action: (deltaTime) => {
            if (CONDITION['depth'] === 'far') {
              $.setPosition(new Vector3(0, 1.5, 3))
            }
        } },
        { type: "exec", action: (deltaTime) => {
            if (CONDITION['text'] === 'Red') {
              $.subNode('Text').setText(`Red`);
            }
        } },
        { type: "exec", action: (deltaTime) => {
            if (CONDITION['text'] === 'Blue') {
              $.subNode('Text').setText(`Blue`);
            }
        } },
        { type: "exec", action: (deltaTime) => {
            if (CONDITION['font'] === 'B') {
              $.setStateCompat('this', 'exp_showItem', true);
            }
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
    3: [
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
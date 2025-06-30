const stateEnterActions = {
    3: [
        { type: "exec", action: () => {
            if (CONDITION['depth'] === 'near') {
              $.setPosition(new Vector3(0, 1.5, 1))
            }
        } },
        { type: "exec", action: () => {
            if (CONDITION['depth'] === 'far') {
              $.setPosition(new Vector3(0, 1.5, 3))
            }
        } },
        { type: "exec", action: () => {
            if (CONDITION['text'] === 'Red') {
              $.subNode('Text').setText(`Red`);
            }
        } },
        { type: "exec", action: () => {
            if (CONDITION['text'] === 'Blue') {
              $.subNode('Text').setText(`Blue`);
            }
        } },
        { type: "exec", action: () => {
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
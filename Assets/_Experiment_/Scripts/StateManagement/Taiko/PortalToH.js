const stateEnterActions = {
    3: [
        { type: "exec", action: (deltaTime) => {
            $.setPosition(new Vector3(0, 0, 2))
        } },
        { type: "exec", action: (deltaTime) => {
            if (CONDITION['selfAvatar'] === 'happi') {
              PARTICIPANTS[1].setPosition(new Vector3(0, 0, -2));
            }
        } },
        { type: "sleep", value: 1 },
        { type: "exec", action: (deltaTime) => {
            if (CONDITION['selfAvatar'] === 'happi') {
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
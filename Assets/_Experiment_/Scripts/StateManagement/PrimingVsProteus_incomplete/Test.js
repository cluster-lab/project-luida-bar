const stateEnterActions = {
    0: [
        { type: "exec", action: (deltaTime) => {
            if (!$.groupState.collectedData) $.groupState.collectedData = {};
                let collectedData = $.groupState.collectedData;
                collectedData[''] = ;
                $.groupState.collectedData = collectedData;
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
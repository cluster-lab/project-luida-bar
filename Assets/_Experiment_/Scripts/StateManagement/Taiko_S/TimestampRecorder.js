const stateEnterActions = {
    3: [
        { type: "exec", action: (deltaTime) => {
            $.state.startTime = Date.now();
        } }
    ],
    5: [
        { type: "exec", action: (deltaTime) => {
            $.sendSignalCompat('this', 'exp_recordCustomData');
        } }
    ],
    6: [
        { type: "exec", action: (deltaTime) => {
            $.sendSignalCompat('this', 'exp_uploadCustomData');
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
    3: [
        { type: "exec", action: (deltaTime) => {
            $.state.endTime = Date.now();
            SendDataToCollector("timestamp", {
              start: $.state.startTime,
              end: $.state.endTime
            });
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
const stateEnterActions = {
    1: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: () => {
            $.setPosition(new Vector3(0, 2, 2))
        } },
        { type: "exec", action: () => {
            $.subNode('Text').setText(`Trials start in 5 seconds.
            
            In each trial, you’ll see a text written
            "Red" or "Blue" in red or blue font,
            which may not necessarily match the word.
            You will be asked to click the sphere matching
            either the font color or the word's meaning.`);
        } }
    ],
    2: [
        { type: "exec", action: () => {
            if (CONDITION['request'] === 'material') {
              $.subNode('Text').setText(`Click the sphere that matches the font color`);
            }
        } },
        { type: "exec", action: () => {
            if (CONDITION['request'] === 'text') {
              $.subNode('Text').setText(`Click the sphere that matches the word’s meaning`);
            }
        } },
        { type: "exec", action: () => {
            $.setPosition(new Vector3(0, 2, 1.5))
        } }
    ],
    3: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`Take a rest for 3 seconds`);
        } }
    ],
    4: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`Well done! Thanks for your participation.
            You can leave this room now.`);
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
    4: [
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
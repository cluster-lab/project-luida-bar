const stateEnterActions = {
    1: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: () => {
            $.subNode('Text').setText(`You will first repeat a calculation task five times.
            Next, you will be shown text in which
            the color and the meaning of the word differ.
            Follow the instructions to either 
            select the ball that matches the "meaning" of the text,
            or the ball that matches the "font color" of the text.
            The experiment will start soon...`);
        } }
    ],
    3: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: () => {
            if (CONDITION['request'] === 'font') {
              $.subNode('Text').setText(`Click the button that
              matches the text's "font color"`);
            }
        } },
        { type: "exec", action: () => {
            if (CONDITION['request'] === 'meaning') {
              $.subNode('Text').setText(`Click the button that
              matches the text's "meaning"`);
            }
        } }
    ],
    4: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`Take a break for 3 seconds`);
        } }
    ],
    6: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: () => {
            $.subNode('Text').setText(`Thank you for the participation!
            Now, you can leave this world freely,
            or step on the portal in front of you
            to return to LUIDA's recruitment world.`);
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
    1: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } }
    ],
    4: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } }
    ],
    6: [
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
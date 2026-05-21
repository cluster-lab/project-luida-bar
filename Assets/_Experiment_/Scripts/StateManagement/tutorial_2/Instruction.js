const stateEnterActions = {
    0: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`Welcome. In this experiment,
            you will embody an avatar to pass a bridge.`);
        } }
    ],
    1: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`Preparing trial...`);
        } },
        { type: "sleep", value: 3 },
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`Look in the mirror`);
        } },
        { type: "sleep", value: 10 },
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`Walk to the other side
            via one of the bridges`);
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
    ]
};
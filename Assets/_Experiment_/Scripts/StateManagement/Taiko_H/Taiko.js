const stateEnterActions = {
    3: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', true);
        } }
    ],
    6: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', false);
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
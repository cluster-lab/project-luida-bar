const stateEnterActions = {
    3: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
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
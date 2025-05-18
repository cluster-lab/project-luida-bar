const stateEnterActions = {
    0: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "sleep", value: 2 },
        { type: "exec", action: () => {
            $.sendSignalCompat('this', 'state_triggerTransition');
        } },
        { type: "exec", action: () => {
            $.setPosition(new Vector3(0, 1, 1))
        } },
    ],
    1: [
        { type: "sleep", value: 2 },
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: () => {
            $.setPosition(new Vector3(0, 2, 3))
        } },
        { type: "sleep", value: 2 },
        { type: "exec", action: () => {
            $.sendSignalCompat('this', 'state_triggerTransition');
        } },
    ],
};

const duringStateActions = {
};

const stateExitActions = {
    0: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
    1: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
};
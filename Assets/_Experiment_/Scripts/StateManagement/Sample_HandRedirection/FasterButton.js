const stateEnterActions = {
    0: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
    5: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
    ],
    10: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
    ],
    12: [
        { type: "exec", action: () => {
            $.sendSignalCompat('this', 'exp_uploadCustomData');
        } },
    ],
};

const duringStateActions = {
};

const stateExitActions = {
    5: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
    10: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
        { type: "exec", action: () => {
            $.sendSignalCompat('this', 'exp_recordCustomData');
        } },
    ],
};


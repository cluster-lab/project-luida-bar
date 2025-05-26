const stateEnterActions = {
    0: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
    ],
    2: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: () => {
            $.subNode('Text').setText(`開始`);
        } },
    ],
    7: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
    ],
    12: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: () => {
            $.subNode('Text').setText(`次へ`);
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
    2: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
    7: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
    12: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
};


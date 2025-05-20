const stateEnterActions = {
    0: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: () => {
            $.setPosition(new Vector3(0, 1, 1))
        } }
    ],
    1: [
        { type: "exec", action: () => {
            $.setPosition(new Vector3(0, 2, 2))
        } }
    ],
    2: [
        { type: "exec", action: () => {
            $.log(CONDITION['within']);
            $.log(CONDITION['between']);
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
};
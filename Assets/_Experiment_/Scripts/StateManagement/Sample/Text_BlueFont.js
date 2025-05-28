const stateEnterActions = {
    3: [
        { type: "exec", action: () => {
            if (CONDITION['depth'] === 'near') {
              $.setPosition(new Vector3(0, 1.5, 1))
            }
        } },
        { type: "exec", action: () => {
            if (CONDITION['depth'] === 'far') {
              $.setPosition(new Vector3(0, 1.5, 3))
            }
        } },
        { type: "exec", action: () => {
            if (CONDITION['font'] === 'B') {
              $.setStateCompat('this', 'exp_showItem', true);
            }
        } },
        { type: "exec", action: () => {
            if (CONDITION['text'] === 'Red') {
              $.subNode('Text').setText(`Red`);
            }
        } },
        { type: "exec", action: () => {
            if (CONDITION['text'] === 'Blue') {
              $.subNode('Text').setText(`Blue`);
            }
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
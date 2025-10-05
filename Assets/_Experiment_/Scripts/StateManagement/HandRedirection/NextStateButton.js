const stateEnterActions = {
    1: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', true);
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


$.onInteract((player) => { 
  ToNextState();
});
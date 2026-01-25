const stateEnterActions = {
    2: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: (deltaTime) => {
            $.setPosition(new Vector3(0, 1, -1))
        } }
    ],
    3: [
        { type: "exec", action: (deltaTime) => {
            $.setPosition(new Vector3(0, 1, 0))
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


$.onCollide((collision) => { 
  ToNextState();
});
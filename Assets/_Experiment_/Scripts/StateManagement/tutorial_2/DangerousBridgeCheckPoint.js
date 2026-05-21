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


$.onCollide((collision) => {
    if (!$.groupState.collectedData) $.groupState.collectedData = {};
        let collectedData = $.groupState.collectedData;
        collectedData['bridge'] = 'danger';
        $.groupState.collectedData = collectedData;
    $.sendSignalCompat('this', 'exp_recordCustomData');
    $.sendSignalCompat('this', 'state_triggerTransition');
});
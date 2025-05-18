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
    2: [
        { type: "exec", action: () => {
            if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0]; 
             $.state.player.send('haptics', {target: "right", frequency: 0.1, amplitude: 0.5, duration: 3});
        } },
        { type: "sleep", value: 3 },
        { type: "exec", action: () => {
            if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0]; 
             $.state.player.send('haptics', {target: "left", frequency: 0.1, amplitude: 1, duration: 3});
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
const stateEnterActions = {
    3: [
        { type: "exec", action: () => {
            $.state.isInTrial = true;
            $.state.timer = 0;
        } }
    ],
    5: [
        { type: "exec", action: () => {
            $.sendSignalCompat('this', 'exp_uploadCustomData');
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
    3: [
        { type: "exec", action: () => {
            $.state.isInTrial = false;
            SendDataToCollector(
                "timer", $.state.timer);
        } },
        { type: "exec", action: () => {
            $.sendSignalCompat('this', 'exp_recordCustomData');
        } }
    ]
};


function Start() { $.state.timer = 0; }
function Update(deltaTime) {
  if ($.state.isInTrial) $.state.timer += deltaTime;
}
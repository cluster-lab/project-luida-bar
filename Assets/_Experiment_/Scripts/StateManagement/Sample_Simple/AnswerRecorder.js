const stateEnterActions = {
    2: [
        { type: "exec", action: () => {
            $.state.isAnswering = true;
            $.state.answerTime = 0;
        } }
    ],
    4: [
        { type: "exec", action: () => {
            $.sendSignalCompat('this', 'exp_uploadCustomData');
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
    2: [
        { type: "exec", action: () => {
            $.state.isAnswering = false;
            $.setStateCompat("owner", "answerTime", $.state.answerTime);
        } },
        { type: "exec", action: () => {
            $.sendSignalCompat('this', 'exp_recordCustomData');
        } }
    ]
};


function Update(deltaTime) {
  if ($.state.isAnswering) $.state.answerTime += deltaTime;
}
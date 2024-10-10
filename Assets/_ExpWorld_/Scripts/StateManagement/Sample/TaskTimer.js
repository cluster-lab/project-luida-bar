function OnStateEnter() {
    $.state.timer = 0;
}

function OnStateExit () {
    $.setStateCompat("owner", "taskTime", $.state.timer);
    $.sendSignalCompat("this", "exp_tryRecordCustomData");
    $.state.timer = 0;
}

function DuringState (deltaTime) {
    $.state.timer += deltaTime;
}
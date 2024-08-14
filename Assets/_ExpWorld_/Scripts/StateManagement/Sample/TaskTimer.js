$.onStart(() => {
    $.sendSignalCompat("this", "state_setID");
})

$.onUpdate((deltaTime) => {
    if ($.getStateCompat("this", "state_exit", "boolean")) {
        OnStateExit();
        $.setStateCompat("this", "state_exit", false);
    }
    if ($.getStateCompat("this", "state_enter", "boolean")) {
        $.setStateCompat("this", "state_enter", false);
        OnStateEnter();
    }
    if ($.getStateCompat("this", "state_isActive", "boolean")) {
        DuringState(deltaTime);
    }
})

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
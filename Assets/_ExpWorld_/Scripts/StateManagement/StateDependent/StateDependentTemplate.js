$.onStart(() => {
    $.sendSignalCompat("this", "state_setID");
})

$.onUpdate(() => {
    if ($.getStateCompat("this", "state_exit", "boolean")) {
        OnStateExit();
        $.setStateCompat("this", "state_exit", false);
    }
    if ($.getStateCompat("this", "state_enter", "boolean")) {
        $.setStateCompat("this", "state_enter", false);
        OnStateEnter();
    }
    DuringState();
})

function OnStateEnter() {}

function OnStateExit () {}

function DuringState () {}
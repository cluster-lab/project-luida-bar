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
    Update(deltaTime);
})

function OnStateEnter() {}

function OnStateExit () {}

function DuringState (deltaTime) {}

function Update (deltaTime) {}
$.onUpdate((deltaTime) => {
    if ($.getStateCompat("this", "state_exit", "boolean")) {
        OnStateExit();
        $.setStateCompat("this", "state_exit", false);
    }
    if ($.getStateCompat("this", "state_enter", "boolean")) {
        $.setStateCompat("this", "state_enter", false);
        $.state.state_id = $.getStateCompat("global", "state_currentID", "integer");
        OnStateEnter();
    }
    if ($.getStateCompat("this", "state_isActive", "boolean")) {
        DuringState(deltaTime);
    }
    Update(deltaTime);
})

function ShowItem() {
    $.setStateCompat("this", "exp_showItem", true);
}

function HideItem() {
    $.setStateCompat("this", "exp_showItem", false);
}

function ToNextState() {
    $.sendSignalCompat("this", "state_triggerTransition");
}

function RecordCustomData() {
    $.sendSignalCompat("this", "exp_recordCustomData");
}

function UploadRecordedData() {
    $.sendSignalCompat("this", "exp_uploadCustomData");
}

function OnStateEnter() {}

function OnStateExit() {}

function DuringState(deltaTime) {}

function Update(deltaTime) {}

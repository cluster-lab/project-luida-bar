let timer = 0;
let CONDITION;

$.onStart(() => {
    $.state.last_state_id = 0;
    $.state.state_id = 0;
    $.state.stateEnterActionID = -1;
    $.state.stateExitActionID = -1;
    $.state.duringStateActionID = -1;
    Start();
})

$.onUpdate((deltaTime) => {
    if ($.getStateCompat("this", "state_exit", "boolean")) {
        $.state.stateExitActionID = 0;
        $.setStateCompat("this", "state_exit", false);
    }
    if ($.state.stateExitActionID >= 0) OnStateExit(deltaTime);

    if ($.getStateCompat("this", "state_enter", "boolean")) {
        $.setStateCompat("this", "state_enter", false);
        $.state.state_id = $.getStateCompat("global", "state_currentID", "integer");
        $.state.stateEnterActionID = 0;
        $.state.duringStateActionID = 0;
    }
    if ($.state.stateEnterActionID >= 0) OnStateEnter(deltaTime);

    if ($.state.duringStateActionID >= 0) DuringState(deltaTime);

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

function OnStateEnter(deltaTime) {
    CONDITION = $.groupState.currentCondition;
    if (!stateEnterActions[$.state.state_id] || $.state.stateEnterActionID >= stateEnterActions[$.state.state_id].length) return;
    
    while ($.state.stateEnterActionID < stateEnterActions[$.state.state_id].length && stateEnterActions[$.state.state_id][$.state.stateEnterActionID].type !== "sleep") {
        stateEnterActions[$.state.state_id][$.state.stateEnterActionID].action();
        $.state.stateEnterActionID += 1;
    }

    if ($.state.stateEnterActionID >= stateEnterActions[$.state.state_id].length) {
        $.state.stateEnterActionID = -1;
    } else if (stateEnterActions[$.state.state_id][$.state.stateEnterActionID].type === "sleep") {
        if (timer >= stateEnterActions[$.state.state_id][$.state.stateEnterActionID].value) {
            $.state.stateEnterActionID += 1;
            timer = 0;
        } else {
            timer += deltaTime;
        }
    }
}

function OnStateExit(deltaTime) {
    if (!stateExitActions[$.state.last_state_id] || $.state.stateExitActionID >= stateExitActions[$.state.last_state_id].length) return;

    while ($.state.stateExitActionID < stateExitActions[$.state.last_state_id].length && stateExitActions[$.state.last_state_id][$.state.stateExitActionID].type !== "sleep") {
        stateExitActions[$.state.last_state_id][$.state.stateExitActionID].action();
        $.state.stateExitActionID += 1;
    }

    if ($.state.stateExitActionID >= stateExitActions[$.state.last_state_id].length) {
        $.state.stateExitActionID = -1;
        $.state.last_state_id = $.state.state_id;
    } else if (stateExitActions[$.state.last_state_id][$.state.stateExitActionID].type === "sleep") {
        if (timer >= stateExitActions[$.state.last_state_id][$.state.stateExitActionID].value) {
            $.state.stateExitActionID += 1;
            timer = 0;
            if ($.state.stateExitActionID >= stateExitActions[$.state.last_state_id].length) {
                $.state.stateExitActionID = -1;
            }
        } else {
            timer += deltaTime;
        }
    }
}

function DuringState(deltaTime) {
    if (!duringStateActions[$.state.state_id] || !duringStateActions[$.state.state_id][$.state.duringStateActionID]) return;
    
    while ($.state.duringStateActionID < duringStateActions[$.state.state_id].length) {
        if (duringStateActions[$.state.state_id][$.state.duringStateActionID].type === "sleep") {
            $.state.duringStateActionID += 1;
        } else {
            duringStateActions[$.state.state_id][$.state.duringStateActionID].action(CONDITION);
            $.state.duringStateActionID += 1;
        }
    }
    $.state.duringStateActionID = 0;
}

function Start() {}
function Update(deltaTime) {}

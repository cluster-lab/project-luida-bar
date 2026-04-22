let timer = 0;
let CONDITION;
let PARTICIPANTS = [];

$.onStart(() => {
    $.state.last_state_id = 0;
    $.state.state_id = 0;
    $.state.stateEnterActionID = -1;
    $.state.stateExitActionID = -1;
    $.state.duringStateActionID = -1;
    Start();
})

$.onUpdate((deltaTime) => {
    // Refresh every frame so enter/during/exit actions (and helper functions
    // like SendHaptics) always see the current participant roster and condition.
    // Do not rely on OnStateEnter alone — $.state persists across script reloads,
    // so DuringState can resume without OnStateEnter re-running.
    CONDITION = $.groupState.currentCondition;
    PARTICIPANTS = [null].concat($.groupState.participants || []);

    if ($.getStateCompat("this", "state_exit", "boolean")) {
        $.state.stateExitActionID = 0;
        $.state.last_state_id = $.state.state_id;
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

function SetPosition(x, y, z) {
    try {
        $.setPosition(new Vector3(parseFloat(x), parseFloat(y), parseFloat(z)));
    } catch (e) {
        $.log(`Error in SetPosition: ${e}. Ensure MovableItem is present and x,y,z are valid numbers.`);
    }
}

function AddPosition(x, y, z) {
    try {
        $.setPosition($.getPosition().add(new Vector3(parseFloat(x), parseFloat(y), parseFloat(z))))
    } catch (e) {
        $.log(`Error in AddPosition: ${e}. Ensure MovableItem is present and x,y,z are valid numbers.`);
    }
}

function SetRotation(x, y, z) {
    try {
        $.setRotation(new Quaternion().setFromEulerAngles(new Vector3(parseFloat(x), parseFloat(y), parseFloat(z))));
    } catch (e) {
        $.log(`Error in SetRotation: ${e}. Ensure MovableItem is present and x,y,z are valid numbers (Euler degrees).`);
    }
}

function AddRotation(x, y, z) {
    try {
        $.setRotation($.getRotation().multiply(new Quaternion().setFromEulerAngles(new Vector3(parseFloat(x), parseFloat(y), parseFloat(z)))))
    } catch (e) {
        $.log(`Error in AddRotation: ${e}. Ensure MovableItem is present and x,y,z are valid numbers (Euler degrees).`);
    }
}

function ShowChild(childName) {
    $.subNode(childName).setEnabled(true);
}

function HideChild(childName) {
    $.subNode(childName).setEnabled(false);
}

function SetChildPosition(childName, x, y, z) {
    try {
        $.subNode(childName).setPosition(new Vector3(parseFloat(x), parseFloat(y), parseFloat(z)));
    } catch (e) {
        $.log(`Error in SetChildPosition: ${e}. Ensure MovableItem is present and x,y,z are valid numbers.`);
    }
}

function AddChildPosition(childName, x, y, z) {
    try {
        $.subNode(childName).setPosition($.subNode(childName).getPosition().add(new Vector3(parseFloat(x), parseFloat(y), parseFloat(z))))
    } catch (e) {
        $.log(`Error in AddChildPosition: ${e}. Ensure MovableItem is present and x,y,z are valid numbers.`);
    }
}

function SetChildRotation(childName, x, y, z) {
    try {
        $.subNode(childName).setRotation(new Quaternion().setFromEulerAngles(new Vector3(parseFloat(x), parseFloat(y), parseFloat(z))));
    } catch (e) {
        $.log(`Error in SetChildRotation: ${e}. Ensure MovableItem is present and x,y,z are valid numbers (Euler degrees).`);
    }
}

function AddChildRotation(childName, x, y, z) {
    try {
        $.subNode(childName).setRotation($.subNode(childName).getRotation().multiply(new Quaternion().setFromEulerAngles(new Vector3(parseFloat(x), parseFloat(y), parseFloat(z)))))
    } catch (e) {
        $.log(`Error in AddChildRotation: ${e}. Ensure MovableItem is present and x,y,z are valid numbers (Euler degrees).`);
    }
}

function ToNextState() {
    $.sendSignalCompat("this", "state_triggerTransition");
}

function SendDataToCollector(label, value) {
    if (!$.groupState.collectedData) $.groupState.collectedData = {};
    let collectedData = $.groupState.collectedData;
    collectedData[label] = value;
    $.groupState.collectedData = collectedData;
}

function ProcessAndSaveCollectedData() {
    $.sendSignalCompat("this", "exp_recordCustomData");
}

function UploadCollectedData() {
    $.sendSignalCompat("this", "exp_uploadCustomData");
}

function SetText(text) {
    try {
        $.subNode("Text").setText(`${text}`);
    } catch (e) {
        $.log(`Error in SetText: ${e}. Ensure a 'Text' sub-node exists and has setText method.`);
    }
}

function SendHaptics(participantId, target, frequency, amplitude, duration) {
    try {
        if (PARTICIPANTS[participantId]) {
            let hapticsTarget = target;
            if (typeof target === 'string') {
                const lowerTarget = target.toLowerCase();
                if (lowerTarget === '"left"' || lowerTarget === "'left'") {
                    hapticsTarget = "left";
                } else if (lowerTarget === '"right"' || lowerTarget === "'right'") {
                    hapticsTarget = "right";
                } else {
                    hapticsTarget = null;
                }
            }

            PARTICIPANTS[participantId].send("haptics", {
                target: hapticsTarget,
                frequency: parseFloat(frequency),
                amplitude: parseFloat(amplitude),
                duration: parseFloat(duration)
            });
        } else {
            $.log("SendHaptics: No player found nearby.");
        }
    } catch (e) {
        $.log(`Error in SendHaptics: ${e}`);
    }
}

function SendViaOsc(participantId, address, values) {
    PARTICIPANTS[participantId].send("sendOsc", { address, values });
}

function OnStateEnter(deltaTime) {
    if (!stateEnterActions[$.state.state_id] || $.state.stateEnterActionID >= stateEnterActions[$.state.state_id].length) return;
    
    while ($.state.stateEnterActionID < stateEnterActions[$.state.state_id].length && stateEnterActions[$.state.state_id][$.state.stateEnterActionID].type !== "sleep") {
        stateEnterActions[$.state.state_id][$.state.stateEnterActionID].action(deltaTime);
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
        stateExitActions[$.state.last_state_id][$.state.stateExitActionID].action(deltaTime);
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
            duringStateActions[$.state.state_id][$.state.duringStateActionID].action(deltaTime);
            $.state.duringStateActionID += 1;
        }
    }
    $.state.duringStateActionID = 0;
}

function Start() {}
function Update(deltaTime) {}

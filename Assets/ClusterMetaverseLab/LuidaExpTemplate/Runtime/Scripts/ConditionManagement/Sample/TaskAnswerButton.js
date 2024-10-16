$.onStart(() => {
    const conditionManager = $.worldItemReference("ConditionManager");
    if (conditionManager) {
        conditionManager.send("exp_conditionDependentObject", true);
    }
})

$.onUpdate(() => {
    if ($.getStateCompat("this", "exp_conditionChanged", "boolean")) {
        $.setStateCompat("this", "exp_conditionChanged", false);
        $.state.currentCondition = $.state.conditions[$.getStateCompat("global", "exp_conditionID", "integer")];
        onConditionChanged();
    }

    tick();
})

$.onReceive((messageType, arg, sender) => {
    if (messageType === "exp_updateConditions") {
        $.state.conditions = arg;
        $.setStateCompat("this", "exp_conditionChanged", true);
    }
})

const wordMeanings = ["R", "G", "B"];
const wordFontColors = ["B", "R", "G"];

// Execution when condition changed
function onConditionChanged () {}

// Real-time execution depending on current condition
function tick () {
    // e.g. if ($.state.currentCondition["color"] === "R") $.setStateCompat("this", "isEnabled", true);
}

$.onInteract(() => {
    const aID = $.getStateCompat("this", "answerID", "integer");
    if (!$.state.currentCondition) return;
    const q = $.state.currentCondition["question"];
    if ($.state.currentCondition["wordObject"] === (q === "fontColor" ? wordFontColors[aID] : wordMeanings[aID])) {
        $.sendSignalCompat("this", "exp_setAnswer");
    }
})
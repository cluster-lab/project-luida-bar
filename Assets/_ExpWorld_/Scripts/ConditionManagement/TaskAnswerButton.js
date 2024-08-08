$.onStart(() => {
    $.getItemsNear($.getPosition(), 0.1).forEach(item => {
        if (item.id === "5570182165721890090") { // ConditionManager
            item.send("exp_conditionDependentObject", true);
        }
    });
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

const wordColors = ["R", "G", "B"];
const wordMeanings = ["G", "B", "R"];

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
    if ($.state.currentCondition["word"] === (q === "font" ? wordColors[aID] : wordMeanings[aID])) {
        $.sendSignalCompat("this", "exp_setAnswer");
    }
})
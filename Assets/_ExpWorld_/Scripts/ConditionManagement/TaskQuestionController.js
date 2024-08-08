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

// Execution when condition changed
function onConditionChanged () {
    if (!$.state.currentCondition) return;
    let wordObject;
    for (var word of ["R", "G", "B"]) {
        for (var lang of ["ja", "en"]) {
            $.log(word + "_" + lang);
            wordObject = $.subNode(word + "_" + lang);
            wordObject?.setEnabled(word === $.state.currentCondition["word"] && lang === $.state.currentCondition["lang"]);
        }
    }
    for (var question of ["font", "meaning"]) {
        wordObject = $.subNode("question_" + question);
        wordObject?.setEnabled(question === $.state.currentCondition["question"]);
    }
}

// Real-time execution depending on current condition
function tick () {
    // e.g. if ($.state.currentCondition["color"] === "R") $.setStateCompat("this", "isEnabled", true);
}
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

function onConditionChanged () {
    if ($.state.currentCondition) {
        $.log($.state.currentCondition);
        $.subNode("Text").setText("Method (between subjects): " + $.state.currentCondition["method"] + ", Color (within subjects): " + $.state.currentCondition["color"] + ", Size (within subjects): " + $.state.currentCondition["size"]);
    }
}

// Real-time execution depending on current condition
function tick () {}
$.onStart(() => {
    $.getItemsNear($.getPosition(), 0.1).forEach(item => {
        item.send("exp_conditionDependentObject", true);
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

function onConditionChanged () {}

// Real-time execution depending on current condition
function tick () {
    // e.g. if ($.state.currentCondition["color"] === "R") $.setStateCompat("this", "isEnabled", true);
}
/* Don't edit the code below unless you are not accessing experiment conditions! */
$.onStart(() => {
    $.getItemsNear($.getPosition(), 0.1).forEach(item => {
        if (item.id === "5570182165721890090") { // ConditionManager
            item.send("exp_conditionDependentObject", true);
        }
    });
    init();
})

$.onUpdate((deltaTime) => {
    if ($.getStateCompat("this", "exp_conditionChanged", "boolean")) {
        $.setStateCompat("this", "exp_conditionChanged", false);
        $.state.currentCondition = $.state.conditions[$.getStateCompat("global", "exp_conditionID", "integer")];
        onConditionChanged();
    }
    tick(deltaTime);
})

$.onReceive((messageType, arg, sender) => {
    if (messageType === "exp_updateConditions") {
        $.state.conditions = arg;
        $.setStateCompat("this", "exp_conditionChanged", true);
    }
})
/* Don't edit the code above unless you are not accessing experiment conditions! */

// Execution when initialized
function init() {
    
}

// Execution on every frame
function tick (deltaTime) {
    // e.g. if ($.state.currentCondition["color"] === "R") $.state.timer = $.state.timer + deltaTime;
}

// Execution when condition changed
function onConditionChanged () {
    // e.g. if ($.state.currentCondition["color"] === "R") $.setStateCompat("this", "isEnabled", true);
}
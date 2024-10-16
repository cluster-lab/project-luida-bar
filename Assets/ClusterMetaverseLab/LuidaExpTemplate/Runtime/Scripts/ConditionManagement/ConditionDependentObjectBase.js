$.onStart(() => {
    const conditionManager = $.worldItemReference("ConditionManager");
    if (conditionManager) {
        conditionManager.send("exp_conditionDependentObject", true);
    }
    Awake();
})

$.onUpdate((deltaTime) => {
    if ($.getStateCompat("this", "exp_conditionChanged", "boolean")) {
        $.setStateCompat("this", "exp_conditionChanged", false);
        $.state.currentCondition = $.state.conditions[$.getStateCompat("global", "exp_conditionID", "integer")];
        OnConditionChanged();
    }
    Update(deltaTime);
})

$.onReceive((messageType, arg, sender) => {
    if (messageType === "exp_updateConditions") {
        $.state.conditions = arg;
        $.setStateCompat("this", "exp_conditionChanged", true);
    }
    OnReceive(messageType, arg, sender);
})

function Awake() {}

function Update (deltaTime) {}

function OnConditionChanged () {}

function OnReceive (messageType, arg, sender) {}
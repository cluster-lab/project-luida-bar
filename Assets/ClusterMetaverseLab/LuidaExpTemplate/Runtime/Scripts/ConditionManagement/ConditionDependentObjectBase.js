$.onStart(() => {
    const conditionManager = $.worldItemReference("ConditionManager");
    if (conditionManager) {
        conditionManager.send("exp_conditionDependentObject", true);
    }
    $.state.bsCond = {};
    $.state.wsVarNames = [];
    $.state.wsCondIndicesList = [];
    Awake();
})

$.onUpdate((deltaTime) => {
    if ($.getStateCompat("this", "exp_conditionChanged", "boolean")) {
        $.setStateCompat("this", "exp_conditionChanged", false);
        OnConditionChanged();
    }
    Update(deltaTime);
})

$.onReceive((messageType, arg, sender) => {
    if (messageType === "exp_updateConditions") {
        if (arg[bsCond]) $.state.bsCond = arg[bsCond];
        if (arg[wsVarNames]) $.state.wsVarNames = arg[wsVarNames];
        if (arg[wsCondIndicesList]) {
            if (!$.state.wsCondIndicesList) $.state.wsCondIndicesList = [];
            $.state.wsCondIndicesList = [ ...$.state.wsCondIndicesList, arg[wsCondIndicesList] ];
        }
        $.state.isConditionsUpdated = arg[isWsDone] || false;
        if (arg[isWsDone] && $.state.bsCond && $.state.wsVarNames && $.state.wsCondIndicesList) {
            $.setStateCompat("this", "exp_conditionChanged", true);
        }
        $.setStateCompat("this", "exp_conditionChanged", true);
    }
    OnReceive(messageType, arg, sender);
})

function getCondition(varName) {
    if ($.state.bsCond[varName]) return $.state.bsCond[varName];
    const currentCondIndices = $.state.wsCondIndicesList[$.getStateCompat("global", "exp_conditionID", "integer")]
    const varNameIndex = $.state.wsVarNames.indexOf(varName);
    return variables.v[varNameIndex][currentCondIndices[varNameIndex]];
}

function Awake() {}

function Update (deltaTime) {}

function OnConditionChanged () {}

function OnReceive (messageType, arg, sender) {}
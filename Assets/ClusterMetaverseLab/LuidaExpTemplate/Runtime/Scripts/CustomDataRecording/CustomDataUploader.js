$.onStart(() => {
    $.state.customData = {};
    const conditionManager = $.worldItemReference("ConditionManager");
    if (conditionManager) {
        conditionManager.send("exp_conditionDependentObject", true);
    }
    $.state.bsCond = {};
    $.state.wsVarNames = [];
    $.state.wsCondIndicesList = [];
})

$.onUpdate(() => {
    if ($.getStateCompat("this", "exp_conditionChanged", "boolean")) {
        $.setStateCompat("this", "exp_conditionChanged", false);
    }

    if ($.getStateCompat("this", "exp_recordCustomData", "boolean")) {
        $.setStateCompat("this", "exp_recordCustomData", false);
        recordData();
    }
    if ($.getStateCompat("this", "exp_uploadCustomData", "boolean")) {
        $.setStateCompat("this", "exp_uploadCustomData", false);
        uploadData();
    }
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
})

function getCondition(varName) {
    if ($.state.bsCond[varName]) return $.state.bsCond[varName];
    const currentCondIndices = $.state.wsCondIndicesList[$.getStateCompat("global", "exp_conditionID", "integer")]
    const varNameIndex = $.state.wsVarNames.indexOf(varName);
    return variables.v[varNameIndex][currentCondIndices[varNameIndex]];
}

function recordData () {
    $.state.customData = calculateData();
}

function uploadData () {
    $.log("Upload custom data: " + JSON.stringify($.state.customData));
    let request = {
        type: "uploadCustomData",
        token: token || "",
        dataByFileName: JSON.stringify($.state.customData),
        eID: expID || "",
        pID: $.getPlayersNear($.getPosition().clone(), Infinity)[0].idfc || "" // TODO: retrieve idfc through cluster Player Script
    };
    $.callExternal(JSON.stringify(request), "customDataUploaded");
}

$.onExternalCallEnd((res, meta, err) =>
{
    if (res == null) {
        $.log("callExternal ERROR: " + err);
        return;
    }

    if (meta === "customDataUploaded") {
        $.log("Custom recorded data uploaded");
    }
});
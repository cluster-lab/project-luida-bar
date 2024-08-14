$.onStart(() => {
    $.state.customData = {};
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
        $.state.conditions = arg;
        $.setStateCompat("this", "exp_conditionChanged", true);
    }
})

function recordData () {
    $.state.customData = calculateData();
}

function uploadData () {
    $.log("Upload custom data: " + JSON.stringify($.state.customData));
    let request = {
        type: "uploadCustomData",
        dataByFileName: JSON.stringify($.state.customData),
        eID: expID || "0",
        pID: $.getStateCompat("owner", "exp_pID", "integer") || 0 // TODO: retrieve pID through cluster Player Script
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
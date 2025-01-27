const uploadInterval = 1;
const dataLengthPerUpload = 15;

$.onStart(() => {
    $.state.customData = {};
    $.state.uploadIndex = 0;
    $.state.elapsedTime = 0;
    $.state.dataLength = 0;
    $.state.isUploading = false;
})

$.onUpdate((deltaTime) => {
    if ($.getStateCompat("this", "exp_recordCustomData", "boolean")) {
        $.setStateCompat("this", "exp_recordCustomData", false);
        recordData();
    }
    if ($.getStateCompat("this", "exp_uploadCustomData", "boolean")) {
        if ($.state.uploadIndex === 0) {
            uploadDataInit();
            $.setStateCompat("this", "exp_uploadCustomData", false);
        }
    }
    if ($.state.isUploading) {
        $.state.elapsedTime = $.state.elapsedTime + deltaTime;
        if ($.state.elapsedTime >= uploadInterval && $.state.uploadIndex < Math.ceil($.state.dataLength / dataLengthPerUpload)) {
            $.state.elapsedTime = 0;
            uploadDataStep();
        }
    }
})

function recordData () {
    $.state.customData = calculateData();
}

function uploadDataInit() {
    $.state.uploadIndex = 0;
    $.state.elapsedTime = 0;
    $.state.isUploading = true;
    let firstFileName = Object.keys($.state.customData)[0];
    if (firstFileName) {
        $.state.dataLength = $.state.customData[firstFileName].length;
    }
}

function uploadDataStep() {
    $.log($.state.uploadIndex);
    if ($.state.uploadIndex < Math.ceil($.state.dataLength / dataLengthPerUpload)) {
        const slicedData = Object.fromEntries(
            Object.entries($.state.customData).map(([key, value]) => [
                key,
                value.slice($.state.uploadIndex * dataLengthPerUpload, ($.state.uploadIndex + 1) * dataLengthPerUpload)
            ])
        );
        let request = {
            type: "uploadCustomData",
            token: token || "",
            data: slicedData,
            eID: expID || "",
            pID: $.getPlayersNear($.getPosition().clone(), Infinity)[0].idfc || "" // TODO: retrieve idfc through cluster Player Script
        };
        $.log(request);
        $.callExternal(JSON.stringify(request), "customDataUploaded");
        $.state.uploadIndex = $.state.uploadIndex + 1;

        if ($.state.uploadIndex >= Math.ceil($.state.dataLength / dataLengthPerUpload)) {
            $.state.uploadIndex = 0;
            $.state.isUploading = false;
        }
    }
}

$.onExternalCallEnd((res, meta, err) =>
{
    if (res == null) {
        $.log("callExternal ERROR: " + err);
        return;
    }

    if (meta === "customDataUploaded") {
        $.log("Response after customDataUploaded called: " + JSON.stringify(res));
    }
});
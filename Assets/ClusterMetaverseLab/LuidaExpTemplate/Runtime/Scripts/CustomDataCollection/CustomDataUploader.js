const uploadInterval = 1;
const dataLengthPerUpload = 100;
const dataByteLengthPerUpload = 100000; // 102400

$.onStart(() => {
    $.state.customData = [];
    $.state.uploadIndex = 0;
    $.state.elapsedTime = 0;
    // $.state.dataLength = 0;
    $.state.steps = 0;
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
        if ($.state.elapsedTime >= uploadInterval && $.state.uploadIndex < $.state.steps) {
            $.state.elapsedTime = 0;
            uploadDataStep();
        }
    }
})

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

function recordData () {
    $.state.customData = calculateData();
}

function uploadDataInit() {
    $.state.uploadIndex = 0;
    $.state.elapsedTime = 0;
    $.state.isUploading = true;

    let dataByteLength = utf8ByteLength(JSON.stringify($.state.customData));
    $.log("Data byte length: " + dataByteLength);
    $.state.steps = Math.ceil(dataByteLength / dataByteLengthPerUpload);
}

function uploadDataStep() {
    $.log("Upload data step: " + $.state.uploadIndex);
    if ($.state.uploadIndex < $.state.steps) {
        let request = {
            type: "uploadCustomData",
            token: token || "",
            data: { data: $.state.customData.slice($.state.uploadIndex * dataLengthPerUpload, ($.state.uploadIndex + 1) * dataLengthPerUpload) },
            eID: expID || "",
            sID: $.groupState.sessionID || ""
        };
        $.log(JSON.stringify(request));
        $.callExternal(
            new ExternalEndpointId(callExternalEndpointID),
            JSON.stringify(request),
            "customDataUploaded");
        $.state.uploadIndex = $.state.uploadIndex + 1;

        if ($.state.uploadIndex >= $.state.steps) {
            $.state.uploadIndex = 0;
            $.state.isUploading = false;
            $.state.customData = [];
        }
    }
}

function utf8ByteLength(str) {
    let bytes = 0;
    for (let i = 0; i < str.length; i++) {
        const code = str.charCodeAt(i);

        if (code <= 0x7F)                      bytes += 1; // U+0000 – U+007F
        else if (code <= 0x7FF)                bytes += 2; // U+0080 – U+07FF
        else if (code >= 0xD800 && code <= 0xDBFF) {
            // Surrogate pair (astral plane)
            bytes += 4;                          // Whole pair is 4 bytes
            i++;                                 // Skip the next surrogate
        }
        else                                   bytes += 3; // U+0800 – U+FFFF
    }
    return bytes;
}

function calculateData () {
    let returnData = $.state.customData;
    if (!Array.isArray(returnData)) returnData = [];
    const CONDITION = $.groupState.currentCondition;
    const PARTICIPANTS = [null].concat($.groupState.participants);
    const COLLECTED_DATA = $.groupState.collectedData;
    function saveData() {

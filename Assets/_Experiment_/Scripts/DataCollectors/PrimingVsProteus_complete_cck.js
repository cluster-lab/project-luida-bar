// === LUIDA AUTO-GENERATED (do not edit between markers) ===
// Regenerated from LuidaDataCollectorConfig — manual edits inside this block will be lost.
(function syncFromCck() {
    if (!$.groupState.collectedData) $.groupState.collectedData = {};
    const cd = $.groupState.collectedData;
    let changed = false;
    // Label: isElderVideo (Bool)
    {
        const v = $.getStateCompat("global", "luida_collect_isElderVideo", "boolean");
        if (v !== undefined && cd["isElderVideo"] !== v) {
            cd["isElderVideo"] = v; changed = true;
        }
    }
    // Label: isElderAvatar (Bool)
    {
        const v = $.getStateCompat("global", "luida_collect_isElderAvatar", "boolean");
        if (v !== undefined && cd["isElderAvatar"] !== v) {
            cd["isElderAvatar"] = v; changed = true;
        }
    }
    // Label: isSafeRoad (Bool)
    {
        const v = $.getStateCompat("global", "luida_collect_isSafeRoad", "boolean");
        if (v !== undefined && cd["isSafeRoad"] !== v) {
            cd["isSafeRoad"] = v; changed = true;
        }
    }
    if (changed) $.groupState.collectedData = cd;
})();
// Rebind COLLECTED_DATA so this saveData() call sees just-synced values.
const COLLECTED_DATA = $.groupState.collectedData;
// === LUIDA AUTO-GENERATED END ===

const fields = {
    video: ((COLLECTED_DATA["isElderVideo"] === true) ? "Elder" : "Young"),
    avatar: ((COLLECTED_DATA["isElderAvatar"] === true) ? "Elder" : "Young"),
    road: ((COLLECTED_DATA["isSafeRoad"] === true) ? "Safe" : "Danger"),
};
return fields;

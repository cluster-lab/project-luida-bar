// === LUIDA AUTO-GENERATED (do not edit between markers) ===
// Regenerated from LuidaDataCollectorConfig — manual edits inside this block will be lost.
(function syncFromCck() {
    if (!$.groupState.collectedData) $.groupState.collectedData = {};
    const cd = $.groupState.collectedData;
    let changed = false;
    // Label: label1 (Integer)
    {
        const v = $.getStateCompat("global", "luida_collect_label1", "integer");
        if (v !== undefined && cd["label1"] !== v) {
            cd["label1"] = v; changed = true;
        }
    }
    if (changed) $.groupState.collectedData = cd;
})();
// Rebind COLLECTED_DATA so this saveData() call sees just-synced values.
const COLLECTED_DATA = $.groupState.collectedData;
// === LUIDA AUTO-GENERATED END ===

const fields = {
};
return fields;

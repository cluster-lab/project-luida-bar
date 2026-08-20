// === LUIDA AUTO-GENERATED (do not edit between markers) ===
// Regenerated from LuidaDataCollectorConfig — manual edits inside this block will be lost.
(function syncFromCck() {
    if (!$.groupState.collectedData) $.groupState.collectedData = {};
    if (!$.groupState._luidaSig) $.groupState._luidaSig = {};
    const cd = $.groupState.collectedData;
    const sg = $.groupState._luidaSig;
    // The gimmick fires a per-label pulse Signal (luida_collect_<label>_w) on each push.
    // When its timestamp advances the gimmick just pushed THIS label -> write its value
    // (catches a re-push of the same constant). First sight writes only a non-default value,
    // so an unset key never overwrites a send value. `sg` is updated ONLY when we write, so a
    // send-only save doesn't consume a label's first-sight before its gimmick first pushes.
    // Label: time (Float)
    {
        const v = $.getStateCompat("global", "luida_collect_time", "float");
        const sig = $.getStateCompat("global", "luida_collect_time_w", "signal");
        const t = sig ? sig.getTime() : 0;
        if (sg["time"] === undefined ? (v !== 0) : (t !== sg["time"])) { cd["time"] = v; sg["time"] = t; }
    }
    // Label: isRed (Bool)
    {
        const v = $.getStateCompat("global", "luida_collect_isRed", "boolean");
        const sig = $.getStateCompat("global", "luida_collect_isRed_w", "signal");
        const t = sig ? sig.getTime() : 0;
        if (sg["isRed"] === undefined ? (v !== false) : (t !== sg["isRed"])) { cd["isRed"] = v; sg["isRed"] = t; }
    }
    $.groupState.collectedData = cd;
    $.groupState._luidaSig = sg;
})();
// Rebind COLLECTED_DATA so this saveData() call sees just-synced values.
const COLLECTED_DATA = $.groupState.collectedData;
// === LUIDA AUTO-GENERATED END ===

const fields = {
    // --- LUIDA AUTO: state machine log (auto-included while automation is active) ---
    stateLog: (typeof COLLECTED_DATA !== "undefined" && COLLECTED_DATA) ? COLLECTED_DATA["stateLog"] : undefined,
    // --- END LUIDA AUTO ---
    // --- LUIDA: collected data items (saved by default; toggle "Ignore on save" to exclude) ---
    time: COLLECTED_DATA["time"],
    isRed: COLLECTED_DATA["isRed"],
    ans: ((COLLECTED_DATA["isRed"] === true) ? "R" : "B"),
    font: CONDITION["font"],
    text: CONDITION["text"],
    req: CONDITION["request"],
    depth: CONDITION["depth"],
};
return fields;

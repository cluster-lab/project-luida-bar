// === LUIDA AUTO-GENERATED (do not edit between markers) ===
// Regenerated from LuidaDataCollectorConfig — manual edits inside this block will be lost.
// No CCK-eligible labels registered. Add labels in LUIDA → Configure data collector → Collected data items.
// === LUIDA AUTO-GENERATED END ===

const fields = {
    // --- LUIDA AUTO: state machine log (auto-included while automation is active) ---
    stateLog: (typeof COLLECTED_DATA !== "undefined" && COLLECTED_DATA) ? COLLECTED_DATA["stateLog"] : undefined,
    // --- END LUIDA AUTO ---
    avatar: CONDITION["avatar"],
    bridge: COLLECTED_DATA["bridge"],
};
return fields;

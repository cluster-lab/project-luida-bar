let stateId = 0;

$.onStart(() => {
    stateId = 0;
})

$.onUpdate(() => {
    if (stateId !== $.getStateCompat("global", "state_currentID", "integer")) {
        stateId = $.getStateCompat("global", "state_currentID", "integer");
        if (!$.groupState.collectedData) $.groupState.collectedData = {};
        let collectedData = $.groupState.collectedData;
        collectedData["stateLog"] = {
            id: stateId, // State's ID
            name: $.groupState.stateNames[stateId], // State's name
            startAt: Date.now(), // Timestamp of the state switching
        };
        $.groupState.collectedData = collectedData;
    }
})


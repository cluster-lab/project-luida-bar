$.onInteract(() => {
    const currentQuestID = $.getStateCompat("owner", "currentQuestID", "integer") + 1;
    if (currentQuestID && currentQuestID > 0) {
        $.setStateCompat("owner", "DropWorldGate" + currentQuestID, true);
    }
})
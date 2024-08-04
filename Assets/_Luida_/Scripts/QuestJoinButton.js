$.onInteract(() => {
    const currentQuestID = $.getStateCompat("owner", "currentQuestID", "integer") + 1;
    if (currentQuestID && currentQuestID > 0) {
        $.setStateCompat("owner", "DropWorldGate" + ("00" + currentQuestID).slice(-2), true);
    }
})
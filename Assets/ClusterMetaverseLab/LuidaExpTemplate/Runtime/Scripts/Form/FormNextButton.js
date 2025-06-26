$.onInteract(() => {
    $.worldItemReference("FormController").send("form_to_next", true);
})

$.onReceive((messageType, arg) => {
    switch (messageType) {
        case "setParticipant":
            $.requestOwner(arg);
            $.setVisiblePlayers([arg]);
            break;
    }
});

$.onInteract(() => {
    $.worldItemReference("FormController").send("form_to_prev", true);
})

$.onReceive((messageType, arg) => {
    switch (messageType) {
        case "setOwner":
            $.requestOwner(arg);
            $.setVisiblePlayers([arg]);
            break;
    }
});

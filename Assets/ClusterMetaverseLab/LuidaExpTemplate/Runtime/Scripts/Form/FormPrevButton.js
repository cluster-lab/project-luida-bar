$.onInteract(() => {
    $.worldItemReference("FormController").send("form_to_prev", true);
})

$.onReceive((messageType, arg) => {
    switch (messageType) {
        case "showButton":
            $.setVisiblePlayers([arg]);
            break;
    }
});

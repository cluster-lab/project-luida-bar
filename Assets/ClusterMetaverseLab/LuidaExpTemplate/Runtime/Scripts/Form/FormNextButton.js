$.onInteract(() => {
    $.log("Next button pressed by " + $.getOwner().idfc);
    $.worldItemReference("FormController").send("form_to_next", true);
})

$.onReceive((messageType, arg) => {
    switch (messageType) {
        case "showButton":
            $.setVisiblePlayers([arg]);
            break;
    }
});

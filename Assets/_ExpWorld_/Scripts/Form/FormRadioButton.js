$.onStart(() => {
    $.getItemsNear($.getPosition(), 0.1).forEach(item => {
        item.send("form_on_answer_option_spawned", true);
    });
})

$.onReceive((messageType, arg, sender) => {
    switch (messageType) {
        case "form_init_answer_option":
            $.state.answerID = arg["id"]
            $.subNode("Text").setText(arg["label"]);
            $.state.formController = sender;
            $.setStateCompat("this", "form_activate_answer_option", true);
            break;
        default:
            break;
    }
})

$.onInteract(player => {
    // TODO: show and move indicator through Player's state
    $.state.formController.send("form_send_answer_radio", $.state.answerID);
})
$.onStart(() => {
    $.state.destroyable = false;
    let owner = $.getOwner();
    $.setVisiblePlayers([owner]);
    $.setStateCompat("this", "show", true);
})

$.onUpdate(() => {
    if ($.getStateCompat("this", "form_try_answer", "boolean")) {
        $.setStateCompat("this", "form_try_answer", false);
        answer();
    }
    if ($.state.destroyable && $.getStateCompat("owner", "form_destroy_answer_option", "boolean")) {
        $.sendSignalCompat("this", "form_destroy_answer_option");
        $.state.destroyable = false;
    }
})

$.onReceive((messageType, arg, sender) => {
    switch (messageType) {
        case "form_init_answer_option":
            $.state.formController = sender;
            $.state.destroyable = true;
            $.state.formController.send("form_answer", $.getStateCompat("this", "form_toggle_on", "boolean"));
            // if (arg["value"]) $.state.answerValue = arg["value"]
            // if (arg["label"] && $.subNode("Text")) $.subNode("Text").setText(arg["label"]);
            break;
        // case "form_destroy_answer_option":
        //     $.setStateCompat("this", "form_destroy_answer_option", true);
        //     break;
        default:
            break;
    }
})

function answer() {
    $.state.formController.send("form_answer", $.getStateCompat("this", "form_toggle_on", "boolean"));
}
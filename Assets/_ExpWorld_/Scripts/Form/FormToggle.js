$.onStart(() => {
    $.state.answerValue = null;
    $.getItemsNear($.getPosition(), 0.1).forEach(item => {
        item.send("form_on_answer_option_spawned", true);
    });
})

$.onUpdate(() => {
    if ($.getStateCompat("this", "form_try_answer", "boolean")) {
        $.setStateCompat("this", "form_try_answer", false);
        answer();
    }
})

$.onReceive((messageType, arg, sender) => {
    switch (messageType) {
        case "form_init_answer_option":
            $.state.formController = sender;
            // if (arg["value"]) $.state.answerValue = arg["value"]
            // if (arg["label"] && $.subNode("Text")) $.subNode("Text").setText(arg["label"]);
            break;
        case "form_destroy_answer_option":
            $.setStateCompat("this", "form_destroy_answer_option", true);
            break;
        default:
            break;
    }
})

function answer() {
    $.state.answerValue = $.getStateCompat("this", "form_toggle_on", "boolean");
    $.state.formController.send("form_answer", $.state.answerValue);
}
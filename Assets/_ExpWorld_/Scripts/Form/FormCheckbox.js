$.onStart(() => {
    $.state.isOn = $.getStateCompat("this", "form_toggle_on", "boolean");
    $.getItemsNear($.getPosition(), 0.1).forEach(item => {
        item.send("form_on_answer_option_spawned", true);
    });
})

$.onUpdate(() => {
    if ($.getStateCompat("this", "form_try_check", "boolean")) {
        $.setStateCompat("this", "form_try_check", false);
        sendCheckboxValue();
    }
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

function sendCheckboxValue () {
    $.state.isOn = $.getStateCompat("this", "form_toggle_on", "boolean");
    $.state.formController.send($.state.isOn ? "form_send_answer_checked" : "form_send_answer_unchecked", $.state.answerID);
}
$.onStart(() => {
    $.state.isOn = $.getStateCompat("this", "form_toggle_on", "boolean");
    $.state.answerValue = null;
    $.state.destroyable = false;
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
            if (arg["value"]) $.state.answerValue = arg["value"]

            $.setVisiblePlayers([arg["participant"]]);
            $.setStateCompat("this", "show", true);
            $.state.destroyable = true;
            break;
        case "form_destroy_answer_option":
            $.sendSignalCompat("this", "form_destroy_answer_option");
            break;
        default:
            break;
    }
})

function answer() {
    $.state.isOn = $.getStateCompat("this", "form_toggle_on", "boolean");
    $.state.formController.send("form_answer", { value: $.state.answerValue, isOn: $.state.isOn } );
}
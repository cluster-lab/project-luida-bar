/*
    Sth like a parent class for any form answer option UI to inherit (despite no implementation of 'class' in CCK script)
*/

$.onStart(() => {
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
    }
})

$.onReceive((messageType, arg, sender) => {
    switch (messageType) {
        case "form_init_answer_option":
            $.setStateCompat("owner", "form_destroy_answer_option", false);
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
    $.state.formController.send("form_answer", $.state.answerValue);
}
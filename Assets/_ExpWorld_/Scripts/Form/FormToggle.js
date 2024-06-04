$.onStart(() => {
    $.state.isOn = $.getStateCompat("this", "form_toggle_on", "boolean");
})

$.onUpdate(() => {
    if ($.getStateCompat("this", "form_try_toggle", "boolean")) {
        $.setStateCompat("this", "form_try_toggle", false);
        sendToggleValue();
    }
})

function sendToggleValue () {
    $.state.isOn = $.getStateCompat("this", "form_toggle_on", "boolean");
    $.getItemsNear($.getPosition(), 0.1).forEach(item => {
        item.send("form_send_answer_toggled", $.state.isOn);
    });
}
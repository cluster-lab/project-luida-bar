$.onStart(() => {
    $.state.isChecked = $.getStateCompat("this", "q_toggle", "boolean");
})

$.onUpdate(() => {
    if ($.getStateCompat("this", "q_try_check", "boolean")) {
        $.setStateCompat("this", "q_try_check", false);
        sendCheckboxValue();
    }
})

function sendCheckboxValue () {
    $.state.checkboxID = $.getStateCompat("this", "q_checkbox_id", "integer");
    $.state.isChecked = $.getStateCompat("this", "q_toggle", "boolean");
    $.getItemsNear($.getPosition(), 0.1).forEach(item => {
        item.send($.state.isChecked ? "q_send_checked" : "q_send_unchecked", $.state.checkboxID);
    });
}
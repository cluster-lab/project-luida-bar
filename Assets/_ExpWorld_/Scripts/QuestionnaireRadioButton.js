$.onUpdate(() => {
    if ($.getStateCompat("this", "q_try_move_indicator", "boolean")) {
        $.setStateCompat("this", "q_try_move_indicator", false);
        $.setStateCompat("owner", "q_move_indicator", $.getPosition().clone());
    }
})
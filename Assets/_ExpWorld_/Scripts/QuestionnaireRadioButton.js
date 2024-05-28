$.onUpdate(() => {
    if ($.getStateCompat("this", "try_move_indicator", "boolean")) {
        $.setStateCompat("this", "try_move_indicator", false);
        $.setStateCompat("owner", "move_indicator", $.getPosition().clone());
    }
})
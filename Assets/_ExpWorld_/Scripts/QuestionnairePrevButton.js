$.onInteract(() => {
    $.getItemsNear($.getPosition(), 0.1).forEach(item => {
        item.send("q_prev_question", true);
    });
})
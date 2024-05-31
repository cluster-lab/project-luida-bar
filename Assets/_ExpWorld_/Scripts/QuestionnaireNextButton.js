$.onInteract(() => {
    $.getItemsNear($.getPosition(), 0.1).forEach(item => {
        item.send("q_save_and_next_question", true);
    });
})
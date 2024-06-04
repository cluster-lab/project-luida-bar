$.onInteract(() => {
    $.getItemsNear($.getPosition(), 0.1).forEach(item => {
        item.send("form_save_and_to_next", true);
    });
})
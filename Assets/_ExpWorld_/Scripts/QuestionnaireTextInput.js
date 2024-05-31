$.onInteract(player => {
    player.requestTextInput("q_request_text_answer", "Hi, what is your name?");
})

$.onTextInput((text, meta, status) => {
    if (meta !== "q_request_text_answer") return;
    switch(status) {
      case TextInputStatus.Success:
        $.getItemsNear($.getPosition(), 0.1).forEach(item => {
            item.send("q_send_text_answer", text);
        });
        $.subNode("Text").setText(text);
        break;
      case TextInputStatus.Busy:
        // 5秒後にretryする
        $.state.should_retry = true;
        $.state.retry_timer = 5;
        break;
      case TextInputStatus.Refused:
        // 拒否された場合は諦める
        $.state.should_retry = false;
        break;
    }
});
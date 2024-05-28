$.onInteract(player => {
    player.requestTextInput("request_questionnaire_answer", "Hi, what is your name?");
})

$.onTextInput((text, meta, status) => {
    if (meta !== "request_questionnaire_answer") return;
    switch(status) {
      case TextInputStatus.Success:
        $.getItemsNear($.getPosition(), 0.1).forEach(item => {
            item.send("send_questionnaire_answer", text);
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
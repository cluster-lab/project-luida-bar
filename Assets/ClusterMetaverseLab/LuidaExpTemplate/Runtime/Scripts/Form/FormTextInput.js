$.onStart(() => {
  $.state.answerValue = null;
  $.state.destroyable = false;
})

$.onUpdate(() => {
    // if ($.getStateCompat("this", "form_try_answer", "boolean")) {
    //     $.setStateCompat("this", "form_try_answer", false);
    //     answer();
    // }
    if ($.state.destroyable && $.getStateCompat("owner", "form_destroy_answer_option", "boolean")) {
        $.destroy();
    }
})

$.onReceive((messageType, arg, sender) => {
  switch (messageType) {
      case "form_init_answer_option":
        $.state.formController = sender;
        $.state.destroyable = true;
        // if (arg["value"]) $.state.answerValue = arg["value"]
        // if (arg["label"] && $.subNode("Text")) $.subNode("Text").setText(arg["label"]);
            if (arg["player"]) $.setVisiblePlayers([arg["player"]]);
        break;
      default:
          break;
  }
})

function answer() {
  $.state.formController.send("form_answer", $.state.answerValue);
}

$.onInteract(player => {
    player.requestTextInput("form_request_text_input", "回答を入力してください");
})

$.onTextInput((text, meta, status) => {
    if (meta !== "form_request_text_input") return;
    switch(status) {
      case TextInputStatus.Success:
        $.state.answerValue = text;
        $.subNode("Text").setText(text);
        answer();
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
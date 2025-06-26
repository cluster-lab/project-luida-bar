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
      $.sendSignalCompat("this", "form_destroy_answer_option");
      $.state.destroyable = false;
    }
})

$.onReceive((messageType, arg, sender) => {
  switch (messageType) {
      case "form_init_answer_option":
        $.state.formController = sender;
        $.state.player = arg["participant"];
        $.setVisiblePlayers([arg["participant"]]);
        $.setStateCompat("this", "show", true);
        $.state.destroyable = true;
        break;
      case "form_destroy_answer_option":
          $.sendSignalCompat("this", "form_destroy_answer_option");
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
        $.state.player?.send("setQuestionnaireUI", { n: "AnsText", t: text });
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
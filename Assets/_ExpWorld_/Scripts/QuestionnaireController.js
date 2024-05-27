$.onStart(() => {
    reset();
})

$.onUpdate(() => {
    $.state.answerType = $.getStateCompat("owner", "questionnaire_answer_type", "integer");
})

$.onInteract(() => {
    try {
        switch ($.state.answerType) {
            case 0:
                const answer = $.getStateCompat("owner", "questionnaire_answer", "integer");
                $.log(answer);
                $.state.answers[$.state.questionID] = answer;
                break;
            case 1:
                $.log($.state.answers[$.state.questionID]);
                break;
            case 2:
                $.getStateCompat("owner", "questionnaire_answer", "boolean");
                $.log(answer);
                $.state.answers[$.state.questionID] = answer;
                break;
            case 3:
                break;
            default:
                break;
        }
        $.log($.state.answers);
        $.state.questionID++;
    } catch (error) {
        $.log(error);
    }
});

$.onTextInput((text, meta, status) => {
    switch(status) {
      case TextInputStatus.Success:
        $.log(text);
        $.state.answers[$.state.questionID] = answer;
        $.state.answerType = 1;
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

function reset () {
    $.state.answers = {};
    $.state.questionID = 0;
    $.state.answerType = -1; // 0: integer, 1: str, 2: boolean, 3: integer[]
}
$.onStart(() => {
    reset();
})

$.onUpdate(() => {
    
})

$.onInteract(() => {
    $.state.answerType = $.getStateCompat("this", "questionnaire_answer_type", "integer");
    let answer = null;
    try {
        switch ($.state.answerType) {
            case 0:
                answer = $.getStateCompat("this", "questionnaire_answer", "integer");
                break;
            case 1:
                answer = $.state.tmpAnswer;
                break;
            case 2:
                answer = $.getStateCompat("this", "questionnaire_answer", "boolean");
                break;
            case 3:
                break;
            default:
                break;
        }
        
        if (answer) $.state.answers = { ...$.state.answers, [$.state.questionID]: answer};
        $.log("Q" + $.state.questionID + ": " + $.state.answers[$.state.questionID]);

        $.state.questionID++;
        $.state.tmpAnswer = null;
        $.setStateCompat("owner", "questionnaire_indicator_active", false);
    } catch (error) {
        $.log(error);
    }
});

$.onReceive((messageType, arg, sender) => {
    if (messageType !== "send_questionnaire_answer") return;
    
    $.state.tmpAnswer = arg;
    $.setStateCompat("this", "questionnaire_answer_type", 1);
})

function reset () {
    $.state.answers = {};
    $.state.questionID = 0;
    $.state.answerType = -1; // 0: integer, 1: str, 2: boolean, 3: integer[]
    $.state.tmpAnswer = null;
    $.setStateCompat("owner", "questionnaire_indicator_active", false);
}
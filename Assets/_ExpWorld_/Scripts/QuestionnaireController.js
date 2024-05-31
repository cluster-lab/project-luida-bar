/*
[$.state.answerType]
0: radio buttons / linear scale (integer)
1: text (string)
2: toggle (boolean)
3: checkboxes (integer[])
*/

$.onStart(() => {
    reset();
    $.setStateCompat("owner", "q_trigger_question_" + $.state.questionID, true);
})

$.onUpdate(() => {
    if ($.getStateCompat("owner", "q_is_next", "boolean")) {
        $.setStateCompat("owner", "q_is_next", false)
        saveAnswer();
    }
    if ($.getStateCompat("owner", "q_is_previous", "boolean")) {
        $.setStateCompat("owner", "q_is_previous", false)
        // TODO: move to prev question
    }
})

$.onInteract(() => {
    saveAnswer();
});

$.onReceive((messageType, arg, sender) => {
    switch (messageType) {
        case "q_send_text_answer":
            $.state.tmpAnswer = arg;
            $.setStateCompat("owner", "q_answer_type", 1);
            break;
        case "q_send_checked":
            if (!Array.isArray($.state.tmpAnswer)) $.state.tmpAnswer = [];
            if (!$.state.tmpAnswer.includes(arg)) $.state.tmpAnswer = [ ...$.state.tmpAnswer, arg ];
            $.setStateCompat("owner", "q_answer_type", 3);
            break;
        case "q_send_unchecked":
            if (!Array.isArray($.state.tmpAnswer)) $.state.tmpAnswer = [];
            $.state.tmpAnswer = $.state.tmpAnswer.filter(item => {
                return item !== arg;
            });
            $.setStateCompat("owner", "q_answer_type", 3);
            break;
        default:
            break;
    }
})

function reset () {
    $.state.answers = {};
    $.state.questionID = 1;
    $.state.answerType = -1; // 0: integer, 1: str, 2: boolean, 3: integer[]
    $.state.tmpAnswer = null;
    $.setStateCompat("owner", "q_indicator_active", false);
}

function saveAnswer () {
    $.state.answerType = $.getStateCompat("owner", "q_answer_type", "integer");
    let answer = null;
    switch ($.state.answerType) {
        case 0: // radio buttons / linear scale
            answer = $.getStateCompat("owner", "q_answer", "integer");
            if (!answer) return;
            break;
        case 1: // text
            answer = $.state.tmpAnswer; // string
            if (!answer) return;
            break;
        case 2: // toggle
            answer = $.getStateCompat("owner", "q_answer", "boolean");
            break;
        case 3: // checkboxes
            answer = $.state.tmpAnswer.sort() || []; // integer[]
            if (!answer) return;
            break;
        default:
            break;
    }
    
    $.state.answers = { ...$.state.answers, [$.state.questionID]: answer };
    $.log(JSON.stringify($.state.answers));

    toNext();
}

function toNext () {
    $.setStateCompat("owner", "q_trigger_question_" + $.state.questionID, false);
    $.state.questionID++;
    $.state.tmpAnswer = null;
    $.setStateCompat("owner", "q_trigger_indicator", false);
    $.setStateCompat("owner", "q_trigger_question_" + $.state.questionID, true);
}
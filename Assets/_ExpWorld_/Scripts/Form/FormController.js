// TODO: copy and modify QuestionnaireController to here

const attrsByQuestionType = [
    { questionType: "Options (single answer)",      answerType: "radio",    aTemplateName: "radio_button" },
    { questionType: "Linear scale",                 answerType: "radio",    aTemplateName: "scale_button" },
    { questionType: "Options (multiple answers)",   answerType: "check", aTemplateName: "checkbox" },
    { questionType: "Toggle",                       answerType: "toggle",   aTemplateName: "toggle" },
    { questionType: "Text",                         answerType: "text",     aTemplateName: "text_input" },
]

function initQuestion (title, description, answerOptions, questionTypeID) { // options: array of string
    $.subNode("Title").setText(title);
    $.subNode("Description").setText(description);
    $.state.answerOptions = answerOptions;
    $.state.questionTypeID = questionTypeID;
    $.state.spawningAnswerID = 0;
    spawnAnswerOption();
}

function setAnswerOptionSpawnPoint () {
    // TODO: calculate & set spawn point position only when answerType === "radio" or "check"
}

function spawnAnswerOption () {
    setAnswerOptionSpawnPoint();
    // Spawn for "radio_button" and "scale_button" and "checkbox"; Set GameObject Active for "toggle" and "text_input"
    $.setStateCompat("this", "form_spawn_" + attrsByQuestionType[$.state.questionTypeID].aTemplateName, true);
}

$.onReceive((messageType, arg, sender) => {
    switch (messageType) {
        case "form_on_answer_option_spawned":
            if (arg && $.state.answerOptions) {
                sender.send("form_init_answer_option", { id: $.state.spawningAnswerID + 1, label: $.state.answerOptions[$.state.spawningAnswerID] });
                if ($.state.spawningAnswerID < $.state.answerOptions.length - 1) {
                    $.state.spawningAnswerID = $.state.spawningAnswerID + 1;
                    spawnAnswerOption();
                }
            }
            break;
        default:
            break;
    }
})
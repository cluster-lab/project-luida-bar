const dummyQuestions = [
    { title: "どのくらいの頻度でVRを体験していますか？", description: "どのくらいの頻度でVRを体験していますか？", questionTypeID: 0, answerOptions: ["なし", "年に一回未満", "年に一回以上", "月に1~2回くらい", "週1回くらい", "週2~3回くらい", "週4回以上"] },
    { title: "VRについて興味ありますか？", description: "1:全く興味ない、7:とても興味ある", questionTypeID: 1, answerOptions: [1, 2, 3, 4, 5, 6, 7] },
    { title: "VRデバイスのメーカーとして認識している会社を選んでください", description: "会社として聞いたことはあるが、VRメーカーであることは知っていなければ、選択しないでください", questionTypeID: 2, answerOptions: ["Meta (Oculus)", "Vive", "Valve", "Sony", "Pico", "DPVR"] },
    { title: "10歳以上ですか？", description: "10歳未満の方はVRデバイスの使用をお控えください", questionTypeID: 3, answerOptions: [] },
    { title: "コメント", description: "何かコメントがあればどうぞ！", questionTypeID: 4, answerOptions: [] }
]

const answerOptionUISpawnCenter = new Vector3(0, -0.5, 0);

const attrsByQuestionType = [
    { questionType: "Options (single answer)",      answerType: "radio",    aTemplateName: "radio_button" },
    { questionType: "Linear scale",                 answerType: "radio",    aTemplateName: "scale_button" },
    { questionType: "Options (multiple answers)",   answerType: "check",    aTemplateName: "checkbox" },
    { questionType: "Toggle",                       answerType: "toggle",   aTemplateName: "toggle" },
    { questionType: "Text",                         answerType: "text",     aTemplateName: "text_input" },
]

// $.onStart(() => {
//     $.state.qID = 0;
//     initQuestion(dummyQuestions[$.state.qID]);
// })

$.onUpdate(() => {
    if ($.getStateCompat("this", "form_init", "boolean")) {
        $.setStateCompat("this", "form_init", false);
        $.state.qID = 0;
        initQuestion(dummyQuestions[$.state.qID]);
    }
})

function initQuestion (q) { // options: array of string
    $.subNode("Title").setText(q.title);
    $.subNode("Description").setText(q.description);
    $.state.questionTypeID = q.questionTypeID;
    $.state.answerOptions = q.answerOptions;
    $.state.spawningAnswerID = 0;
    $.state.answerOptionUIs = [];
    spawnAnswerOptionUI();
}

function setAnswerOptionUISpawnPoint () {
    // TODO: calculate & set spawn point position only when answerType === "radio" or "check"
    let pos = $.getPosition().clone().add(answerOptionUISpawnCenter);
    let rot = $.getRotation().clone();
    switch ($.state.questionTypeID) {
        case 0:
        case 2:
            // max 5 options (rows) per column
            // 1 columns: x = -0.1 - 0.3 * ((chars - 1) / 7), textsize = 0.05, max 30 chars per text line, 1~2 lines
            // 2 columns: x = -0.5 - 0.065 * (chars - 1) & 0.2, textsize = 0.04, 15 chars per text line, 1~2 lines; textsize = 0.03 for 3 lines
            // > 3 columns: x = -1.5 + 0.1 + (3 / columnCnt) * columnID, textsize = 0.05 - chars * (0.025/(max chars per line)), max chars per line = 3->15, 4->10, 5->6, 6->4 = (60/columnCnt - 5)
            break;
        case 1:
            pos = pos.add(new Vector3((($.state.answerOptions.length > 11 ? 3 : 2) / ($.state.answerOptions.length - 1)) * ($.state.spawningAnswerID - ($.state.answerOptions.length - 1) / 2), 0, 0));
            break;
        case 3:
        case 4:
            break;
        default:
            break;
    }
    // TODO: set pos and rot for spawn point
}

function spawnAnswerOptionUI () {
    setAnswerOptionUISpawnPoint();
    $.sendSignalCompat("this", "form_spawn_" + attrsByQuestionType[$.state.questionTypeID].aTemplateName);
}

$.onReceive((messageType, arg, sender) => {
    switch (messageType) {
        case "form_on_answer_option_spawned":
            if (arg && $.state.answerOptions) {
                $.state.answerOptionUIs = [ ...$.state.answerOptionUIs, sender ];
                sender.send("form_init_answer_option", { id: $.state.spawningAnswerID + 1, label: $.state.answerOptions[$.state.spawningAnswerID] });
                if ($.state.spawningAnswerID < $.state.answerOptions.length - 1) {
                    $.state.spawningAnswerID = $.state.spawningAnswerID + 1;
                    spawnAnswerOptionUI();
                }
            }
            break;
        case "form_answer":
            switch ($.state.questionTypeID) {
                case 0: // radio buttons
                case 1: // linear scale
                case 3: // toggle
                case 4: // text
                    $.state.tmpAnswer = arg;
                    break;
                case 2: // checkboxes
                    if (!Array.isArray($.state.tmpAnswer)) $.state.tmpAnswer = [];
                    if (arg.isOn) {
                        if (!$.state.tmpAnswer.includes(arg.value)) $.state.tmpAnswer = [ ...$.state.tmpAnswer, arg.value ];
                    } else {
                        if ($.state.tmpAnswer.includes(arg.value)) {
                            $.state.tmpAnswer = $.state.tmpAnswer.filter(item => {
                                return item !== arg.value;
                            });
                        }
                    }
                    break;
                default:
                    break;
            }
            break;
        case "form_to_next":
            saveAnswer();
            break;
        case "form_to_prev":
            toPrev();
            break;
        default:
            break;
    }
})

function destroyAnswerOptionUIs () {
    $.state.answerOptionUIs.forEach(optionUI => {
        optionUI.send("form_destroy_answer_option", true);
    });
    $.state.answerOptionUIs = [];
}

function saveAnswer () {
    $.state.answers = { ...$.state.answers, [$.state.questionID]: $.state.tmpAnswer };
    $.log("Update answers: " + JSON.stringify($.state.answers));
    toNext();
}

function submitAnswers () {
    // TODO: Send $.state.answers to DB
    $.log("Send final answers: " + JSON.stringify($.state.answers));
}

function toNext () {
    $.state.qID = $.state.qID + 1;
    if ($.state.qID >= dummyQuestions.length) {
        submitAnswers();
    } else {
        destroyAnswerOptionUIs();
        $.state.tmpAnswer = null;
        // TODO: hide selection indicator
        initQuestion(dummyQuestions[$.state.qID]);
    }
}

function toPrev () {
    if ($.state.qID <= 0) return;
    destroyAnswerOptionUIs();
    $.state.qID = $.state.qID - 1;
    $.state.tmpAnswer = null;
    // TODO: hide selection indicator
    initQuestion(dummyQuestions[$.state.qID]);
}

function reset () {
    $.state.answers = {};
    $.state.qID = 0;
    $.state.answerType = -1;
    $.state.tmpAnswer = null;
    // TODO: hide selection indicator
}
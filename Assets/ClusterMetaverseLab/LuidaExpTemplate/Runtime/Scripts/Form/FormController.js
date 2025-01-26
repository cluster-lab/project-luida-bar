$.onStart(() => {
    reset();
});

$.onUpdate((deltaTime) => {
    // Ensure that the question is only initialized after all currently displayed answer options are destroyed
    if (!$.state.isInitiated && $.getStateCompat("this", "form_set_content_active", "boolean")) {
        $.state.isInitiated = true;
        $.state.qID = 0;
        $.subNode("LoadingHint").setEnabled(true);

        // Reintroduced callExternal to get questions
        const questionnaireID = $.getStateCompat("this", "qID", "integer");
        if (questionnaireID !== -1) {
            let request = { type: "questions", token: token || "", eID: expID || "", qID: questionnaireID };
            $.callExternal(JSON.stringify(request), "getQuestions");
        }
    }

    if ($.state.tryInitQuestion && !$.state.answerOptionUIs.some(ans => ans.exists())) {
        initQuestion();
    }

    // Timer to trigger batch generation of answer options
    $.state.timer = ($.state.timer || 0) + deltaTime;
    if ($.state.timer > 0.2 && $.state.pendingAnswerOptions && $.state.pendingAnswerOptions.length > 0) {
        spawnNextAnswerOption();
        $.state.timer = 0; // Reset the timer after generating a batch
    }
});

$.onInteract(() => {
    $.setStateCompat("this", "form_set_content_active", true);
    $.setStateCompat("this", "form_set_start_hint_active", false);
});

$.onReceive((messageType, arg) => {
    if (!$.state.isInitiated) return;
    switch (messageType) {
        case "form_answer":
            handleFormAnswer(arg);
            break;
        case "form_to_next":
            saveAnswer();
            break;
        case "form_to_prev":
            toPrev();
            break;
    }
});

function tryInitQuestion() {
    $.state.tryInitQuestion = true;
}

function initQuestion() {
    // Prepare to stop destruction of answer options and initialize a new question
    $.sendSignalCompat("this", "form_stop_destroy_answer_option");
    $.state.tryInitQuestion = false;
    $.state.answerOptionUIs = [];
    $.state.answerOptionLocalPositions = [];
    const q = $.state.questions[$.state.qID];
    $.subNode("Title").setText(splitTextByWidth(q.t, 50));
    $.subNode("Description").setText(splitTextByWidth(q.d, 100));
    $.state.questionTypeID = q.i;
    $.state.answerOptions = Array.isArray(q.a)
        ? q.a
        : (typeof q.a === "string" ? q.a.split(",") : []) ;
    spawnAnswerOptionUI();
}

function spawnAnswerOptionUI() {
    $.state.pendingAnswerOptions = $.state.answerOptions.slice();
    $.state.answerOptionIndex = 0;
}

function addAnswerOption (id, localPos, rot, ansId) {
    const itemHandle = $.createItem(id, $.getPosition().clone().add(localPos.clone().applyQuaternion(rot)), rot);
    $.state.answerOptionUIs = [...$.state.answerOptionUIs, itemHandle];
    $.state.answerOptionLocalPositions = [...$.state.answerOptionLocalPositions, localPos];
    itemHandle.send("form_init_answer_option", { value: ansId + 1, label: $.state.answerOptions[ansId] });
}

function spawnNextAnswerOption() {
    const n = $.state.pendingAnswerOptions.length || 0;
    if ($.state.answerOptionIndex >= n) {
        $.state.pendingAnswerOptions = [];
        return;
    }

    let rotOffset = $.getRotation().clone();
    let answerUiId = "";

    // Maximum number of rows (per column)
    const maxRows = 5;
    const batchSize = 3; // Number of options to generate in each batch
    const numColumns = Math.ceil(n / maxRows); // Calculate number of columns based on total options and max rows per column

    // Adjust x-position based on number of columns to center them
    const totalWidth = numColumns * 0.5; // Adjust total width depending on your frame size
    let i = 0;

    for (i = 0; i < batchSize && $.state.answerOptionIndex < n; i++) {
        let columnIndex = Math.floor($.state.answerOptionIndex / maxRows);
        let rowIndex = $.state.answerOptionIndex % maxRows;

        // Calculate dynamic x and y positions
        let x = (columnIndex - (numColumns - 1) / 2) - 0.2; // Adjust 0.5 for spacing between columns
        let y = ((maxRows - 1) / 2 - rowIndex) * 0.2 - 0.1; // 0.2 is the vertical spacing between rows

        switch ($.state.questionTypeID) {
            case 0: // Radio Buttons (single answer)
            case 2: // Checkbox (multiple answers)
                answerUiId = new WorldItemTemplateId($.state.questionTypeID === 0
                    ? "answer-option-radio-button"
                    : "answer-option-checkbox");

                // Add the option at calculated x and y positions
                addAnswerOption(answerUiId, new Vector3(x, y, 0), rotOffset, $.state.answerOptionIndex);
                break;

            case 1: // Linear Scale
                answerUiId = new WorldItemTemplateId("answer-option-scale-button");

                let scaleX = ((n > 11 ? 3 : 2) / (n - 1)) * ($.state.answerOptionIndex - (n - 1) / 2);
                addAnswerOption(answerUiId, new Vector3(scaleX, 0, 0), rotOffset, $.state.answerOptionIndex);
                break;

            case 3: // Toggle
                answerUiId = new WorldItemTemplateId("answer-option-toggle");
                addAnswerOption(answerUiId, new Vector3(0, 0, 0), rotOffset, $.state.answerOptionIndex);
                break;

            case 4: // Text Input
                answerUiId = new WorldItemTemplateId("answer-option-text-input");
                addAnswerOption(answerUiId, new Vector3(0, 0, 0), rotOffset, $.state.answerOptionIndex);
                break;

            default:
                break;
        }

        $.state.answerOptionIndex += 1;
    }
}

function destroyAnswerOptionUIs() {
    // Send a signal to destroy the current answer option UI elements
    $.sendSignalCompat("this", "form_destroy_answer_option");
}

function handleFormAnswer(arg) {
    let posOffset = $.getPosition().clone();
    switch ($.state.questionTypeID) {
        case 0: // Radio Buttons
        case 1: // Linear Scale
            $.subNode("RadioButtonIndicator").setEnabled(true);
            $.subNode("RadioButtonIndicator").setPosition($.state.answerOptionLocalPositions[arg - 1].clone());
        case 3: // Toggle
        case 4: // Text Input
            $.state.tmpAnswer = arg;
            break;
        case 2: // Checkbox
            if (!Array.isArray($.state.tmpAnswer)) $.state.tmpAnswer = [];
            if (arg.isOn) {
                if (!$.state.tmpAnswer.includes(arg.value)) $.state.tmpAnswer = [...$.state.tmpAnswer, arg.value];
            } else {
                $.state.tmpAnswer = $.state.tmpAnswer.filter(item => item !== arg.value);
            }
            break;
    }
}

function saveAnswer() {
    if (!$.state.questions[$.state.qID]) return;
    if ($.state.questions[$.state.qID].r && (!$.state.tmpAnswer && $.state.tmpAnswer !== false)) return;
    let answers = [ ...$.state.answers ];
    answers[$.state.qID] = $.state.tmpAnswer;
    $.state.answers = answers;
    toNext();
}

function submitAnswers() {
    $.log("Send final answers: " + JSON.stringify($.state.answers));
    let request = {
        type: "questionAnswers",
        token: token || "",
        eID: expID || "",
        qID: $.getStateCompat("this", "qID", "integer").toString() || "1",
        pID: $.getPlayersNear($.getPosition().clone(), 100)[0].idfc || "", // TODO: retrieve idfc through cluster Player Script
        // pRole: "",
        answers: $.state.answers
    };
    const conditionManager = $.worldItemReference("ConditionManager");
    if (conditionManager) {
        conditionManager.send("exp_questionnaire_answer", $.state.answers);
    }
    $.callExternal(JSON.stringify(request), "postQuestionAnswers");
    $.setStateCompat("this", "form_set_content_active", false);
    reset(false);
}

function toNext() {
    destroyAnswerOptionUIs(); // Ensure previous UI elements are destroyed
    $.state.qID = $.state.qID + 1;
    $.subNode("RadioButtonIndicator").setEnabled(false);
    if ($.state.qID >= $.state.questions.length) {
        submitAnswers();
    } else {
        $.state.tmpAnswer = null;
        tryInitQuestion();
    }
}

function toPrev() {
    if ($.state.qID <= 0) return;
    destroyAnswerOptionUIs(); // Ensure previous UI elements are destroyed
    $.subNode("RadioButtonIndicator").setEnabled(false);
    $.state.qID = $.state.qID - 1;
    $.state.tmpAnswer = null;
    tryInitQuestion();
}

function reset(showStartHint = true) {
    destroyAnswerOptionUIs(); // Reset will also destroy any existing answer option UIs
    $.setStateCompat("this", "form_set_start_hint_active", showStartHint);
    $.subNode("RadioButtonIndicator").setEnabled(false);
    $.state.answers = [];
    $.state.qID = 0;
    $.state.isInitiated = false;
    $.state.tryInitQuestion = false;
    $.state.answerOptionUIs = [];
    $.state.answerOptionLocalPositions = [];
    $.state.answerOptionIndex = 0;
    $.state.pendingAnswerOptions = [];
    $.setStateCompat("this", "form_set_content_active", false);
}

$.onExternalCallEnd((res, meta, err) => {
    if (res == null) {
        $.log("callExternal ERROR: " + err);
        return;
    }

    if (meta === "getQuestions") {
        const parsedRes = JSON.parse(res);
        $.state.questions = parsedRes.questions;
        $.subNode("LoadingHint").setEnabled(false);
        tryInitQuestion();
    }

    if (meta === "postQuestionAnswers") {
        $.log("Answers recorded!");
        $.sendSignalCompat("this", "state_triggerTransition");
        reset(false);
    }
});

function splitTextByWidth(text, maxWidth = 50) {
    const lines = [];
    let currentLine = '';
    let currentWidth = 0;

    for (const char of text) {
        // Check if the character is full-width (2 bytes in UTF-16)
        const charWidth = char.match(/[^\x00-\x7F]/) ? 2 : 1;

        if (currentWidth + charWidth > maxWidth) {
        lines.push(currentLine);
        currentLine = '';
        currentWidth = 0;
        }

        currentLine += char;
        currentWidth += charWidth;
    }

    if (currentLine) {
        lines.push(currentLine);
    }

    return lines.join('\n');
}

const haptics = _.hapticsHandle;

_.onReceive((id, body, sender) => {
    switch (id) {
        case "haptics":
            if (haptics.isAvailable()) {
                const effect = new HapticsEffect();
                effect.frequency = body.frequency;
                effect.amplitude = body.amplitude;
                effect.duration = body.duration;
                haptics.playEffect(effect, body.target);
            }
            break;
        case "initializeParticipant":
            _.setMoveSpeedRate(1);
            _.sendTo(sender, "envInfoResponse", {
                isAndroid: _.isAndroid,
                isDesktop: _.isDesktop,
                isIos: _.isIos,
                isMacOs: _.isMacOs,
                isMobile: _.isMobile,
                isVr: _.isVr,
                isWindows: _.isWindows
            });
            break;
        case "setQuestionnaireUI":
            setQuestionnaireUI(body.n,
                (body.hasOwnProperty("e") ? body.e : null),
                body.p, body.r,
                (body.hasOwnProperty("t") ? body.t.toString() : null));
            break;
        case "setQuestionnaireUIs":
            for (const ui of body) {
                setQuestionnaireUI(ui.n,
                    (ui.hasOwnProperty("e") ? ui.e : null),
                    ui.p, ui.r,
                    (ui.hasOwnProperty("t") ? ui.t.toString() : null));
            }
            break;
        case "clearQuestionnaireAnswerUIs":
            let targetUI = _.playerLocalObject("luida_questionnaire_ui").findObject("AnsText");
            targetUI.setEnabled(false);
            for (let i = 0; i < 20; i++) {
                targetUI = _.playerLocalObject("luida_questionnaire_ui").findObject("AnsOptLabel_" + i);
                if (targetUI) {
                    targetUI.setEnabled(false);
                } else {
                    break;
                }
            }
            break;
        default:
            break;
    }
});

function setQuestionnaireUI(targetName, isEnabled, position, rotation, text) {
    let targetUI = _.playerLocalObject("luida_questionnaire_ui").findObject(targetName);
    if (!targetUI) return;
    if (isEnabled !== null) targetUI.setEnabled(isEnabled);
    if (position) targetUI.getUnityComponent("Transform").unityProp.localPosition = position;
    if (rotation) targetUI.getUnityComponent("Transform").unityProp.localRotation = rotation;
    if (text !== null) targetUI.findObject("Text").getUnityComponent("Text").unityProp.text = text;
}

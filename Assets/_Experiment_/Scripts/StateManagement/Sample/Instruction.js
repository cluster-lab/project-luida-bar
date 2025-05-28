const stateEnterActions = {
    1: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: () => {
            $.subNode('Text').setText(`In this study, we replicate the Stroop effect in a VR environment—
            the phenomenon whereby it takes longer to identify a word’s font color 
            when that color conflicts with the word’s semantic meaning—
            and examine how the stimulus presentation depth (near vs. far) 
            affects response times and accuracy.`);
        } }
    ],
    3: [
        { type: "exec", action: () => {
            if (CONDITION['request'] === 'font') {
              $.subNode('Text').setText(`Click the button that matches the text's font color.`);
            }
        } },
        { type: "exec", action: () => {
            if (CONDITION['request'] === 'meaning') {
              $.subNode('Text').setText(`Click the button that matches the text's meaning.`);
            }
        } }
    ],
    5: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`Well done!
            Now please fill in a questionnaire
            (Displayed in 10 seconds)`);
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
    3: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`Take a break for 3 seconds`);
        } }
    ],
    5: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } }
    ]
};
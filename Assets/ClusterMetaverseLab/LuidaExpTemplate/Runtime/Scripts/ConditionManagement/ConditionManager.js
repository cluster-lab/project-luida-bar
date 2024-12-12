$.onStart(() => {
    reset();
});

$.onUpdate(() => {
    if ($.state.trialID !== $.getStateCompat("global", "exp_trialID", "integer")) {
        $.state.trialID = $.getStateCompat("global", "exp_trialID", "integer");
        
        if ($.state.trialID >= $.state.trialCount) {
            $.sendSignalCompat("this", "exp_resetTrials");
        } else if ($.state.trialID <= -1) {
            reset();
        } else {
            let condition = { ...$.state.betweenSubjectsConditions };
            for (let i = 0; i < $.state.withinSubjectsVariableNames.length; i++) {
                condition[$.state.withinSubjectsVariableNames[i]] = $.state.withinSubjectsConditionIndicesByTrial[$.state.trialID][i]
            }
            $.groupState.currentCondition = condition;
    
            // Check if this is the last trial (if true, stop repeating trials when next state transition is triggered)
            if ($.state.trialCount > 0 && !$.state.isLast && $.state.trialID >= $.state.trialCount - 1) {
                $.state.isLast = true;
                $.sendSignalCompat("this", "exp_readyToLeaveTrials");
            }
        }
    }
});

$.onReceive((messageType, arg, sender) => {
    switch (messageType) {
        // case "exp_conditionDependentObject":
        //     $.state.conditionDependentObjects = [ ...$.state.conditionDependentObjects, sender ];
        //     break;
        // case "exp_questionnaire_answer":
        //     $.state.betweenSubjectsConditions = GetBetweenSubjectsCondition(arg);
        //     break;
        default:
            break;
    }
});

function reset() {
    $.state.trialID = -1;
    $.state.betweenSubjectsConditions = {};
    $.state.withinSubjectsVariableNames = [];
    $.state.withinSubjectsConditionIndicesByTrial = [];
    $.state.trialCount = 1;
    $.state.conditionDependentObjects = [];
    $.groupState.currentCondition = {};
    initializeBetweenSubjectsConditionsRandomly();
    try {
        initializeWithinSubjectsConditions(within_subjects_variables, trialsCountForEachUniqueCondition);
    } catch (error) {
        $.log("Within-subjects variables are not defined.");
    }
}

function initializeBetweenSubjectsConditionsRandomly() {
    let betweenSubjectsCondition = {};
    try {
        between_subjects_variables.forEach(v => {
            betweenSubjectsCondition[v.name] = v.values[Math.floor(Math.random() * v.values.length)];
        });
    } catch (e) {
        $.log("Between-subjects variables are not defined.");
    }
    return betweenSubjectsCondition;
}

function initializeWithinSubjectsConditions(variables, repeatsPerCond = 1) {
    variables = [ ...variables.sort((a, b) => a.isRandom - b.isRandom) ];
    const varNames = variables.map(v => v.name);

    let indicesPerVar = variables.map(v => Array.from({ length: v.values.length }, (_, i) => i));
    let condIndicesList = indicesPerVar.reduce((acc, array) => {
        return acc.flatMap(accItem => array.map(arrayItem => [...accItem, arrayItem]));
    }, [[]]).flatMap(condIndices => Array.from({ length: repeatsPerCond }, () => condIndices));

    let shufflePartitionSize = 0;
    for (let i = 0; i < variables.length; i++) {
        if (variables[i].isRandom) {
            shufflePartitionSize = shufflePartitionSize === 0 ? variables[i].values.length : shufflePartitionSize * variables[i].values.length;
        }
    }
    shufflePartitionSize *= repeatsPerCond;

    if (shufflePartitionSize > 0) {
        const result = [...condIndicesList]; // Copy the array to avoid mutating the original
        for (let i = 0; i < condIndicesList.length; i += shufflePartitionSize) {
            const partition = result.slice(i, i + shufflePartitionSize);
            partition = shuffleArray(partition);
            result.splice(i, shufflePartitionSize, ...partition);
        }
        condIndicesList = result;
    }

    $.state.withinSubjectsVariableNames = varNames;
    $.state.withinSubjectsConditionIndicesByTrial = condIndicesList;
    $.state.trialCount = condIndicesList.length;
};

function shuffleArray(array) {
    for (let i = array.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [array[i], array[j]] = [array[j], array[i]];
    }
    return array;
}

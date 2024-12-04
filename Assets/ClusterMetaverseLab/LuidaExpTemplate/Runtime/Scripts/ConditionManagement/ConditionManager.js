const within_subjects_variables_dummy = [
    { name: "color", values: ["R", "G", "B"], isRandom: false },
    { name: "size", values: [5, 10, 15], isRandom: true },
];

const between_subjects_variables_dummy = [
    { name: "method", values: ["old", "new"] }
];

function GetBetweenSubjectsCondition(questionnaireAnswers) {
    return {};
}

$.onStart(() => {
    $.state.trialCount = -1;
    $.state.conditionDependentObjects = [];
});

$.onUpdate(() => {
    if ($.getStateCompat("this", "exp_initCondition", "boolean")) {
        $.setStateCompat("this", "exp_initCondition", false);
        $.state.isLast = false;
        try {
            initCondWithinSubjects(within_subjects_variables, trialsCountForEachUniqueCondition);
        } catch (error) {
            $.log("Within-subjects variables are not defined. Set trial count to 1 and set conditions to empty object.");
            $.state.wsVarNames = [];
            $.state.wsCondIndicesList = [];
            $.state.trialCount = 1;
        }
        sendUpdates();
    }
    if ($.state.trialCount > 0 && !$.state.isLast && $.getStateCompat("global", "exp_conditionID", "integer") >= $.state.trialCount - 1) {
        $.state.isLast = true;
        $.sendSignalCompat("this", "exp_readyToLeaveTrials");
    }
});

$.onReceive((messageType, arg, sender) => {
    switch (messageType) {
        case "exp_conditionDependentObject":
            $.state.conditionDependentObjects = [ ...$.state.conditionDependentObjects, sender ];
            break;
        case "exp_questionnaire_answer":
            $.state.bsCond = GetBetweenSubjectsCondition(arg);
        default:
            break;
    }
});

// --------- Initialize conditions from variables ----------

function initCondWithinSubjects(variables, repeatsPerCond = 1) {
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

    $.state.wsVarNames = varNames;
    $.state.wsCondIndicesList = condIndicesList;
    $.state.trialCount = condIndicesList.length;
};

// ---------- Send initialized conditions ----------

async function sendUpdates() {
    const batchSize = 10; // Number of objects to process per batch
    const delay = 1000; // Delay in milliseconds between batches

    // Helper function to process objects in batches with delay
    async function processInBatches(objects, processFn) {
        for (let i = 0; i < objects.length; i += batchSize) {
            const batch = objects.slice(i, i + batchSize);
            await Promise.all(batch.map(processFn));
            if (i + batchSize < objects.length) {
                await sleep(delay);
            }
        }
    }

    // Send `exp_bsCond` and `exp_wsVarNames` once to each object
    await processInBatches($.state.conditionDependentObjects, async (obj) => {
        obj.send("exp_updateConditions", { bsCond: $.state.bsCond, wsVarNames: $.state.wsVarNames });
    });

    await sleep(delay);

    // Function to split large data into smaller chunks
    function splitLargeData(data, maxSize) {
        const chunks = [];
        let currentChunk = [];
        let currentSize = 0;

        for (const item of data) {
            const itemSize = $.computeSendableSize([item]); // Calculate size of a single item
            if (currentSize + itemSize > maxSize) {
                chunks.push(currentChunk); // Push current chunk and start a new one
                currentChunk = [];
                currentSize = 0;
            }
            currentChunk.push(item);
            currentSize += itemSize;
        }

        if (currentChunk.length > 0) {
            chunks.push(currentChunk); // Push the final chunk
        }

        return chunks;
    }

    // Split `$.state.wsCondIndicesList` into smaller chunks if necessary
    const maxChunkSize = 900;
    const wsChunks = splitLargeData($.state.wsCondIndicesList, maxChunkSize);

    // Send each chunk of `wsCondIndicesList`
    for (let i = 0; i < wsChunks.length; i++) {
        const isLastChunk = i === wsChunks.length - 1;

        await processInBatches($.state.conditionDependentObjects, async (obj) => {
            obj.send("exp_updateConditions", { 
                wsCondIndicesList: wsChunks[i], 
                isWsDone: isLastChunk 
            });
        });

        if (!isLastChunk) {
            await sleep(delay);
        }
    }
}

// ---------- Helper functions ---------

function shuffleArray(array) {
    for (let i = array.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [array[i], array[j]] = [array[j], array[i]];
    }
    return array;
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

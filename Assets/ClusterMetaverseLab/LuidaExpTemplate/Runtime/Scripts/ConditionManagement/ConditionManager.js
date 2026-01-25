$.state.isServerAssigned = false;

$.onStart(() => {
  initializeRandomBetweenSubjectsConditions();
  reset();
});

$.onUpdate(() => {
  if (
    $.state.trialID !== $.getStateCompat("global", "exp_trialID", "integer") &&
      $.getStateCompat("global", "exp_trialID", "integer") >= 0
  ) {
    $.state.trialID = $.getStateCompat("global", "exp_trialID", "integer");
    updateCondition();
  }
});

$.onReceive((messageType, arg, sender) => {
  switch (messageType) {
    case "luida_server_assigned_conditions":
      $.state.betweenSubjectsConditions = arg;
      $.state.isServerAssigned = true;
      $.log("Server assigned conditions: " + JSON.stringify(arg));
      break;
    case "luida_participants_info":
      $.groupState.participants = arg.participants;
      $.groupState.sessionID = arg.sessionID;
      sender.send("betweenSubjectsCondition", $.state.betweenSubjectsConditions);
      break;
    // case "exp_questionnaire_answer":
    //     $.state.betweenSubjectsConditions = GetBetweenSubjectsCondition(arg);
    //     break;
    default:
      break;
  }
});

function updateCondition() {
  if ($.state.trialID >= $.state.trialCount) {
    $.sendSignalCompat("this", "exp_resetTrials");
  } else if ($.state.trialID <= -1) {
    reset();
  } else {
    let condition = { ...$.state.betweenSubjectsConditions };
    for (let i = 0; i < $.state.withinSubjectsVariableNames.length; i++) {
      const varName = $.state.withinSubjectsVariableNames[i];
      const varValue =
          within_subjects_variables[i].values[
              $.state.withinSubjectsConditionIndicesByTrial[$.state.trialID][i]
              ];
      condition[varName] = varValue;
    }
    $.groupState.currentCondition = condition;

    // Check if this is the last trial (if true, stop repeating trials when next state transition is triggered)
    if (
        $.state.trialCount > 0 &&
        !$.state.isLast &&
        $.state.trialID >= $.state.trialCount - 1
    ) {
      $.state.isLast = true;
      $.sendSignalCompat("this", "exp_readyToLeaveTrials");
    }
  }
}

function reset() {
  $.state.trialID = 0;
  if (!$.state.betweenSubjectsConditions)
    $.state.betweenSubjectsConditions = {};
  $.state.withinSubjectsVariableNames = [];
  $.state.withinSubjectsConditionIndicesByTrial = [];
  $.state.trialCount = 1;
  $.groupState.currentCondition = {
    ...$.state.betweenSubjectsConditions,
  };
  $.groupState.stateNames = state_names || [];
  try {
    initializeWithinSubjectsConditions(
      within_subjects_variables,
      trialsCountForEachUniqueCondition
    );
  } catch (error) {
    $.log(error);
    $.log("Within-subjects variables are not defined.");
    initializeWithinSubjectsConditions([], 1);
  }
  updateCondition();
}

function initializeRandomBetweenSubjectsConditions() {
  // Skip if server already assigned conditions
  if ($.state.isServerAssigned && $.state.betweenSubjectsConditions) {
    $.log("Using server-assigned between-subjects conditions");
    return;
  }

  let betweenSubjectsCondition = {};
  try {
    between_subjects_variables.forEach((v) => {
      if (!betweenSubjectsCondition[v.name]) {
        if (v.debugValue) {
          betweenSubjectsCondition[v.name] = v.debugValue;
        } else if (v.isRandom) {
          betweenSubjectsCondition[v.name] =
              v.values[Math.floor(Math.random() * v.values.length)];
        }
      }
    });
  } catch (e) {
    $.log("Between-subjects variables are not defined.");
  }
  $.state.betweenSubjectsConditions = betweenSubjectsCondition;
}

function initializeWithinSubjectsConditions(variables, repeatsPerCond = 1) {
  variables = [...variables.sort((a, b) => a.isRandom - b.isRandom)];
  const varNames = variables.map((v) => v.name);

  let indicesPerVar = variables.map((v) =>
    Array.from({ length: v.values.length }, (_, i) => i)
  );
  let condIndicesList = indicesPerVar
    .reduce(
      (acc, array) => {
        return acc.flatMap((accItem) =>
          array.map((arrayItem) => [...accItem, arrayItem])
        );
      },
      [[]]
    )
    .flatMap((condIndices) =>
      Array.from({ length: repeatsPerCond }, () => condIndices)
    );

  let shufflePartitionSize = 0;
  for (let i = 0; i < variables.length; i++) {
    if (variables[i].isRandom) {
      shufflePartitionSize =
        shufflePartitionSize === 0
          ? variables[i].values.length
          : shufflePartitionSize * variables[i].values.length;
    }
  }
  shufflePartitionSize *= repeatsPerCond;

  if (shufflePartitionSize > 0) {
    const result = [...condIndicesList]; // Copy the array to avoid mutating the original
    for (let i = 0; i < condIndicesList.length; i += shufflePartitionSize) {
      let partition = result.slice(i, i + shufflePartitionSize);
      partition = shuffleArray(partition);
      result.splice(i, shufflePartitionSize, ...partition);
    }
    condIndicesList = result;
  }

  $.state.withinSubjectsVariableNames = varNames;
  $.state.withinSubjectsConditionIndicesByTrial = condIndicesList;
  $.state.trialCount = condIndicesList.length;
}

function shuffleArray(array) {
  for (let i = array.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [array[i], array[j]] = [array[j], array[i]];
  }
  return array;
}

$.onStart(() => {
  $.state.isBetweenSubjectsConditionsSet = false;
  $.groupState.isParticipantsEnough = false;
  $.groupState.sessionID = Date.now() + "_" +  (Math.random() + 1).toString(36).substring(2, 8);
  $.groupState.participants = []; // array of PlayerHandle who are currently in the experiment
  $.state.participantsEnvInfo = [];
  $.state.idfc2userId = {};
  $.state.timer = 0;
  
  // TODO: load exp info so that we can check later what environments this experiment requires
})

$.onUpdate((deltaTime) => {
    if (!$.groupState.isParticipantsEnough) {
        $.state.timer += deltaTime;
        if ($.state.timer >= 1){
            $.state.timer = 0;
            let pIdfcs = $.groupState.participants.map(p => p.idfc);
            const newPlayers = $.getPlayersNear($.getPosition(), Infinity)
                .filter(p => !pIdfcs.includes(p.idfc));
            if (newPlayers.length > 0) {
                for (const newPlayer of newPlayers) {
                    // TODO: Check if the player is eligible to join the experiment before adding them to $.groupState.participants.
                    $.groupState.participants = [ ...$.groupState.participants, newPlayer ];
                    $.setPlayerScript(newPlayer);
                    newPlayer.send("initializeParticipant", true);
                }
            }
            if (!$.groupState.isParticipantsEnough && $.groupState.participants.length >= pNum) {
                HandleParticipantsEnough();
            }
        }
    } else if ($.state.isBetweenSubjectsConditionsSet) { // participants are enough & conditions are set
        $.state.isBetweenSubjectsConditionsSet = false;
        let request = {
            type: "uploadCustomData",
            data: {
                pInfo: $.state.participantsEnvInfo.map(info => ({ ts: Date.now(), sID: $.groupState.sessionID || "", ...info, betweenSubjectsConditions: $.state.betweenSubjectsConditions })),
                idfc2userId: $.state.idfc2userId
            },
            token: token || "",
            eID: expID || "",
            sID: $.groupState.sessionID || ""
        };
        $.callExternal(new ExternalEndpointId(callExternalEndpointID), JSON.stringify(request), "customDataUploaded");
    }
/*
  if ($.getStateCompat("this", "exp_checkJoinEligibility", "boolean")) {
    $.setStateCompat("this", "exp_checkJoinEligibility", false);

    let pIdfcs = $.groupState.participants.map(p => p.idfc);
    const newPlayers = $.getPlayersNear($.getPosition(), 1)
      .filter(p => !pIDFCs.includes(p.idfc));
    
    if (newPlayers.length > 0) {
      $.state.newPlayers = newPlayers;
      $.log(newPlayers[0].idfc);

      const request = {
        type: "checkJoinEligibility",
        token: token || "",
        eID: expID || "",
        idfcs: newPlayers.map(p => p.idfc).join("|")
      };
      $.callExternal(new ExternalEndpointId(callExternalEndpointID), JSON.stringify(request), "joinEligibilityChecked");
    }
  }
*/
})

$.onReceive((messageType, arg, sender) => {
    switch (messageType) {
        case "betweenSubjectsCondition":
            $.state.betweenSubjectsConditions = arg;
            $.state.isBetweenSubjectsConditionsSet = true;
            break;
        case "envInfoResponse":
            $.state.participantsEnvInfo = [
              ...$.state.participantsEnvInfo,
              {
                idfc: sender.idfc,
                envInfo: arg
              }
            ];
            let idfc2userId = { ...$.state.idfc2userId };
            idfc2userId[sender.idfc] = sender.userId;
            $.state.idfc2userId = idfc2userId;
            break;
        default:
            break;
    }
}, { item: true, player: true });

function HandleParticipantsEnough() {
  $.log("Participants are enough. Checking session status...");
  $.groupState.isParticipantsEnough = true;

  // Build between-subjects config from local variables
  let betweenSubjectsConfig = [];
  try {
    if (typeof between_subjects_variables !== 'undefined' && Array.isArray(between_subjects_variables)) {
      betweenSubjectsConfig = between_subjects_variables.map(v => ({
        name: v.name,
        values: v.values
      }));
    }
  } catch (e) {
    $.log("No between_subjects_variables defined: " + e);
  }

  const request = {
    type: "getSessionStatus",
    token: token || "",
    eID: expID || "",
    betweenSubjectsConfig: betweenSubjectsConfig
  };
  $.callExternal(new ExternalEndpointId(callExternalEndpointID), JSON.stringify(request), "sessionStatusChecked");
}

function proceedWithLocalConditions() {
  const conditionManager = $.worldItemReference("ConditionManager");
  if (conditionManager) {
    conditionManager.send("luida_participants_info", {
      participants: $.groupState.participants,
      sessionID: $.groupState.sessionID
    });
  }
  $.sendSignalCompat("this", "exp_playersAreEnough");
  $.sendSignalCompat("this", "exp_StartStateTransition");
}

$.onExternalCallEnd((res, meta, err) => {
  if (res == null) {
    $.log("callExternal ERROR: " + err);
    // Fall back to local random assignment on error
    if (meta === "sessionStatusChecked") {
      proceedWithLocalConditions();
    }
    return;
  }

  if (meta === "sessionStatusChecked") {
    try {
      const parsedRes = JSON.parse(res);

      if (!parsedRes.canAccept) {
        $.log("Experiment has reached max sessions. Current: " + parsedRes.currentSessionCount + "/" + parsedRes.maxSessionCount);
        // Optionally handle rejection (teleport back, show message, etc.)
        return;
      }

      const conditionManager = $.worldItemReference("ConditionManager");
      if (conditionManager) {
        if (parsedRes.assignedConditions) {
          conditionManager.send("luida_server_assigned_conditions", parsedRes.assignedConditions);
        }
        conditionManager.send("luida_participants_info", {
          participants: $.groupState.participants,
          sessionID: $.groupState.sessionID
        });
      }

      $.sendSignalCompat("this", "exp_playersAreEnough");
      $.sendSignalCompat("this", "exp_StartStateTransition");
    } catch (parseError) {
      $.log("Error parsing session status response: " + parseError);
      proceedWithLocalConditions();
    }
  }
});

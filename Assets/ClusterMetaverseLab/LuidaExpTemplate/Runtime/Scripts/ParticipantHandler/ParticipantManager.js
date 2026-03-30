const REJECTION_GATE_POS = new Vector3(-100, -100, -100);

$.onStart(() => {
  $.state.isBetweenSubjectsConditionsSet = false;
  $.groupState.isParticipantsEnough = false;
  $.groupState.sessionID = Date.now() + "_" +  (Math.random() + 1).toString(36).substring(2, 8);
  $.groupState.participants = []; // array of PlayerHandle who are currently in the experiment
  $.state.participantsEnvInfo = [];
  $.state.idfc2userId = {};
  $.state.timer = 0;
  $.state.pendingEligibilityChecks = {};
  $.state.rejectionQueue = [];
  $.state.isProcessingRejection = false;
  $.state.rejectionTimer = 0;

  // Place world gate at remote position and keep it enabled
  $.subNode("WorldGateToLuidaBar").setPosition(REJECTION_GATE_POS);
  $.subNode("WorldGateToLuidaBar").setEnabled(false);
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

    // Process rejection queue: teleport rejected players to remote world gate one at a time
    if ($.state.rejectionQueue.length > 0 && !$.state.isProcessingRejection) {
        const rejection = $.state.rejectionQueue[0];
        const allPlayers = $.getPlayersNear($.getPosition(), Infinity);
        const player = allPlayers.find(p => p.idfc === rejection.idfc);

        if (!player) {
            // Player already left, skip
            $.state.rejectionQueue = $.state.rejectionQueue.slice(1);
        } else {
            $.state.isProcessingRejection = true;
            $.state.rejectionTimer = 0;
            // Teleport player to the gate (convert item-local to global coords)
            $.subNode("WorldGateToLuidaBar").setEnabled(true);
            const itemPos = $.getPosition();
            player.setPosition(new Vector3(
                REJECTION_GATE_POS.x + itemPos.x,
                REJECTION_GATE_POS.y + itemPos.y + 0.5,
                REJECTION_GATE_POS.z + itemPos.z
            ));
            $.log("Rejection: teleported player " + rejection.idfc + " to remote world gate");
        }
    }

    if ($.state.isProcessingRejection) {
        $.state.rejectionTimer += deltaTime;
        if ($.state.rejectionTimer >= 1.5) {
            const processed = $.state.rejectionQueue[0];
            $.state.rejectionQueue = $.state.rejectionQueue.slice(1);
            $.state.isProcessingRejection = false;
            $.state.rejectionTimer = 0;
            $.subNode("WorldGateToLuidaBar").setEnabled(false);
            $.log("Rejection: finished processing player " + (processed ? processed.idfc : "unknown"));
        }
    }
})

$.onReceive((messageType, arg, sender) => {
    switch (messageType) {
        case "betweenSubjectsCondition":
            $.state.betweenSubjectsConditions = arg;
            $.state.isBetweenSubjectsConditionsSet = true;
            break;
        case "envInfoResponse":
            // Store envInfo
            $.state.participantsEnvInfo = [
              ...$.state.participantsEnvInfo,
              { idfc: sender.idfc, envInfo: arg }
            ];
            let idfc2userId = { ...$.state.idfc2userId };
            idfc2userId[sender.idfc] = sender.userId;
            $.state.idfc2userId = idfc2userId;

            // Check eligibility via backend API
            let pendingChecks = { ...$.state.pendingEligibilityChecks };
            pendingChecks[sender.idfc] = true;
            $.state.pendingEligibilityChecks = pendingChecks;

            const eligibilityRequest = {
                type: "checkJoinEligibility",
                token: token || "",
                eID: expID || "",
                envInfo: [arg]
            };
            $.callExternal(
                new ExternalEndpointId(callExternalEndpointID),
                JSON.stringify(eligibilityRequest),
                "joinEligibilityChecked_" + sender.idfc
            );
            break;
        default:
            break;
    }
}, { item: true, player: true });

function HandleParticipantsEnough() {
  $.log("Participants are enough to start the experiment.");
  $.groupState.isParticipantsEnough = true;
  $.sendSignalCompat("this", "exp_playersAreEnough");
  $.sendSignalCompat("this", "exp_StartStateTransition");

  const conditionManager = $.worldItemReference("ConditionManager");
  if (conditionManager) {
    conditionManager.send("luida_participants_info", {
      participants: $.groupState.participants,
      sessionID: $.groupState.sessionID
    });
  }
}

$.onExternalCallEnd((res, meta, err) => {
  if (res == null) {
    $.log("callExternal ERROR: " + err);
    return;
  }

  if (meta.startsWith("joinEligibilityChecked_")) {
    const idfc = meta.replace("joinEligibilityChecked_", "");
    const parsedRes = JSON.parse(res);

    // Clear pending check
    let pendingChecks = { ...$.state.pendingEligibilityChecks };
    delete pendingChecks[idfc];
    $.state.pendingEligibilityChecks = pendingChecks;

    if (!parsedRes.eligible) {
      $.log("Platform not allowed for " + idfc + ": " + (parsedRes.reason || "unknown"));
      // Remove ineligible participant
      $.groupState.participants = $.groupState.participants.filter(
        p => p.idfc !== idfc
      );
      $.state.participantsEnvInfo = $.state.participantsEnvInfo.filter(
        info => info.idfc !== idfc
      );
      // Enqueue for isolated rejection via remote world gate
      $.state.rejectionQueue = [...$.state.rejectionQueue, { idfc: idfc }];
    }
  }

  if (meta === "customDataUploaded") {
    $.log("Response after customDataUploaded called: " + JSON.stringify(res));
  }
});

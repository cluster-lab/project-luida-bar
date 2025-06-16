$.onStart(() => {
  $.groupState.isParticipantsEnough = false;
  $.groupState.participants = []; // array of PlayerHandle who are currently in the experiment
  $.state.pIDFCs = []; // array of player idfcs who are currently in the experiment
})

$.onUpdate(() => {
    if ($.getStateCompat("this", "onJoined", "boolean")) {
        $.setStateCompat("this", "onJoined", false);
        const newPlayers = $.getPlayersNear($.getPosition(), Infinity)
            .filter(p => !$.state.pIDFCs.includes(p.idfc));
        if (newPlayers.length > 0) {
            for (const newPlayer of newPlayers) {
                // TODO: Check if the player is eligible to join the experiment before adding them to pIDFCs & participants.
                $.state.pIDFCs.push(newPlayer.idfc);
                $.groupState.participants.push(newPlayer);
                $.setPlayerScript(newPlayer);
                newPlayer.send("envInfoRequest", true);
            }
        }
        if (!$.groupState.isParticipantsEnough && $.state.pIDFCs.length >= pNum) {
            $.groupState.isParticipantsEnough = true;
            $.log("Participants are enough to start the experiment.");
        }
    }
/*
  if ($.getStateCompat("this", "exp_checkJoinEligibility", "boolean")) {
    $.setStateCompat("this", "exp_checkJoinEligibility", false);

    const newPlayers = $.getPlayersNear($.getPosition(), 1)
      .filter(p => !$.state.pIDFCs.includes(p.idfc));
    
    if (newPlayers.length > 0) {
      $.state.newPlayers = newPlayers;
      $.log(newPlayers[0].idfc);

      const request = {
        type: "checkJoinEligibility",
        token: token || "",
        eID: expID || "",
        idfcs: newPlayers.map(p => p.idfc).join("|")
      };
      $.callExternal(callExternalEndpointID, JSON.stringify(request), "joinEligibilityChecked");
    }
  }
*/
})

$.onReceive((messageType, arg, sender) => {
    switch (messageType) {
        case "envInfoResponse":
            let request = {
                type: "uploadCustomData",
                data: { envInfo: [ arg ] },
                token: token || "",
                eID: expID || "",
                pID: sender.idfc
            };
            $.callExternal(callExternalEndpointID || "", JSON.stringify(request), "customDataUploaded");
            break;
        default:
            break;
    }
});

/*
$.onExternalCallEnd((res, meta, err) =>
{
  if (res == null) {
    $.log("callExternal ERROR: " + err);
    return;
  }

  if (meta === "joinEligibilityChecked") {
    const parsedRes = JSON.parse(res);
    $.state.newPlayers.forEach(newPlayer => {
      if (parsedRes.ineligibleIdfcs.includes(newPlayer.idfc)) {
        // TODO: Show message about that this player is not eligible to join this experiment, and teleport the player back to LUIDA bar world.
      } else {
        newPlayer.setMoveSpeedRate(1);
        $.state.pIDFCs = [ ...$.state.pIDFCs, newPlayer.idfc ];
        // TODO: Instead of enabling move, teleport the player from the checking area to the task area.
      }
    });
    
    $.state.newPlayers = [];
    $.log("Only users who have not joined this experiment before are allowed to proceed to the experiment.");
  }
});
*/

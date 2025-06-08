$.onStart(() => {
  $.state.inRoomIdfcs = [];
})

$.onUpdate(() => {
    if ($.getStateCompat("this", "onJoined", "boolean")) {
        $.setStateCompat("this", "onJoined", false);
        const newPlayers = $.getPlayersNear($.getPosition(), Infinity)
            .filter(p => !$.state.inRoomIdfcs.includes(p.idfc));
        if (newPlayers.length > 0) {
            for (const newPlayer of newPlayers) {
                $.state.inRoomIdfcs.push(newPlayer.idfc);
                $.setPlayerScript(newPlayer);
            }
        }
    }
/*
  if ($.getStateCompat("this", "exp_checkJoinEligibility", "boolean")) {
    $.setStateCompat("this", "exp_checkJoinEligibility", false);

    const newPlayers = $.getPlayersNear($.getPosition(), 1)
      .filter(p => !$.state.inRoomIdfcs.includes(p.idfc));
    
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
        $.state.inRoomIdfcs = [ ...$.state.inRoomIdfcs, newPlayer.idfc ];
        // TODO: Instead of enabling move, teleport the player from the checking area to the task area.
      }
    });
    
    $.state.newPlayers = [];
    $.log("Only users who have not joined this experiment before are allowed to proceed to the experiment.");
  }
});
*/

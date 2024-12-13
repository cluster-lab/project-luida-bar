$.onUpdate(() => {
  if ($.getStateCompat("this", "state_enter", "boolean")) {
      $.setStateCompat("this", "state_enter", false);
      OnStateEnter();
  }
})

function OnStateEnter() {
  const request = {
    type: "recordIdfcs",
    token: token || "",
    eID: expID || "",
    idfcs: $.getPlayersNear($.getPosition().clone(), Infinity).map(player => player.idfc),
  };
  $.callExternal(JSON.stringify(request), "userIdfcsRecorded");
}

$.onExternalCallEnd((res, meta, err) =>
{
  if (res == null) {
    $.log("callExternal ERROR: " + err);
    return;
  }

  if (meta === "userIdfcsRecorded") {
    $.log("Users' idfc are recorded. Reward to these users will be proceeded, and these users will not be able to enter this experiment again.");
  }
});
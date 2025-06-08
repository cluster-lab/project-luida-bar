$.onUpdate(() => {
  if ($.getStateCompat("this", "state_enter", "boolean")) {
      $.setStateCompat("this", "state_enter", false);
      OnStateEnter();
  }
})

function OnStateEnter() {
  const request = {
    type: "postDoneIdfcs",
    token: token || "",
    eID: expID || "",
    idfcs: $.getPlayersNear($.getPosition().clone(), Infinity).map(player => player.idfc),
  };
  $.callExternal(callExternalEndpointID, JSON.stringify(request), "doneIdfcsPosted");
}

$.onExternalCallEnd((res, meta, err) =>
{
  if (res == null) {
    $.log("callExternal ERROR: " + err);
    return;
  }

  if (meta === "doneIdfcsPosted") {
    $.log("idfc of users that are done with this experiment this time are recorded.");
  }
});
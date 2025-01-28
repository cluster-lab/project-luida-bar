function calculateData () {
  let returnData = $.state.customData;
  const CONDITION = $.groupState.currentCondition;

  function saveData_timeAndAvatar() {
return {
  p: $.getPlayersNear($.getPosition(), Infinity)[0].userDisplayName,
  t: Date.now().toString(),
  a: "D",
};
    return {};
  }
  const newRecord_timeAndAvatar = saveData_timeAndAvatar();
  if ("timeAndAvatar" in returnData && Array.isArray(returnData["timeAndAvatar"])) {
    returnData["timeAndAvatar"].push(newRecord_timeAndAvatar);
  } else {
    returnData["timeAndAvatar"] = [newRecord_timeAndAvatar];
  }

  return returnData;
}

function calculateData () {
  let returnData = $.state.customData;
  const CONDITION = $.groupState.currentCondition;

  function saveData_avatarAndTimestamp() {
return {
  ts: new Date().valueOf()
};
    return {};
  }
  const newRecord_avatarAndTimestamp = saveData_avatarAndTimestamp();
  if ("avatarAndTimestamp" in returnData && Array.isArray(returnData["avatarAndTimestamp"])) {
    returnData["avatarAndTimestamp"].push(newRecord_avatarAndTimestamp);
  } else {
    returnData["avatarAndTimestamp"] = [newRecord_avatarAndTimestamp];
  }

  return returnData;
}

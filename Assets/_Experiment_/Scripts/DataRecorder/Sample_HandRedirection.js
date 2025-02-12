function calculateData () {
  let returnData = $.state.customData;
  const CONDITION = $.groupState.currentCondition;

  function saveData_taskAnswer() {
return {
  g: CONDITION["gain"],
  a: $.getStateCompat("global", "isFaster", "boolean")
};
    return {};
  }
  const newRecord_taskAnswer = saveData_taskAnswer();
  if ("taskAnswer" in returnData && Array.isArray(returnData["taskAnswer"])) {
    returnData["taskAnswer"].push(newRecord_taskAnswer);
  } else {
    returnData["taskAnswer"] = [newRecord_taskAnswer];
  }

  return returnData;
}

function calculateData () {
  let returnData = $.state.customData;
  const CONDITION = $.groupState.currentCondition;
  const PARTICIPANTS = $.groupState.participants;
  const COLLECTED_DATA = $.groupState.collectedData;

  function saveData_example() {
return { foo: "bar" };
    return {};
  }
  const newRecord_example = saveData_example();
  if ("example" in returnData && Array.isArray(returnData["example"])) {
    returnData["example"].push(newRecord_example);
  } else {
    returnData["example"] = [newRecord_example];
  }

  return returnData;
}

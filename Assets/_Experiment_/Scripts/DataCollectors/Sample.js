function calculateData () {
  let returnData = $.state.customData;
  const CONDITION = $.groupState.currentCondition;

  function saveData_AnswerAndTime() {
return {
  d: CONDITION['depth'],
  req: CONDITION['request'],
  font: CONDITION['font'],
  text: CONDITION['text'],
  ans: $.getStateCompat('global', 'isRed', 'boolean') ? "R" : "B",
  time: $.getStateCompat('owner', 'timer', 'float')
};
    return {};
  }
  const newRecord_AnswerAndTime = saveData_AnswerAndTime();
  if ("AnswerAndTime" in returnData && Array.isArray(returnData["AnswerAndTime"])) {
    returnData["AnswerAndTime"].push(newRecord_AnswerAndTime);
  } else {
    returnData["AnswerAndTime"] = [newRecord_AnswerAndTime];
  }

  return returnData;
}

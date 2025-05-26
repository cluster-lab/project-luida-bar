function calculateData () {
  let returnData = $.state.customData;
  const CONDITION = $.groupState.currentCondition;

  function saveData_SelectionAnswer() {
return {
  mat: CONDITION['material'],
  txt: CONDITION['text'],
  ans: $.getStateCompat("global", "isAnswerRed", "boolean") ? "R" : "B",
  time: $.getStateCompat("owner", "answerTime", "float")
};
    return {};
  }
  const newRecord_SelectionAnswer = saveData_SelectionAnswer();
  if ("SelectionAnswer" in returnData && Array.isArray(returnData["SelectionAnswer"])) {
    returnData["SelectionAnswer"].push(newRecord_SelectionAnswer);
  } else {
    returnData["SelectionAnswer"] = [newRecord_SelectionAnswer];
  }

  return returnData;
}

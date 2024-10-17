function calculateData () {
  let fileName = "taskAnswers";
  let returnData = $.state.customData;

  const newRecord = {
    g: $.state.currentCondition["gain"], // 現在のゲイン条件
    a: $.getStateCompat("global", "isFaster", "boolean") ? "F" : "S" // 「速い」「遅い」のどちらを選んだか
  };

  if (fileName in returnData && Array.isArray(returnData[fileName])) {
    returnData[fileName].push(newRecord);
  } else {
    returnData[fileName] = [newRecord];
  }

  return returnData;
}

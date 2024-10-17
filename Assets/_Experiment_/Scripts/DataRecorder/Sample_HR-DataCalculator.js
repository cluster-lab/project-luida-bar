function calculateData () {
  let fileName = "taskAnswers";
  return {
      ...$.state.customData,
      [fileName]: [
          ...($.state.customData[fileName] || []),
          {
              g: $.state.currentCondition["gain"], // 現在のゲイン条件
              a: $.getStateCompat("global", "isFaster", "boolean") ? "F" : "S" // 「速い」「遅い」のどちらを選んだか
          }
      ]
  };
}

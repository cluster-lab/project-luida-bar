function calculateData () {
  let returnData = $.state.customData;
  const CONDITION = $.groupState.currentCondition;

  function saveData_test() {
$.state.i = ($.state.i || 0) + 1;
return { foo: "bar", score: 12.34, i: $.state.i };
    return {};
  }
  const newRecord_test = saveData_test();
  if ("test" in returnData && Array.isArray(returnData["test"])) {
    returnData["test"].push(newRecord_test);
  } else {
    returnData["test"] = [newRecord_test];
  }

  return returnData;
}

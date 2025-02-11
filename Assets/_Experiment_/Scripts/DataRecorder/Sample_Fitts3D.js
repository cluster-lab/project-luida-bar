function calculateData () {
  let returnData = $.state.customData;
  const CONDITION = $.groupState.currentCondition;

  function saveData_taskTime() {
let data = {
t: Number(($.getStateCompat("owner", "taskTime", "float")).toFixed(4)),
x: $.getStateCompat("owner", "x", "integer"),
y: $.getStateCompat("owner", "y", "integer"),
z: CONDITION["d"],
s: CONDITION["s"]
};
return data;
    return {};
  }
  const newRecord_taskTime = saveData_taskTime();
  if ("taskTime" in returnData && Array.isArray(returnData["taskTime"])) {
    returnData["taskTime"].push(newRecord_taskTime);
  } else {
    returnData["taskTime"] = [newRecord_taskTime];
  }

  return returnData;
}

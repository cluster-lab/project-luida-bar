function calculateData () {
    let fileName = "yourFileName";
    let returnData = $.state.customData;

    /*
      Change the value of `fileName`
      And implement your calculation for data to record
      Then save the calculation result into the `newRecord` variable below
      * You can use $.groupState.currentCondition[variable's name] to access the current experimental condition
    */
    
    const newRecord = {
      // yourKey: yourValue
    };

    if (fileName in returnData && Array.isArray(returnData[fileName])) {
      returnData[fileName].push(newRecord);
    } else {
      returnData[fileName] = [newRecord];
    }
  
    return returnData;
}
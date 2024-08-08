function calculateData () {
    let fileName = "sampleFileName";
    // Implement your calculation for custom data to record & upload
    // $.state.currentCondition is available if this script is combined with CustomDataUploader.js
    
    // Return the data in the following format:
    return { ...$.state.customData, [fileName]: {} }; // $.state.customData is the custom data recorded before
    /*
    e.g.
    return { ...$.state.customData, [fileName]: {
      ...($.state.customData[fileName] || {}),
      labelName: calculationResult
    } };
    */
}
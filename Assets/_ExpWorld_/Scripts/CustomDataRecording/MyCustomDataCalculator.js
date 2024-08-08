function calculateData () {
    let fileName = "taskTime";
    if (!$.state.currentCondition) return $.state.customData || {};
    
    const label = [$.state.currentCondition["question"], $.state.currentCondition["word"], $.state.currentCondition["lang"]].join("_");
    
    return { ...$.state.customData,
        [fileName]: {
            ...($.state.customData[fileName] || {}),
            [label]: $.getStateCompat("owner", "taskTime", "float")
        }
    };
}
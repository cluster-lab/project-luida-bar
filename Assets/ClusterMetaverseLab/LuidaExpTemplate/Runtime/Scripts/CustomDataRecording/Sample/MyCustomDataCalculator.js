function calculateData () {
    let fileName = "taskTime";
    if (!$.state.currentCondition) return $.state.customData || {};
    
    return { ...$.state.customData,
        [fileName]: [
            ...($.state.customData[fileName] || []),
            {
                q: $.state.currentCondition["question"],
                l: $.state.currentCondition["lang"],
                w: $.state.currentCondition["wordObject"],
                t: Math.round($.getStateCompat("owner", "taskTime", "float") * 10000) / 10000
            }
        ]
    };
}
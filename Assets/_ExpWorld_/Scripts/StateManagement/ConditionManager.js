/*
variables: [ { name: "color", values: [“R”, “G”, “B”], isRandomized: false }, { name: "size", values: [5, 10], isRandomized: true } ]

trialsCountForEachCondition = 1

$.state.conditions: [ { color: “R”, size: 5 }, { color: “R”, size: 10 }, { color: “G”, size: 5 }, { color: “G”, size: 10 }, { color: “B”, size: 10 }, { color: “B”, size: 5 } ]

onStart():
    InitConditions();
onUpdate():
    if (!$.state.isLast && global.exp_conditionID >= $.state.conditions.length - 1)
        $.state.isLast = true
        $.sendSignalCompat(“this”, “exp_readyToLeaveTrials”)
onReceive():
    if (type === “conditionDependentObject”) $.state.conditionDependentObjects.push(item)
InitConditions(): Initialize conditions from variables
    decide trial count & conditions in each trial (random or in order) by within-subject independent variables
    send $.state.conditions with type “exp_conditions” to each item in $.state.conditionDependentObjects
    $.state.isLast = false
*/
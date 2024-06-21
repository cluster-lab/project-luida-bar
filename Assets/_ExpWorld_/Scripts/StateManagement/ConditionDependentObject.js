/*
dependingConditions: [ { color: “G” } ]

onStart(): getItemsNear -> send “conditionDependentObject”

onReceive():
    if type === “exp_conditions”:
        $.state.conditions = message
        setDependingConditions()

onUpdate():
    for (var, value) in each dependingCondition:
        if ($.state.conditions[global.exp_conditionID][var] === value): enable self or run functions
setDependingConditions(): set dependingConditions from children gameObjects
    variables.forEach:
        if $.subNode(var.name).getEnabled():
            var.values.forEach:
                if $.subNode(var.name + “|” + value).getEnabled():
                    dependingConditions.push({ [var.name]: value })
*/
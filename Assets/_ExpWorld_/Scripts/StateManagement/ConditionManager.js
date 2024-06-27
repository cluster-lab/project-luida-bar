const variables = [
    { name: "color", values: ["R", "G", "B"], isRandomized: false },
    { name: "size", values: [5, 10], isRandomized: true }
];

const trialsCountForEachCondition = 1;

$.onStart(() => {
    $.state.conditionDependentObjects = [];
});

$.onUpdate(() => {
    if (!$.state.isLast && $.getStateCompat("global", "exp_conditionID", "integer") >= $.state.conditions.length - 1) {
        $.state.isLast = true;
        $.sendSignalCompat("this", "exp_readyToLeaveTrials");
    }
});

$.onReceive((messageType, arg, sender) => {
    switch (messageType) {
        case "exp_conditionDependentObject":
            $.state.conditionDependentObjects = [ ...$.state.conditionDependentObjects, sender ];
            break;
        case "exp_initCondition":
            InitConditions();
            break;
        default:
            break;
    }
});

// Initialize conditions from variables
function InitConditions () {
    $.state.isLast = false;

    // TODO: replace the below line with the following: decide trial count & conditions in each trial (random or in order) by within-subject independent variables
    $.state.conditions = [ { color: "R", size: 5 }, { color: "R", size: 10 }, { color: "G", size: 5 }, { color: "G", size: 10 }, { color: "B", size: 10 }, { color: "B", size: 5 } ];
    
    $.state.conditionDependentObjects.forEach(obj => {
        obj.send("exp_updateConditions", $.state.conditions );
    });
}
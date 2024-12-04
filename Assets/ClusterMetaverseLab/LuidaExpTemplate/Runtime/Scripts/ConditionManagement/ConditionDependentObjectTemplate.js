/*
    ***** Don't use $.onStart in this file! *****
    Implement the Awake function below for executions on initialization
*/
function Awake() {
    
}
/*
    ***** Don't use $.onUpdate! *****
    Implement the Update function below for executions every frame
*/
function Update (deltaTime) {
    // You can use function getCondition(variable's name) to access the current experimental condition
    // e.g. if (getCondition("color") === "R") $.state.timer = $.state.timer + deltaTime;
}

/*
    Implement the OnConditionChanged function below for executions when moving to next trial (i.e. experimental condition is changed)
*/
function OnConditionChanged () {
    // You can use function getCondition(variable's name) to access the current experimental condition
    // e.g. if (getCondition("color") === "R") $.setStateCompat("this", "isEnabled", true);
}

/*
    ***** Don't use $.onReceive in this file! *****
    If necessary, implement the OnReceive function below
*/
// function OnReceive (messageType, arg, sender) {}
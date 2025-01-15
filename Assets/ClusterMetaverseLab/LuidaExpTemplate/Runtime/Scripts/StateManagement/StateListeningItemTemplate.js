$.onStart(() => {
    
})

function OnStateEnter() {
    const STATE_ID = $.state.state_id;
    const CONDITION = $.groupState.currentCondition; // You can use CONDITION[variable's name] to access the current experimental condition
    // if (stateID === i) {}
}

function OnStateExit() {
    const STATE_ID = $.state.state_id;
    const CONDITION = $.groupState.currentCondition;
    // if (stateID === i) {}
}

function DuringState(deltaTime) {
    const STATE_ID = $.state.state_id;
    const CONDITION = $.groupState.currentCondition;
    // if (stateID === i) {}
}

function Update(deltaTime) {}

/*
onUpdate:
    if ($.getStateCompat(“this”, “state_exit”, “boolean”)) 
        OnStateExit()
        $.setStateCompat(“this”, “state_exit”, false)
    if ($.getStateCompat(“this”, “state_enter”, “boolean”)) 
        $.setStateCompat(“this”, “state_enter”, false)
        OnStateEnter()
    do some calculation (e.g. count time or sth) and if sth matched: sendSignalCompat(“this”, “state_triggerTransition”)
DuringState()
    OnStateEnter(): customizable
DuringState()
    OnStateExit(): customizable
*/
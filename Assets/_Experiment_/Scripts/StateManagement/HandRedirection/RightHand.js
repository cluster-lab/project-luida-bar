const stateEnterActions = {
    2: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', true);
        } }
    ],
    3: [
        { type: "exec", action: (deltaTime) => {
            $.state.originPos = $.getPosition().clone();
            $.log('gain: ' + CONDITION['gain']);
        } }
    ],
    6: [
        { type: "exec", action: (deltaTime) => {
            $.sendSignalCompat('this', 'exp_uploadCustomData');
        } }
    ]
};

const duringStateActions = {
    2: [
        { type: "exec", action: (deltaTime) => {
            $.setPosition(PARTICIPANTS[1].getHumanoidBonePosition(HumanoidBone.RightHand));
            $.setRotation(PARTICIPANTS[1].getHumanoidBoneRotation(HumanoidBone.RightHand));
        } }
    ],
    3: [
        { type: "exec", action: (deltaTime) => {
            $.setPosition($.state.originPos.clone()
              .add(PARTICIPANTS[1].getHumanoidBonePosition(HumanoidBone.RightHand).clone()
                .sub($.state.originPos)
                .multiplyScalar(CONDITION['gain'] || 1)));
            $.setRotation(PARTICIPANTS[1].getHumanoidBoneRotation(HumanoidBone.RightHand));
        } }
    ],
    5: [
        { type: "exec", action: (deltaTime) => {
            $.setPosition(PARTICIPANTS[1].getHumanoidBonePosition(HumanoidBone.RightHand));
            $.setRotation(PARTICIPANTS[1].getHumanoidBoneRotation(HumanoidBone.RightHand));
        } }
    ]
};

const stateExitActions = {
    3: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', false);
        } }
    ]
};


// function Start() { }
// function Update(deltaTime) { }
// $.onCollide((collision) => { });
// $.onGrab((isGrab, isLeftHand, player) => { });
// $.onInteract((player) => { });
// $.onUse((isDown, player) => { });
// $.onPhysicsUpdate((deltaTime) => { });
// $.onReceive((messageType, arg, sender) => { });
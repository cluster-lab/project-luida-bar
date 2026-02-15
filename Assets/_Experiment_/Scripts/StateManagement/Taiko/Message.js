const stateEnterActions = {
    1: [
        { type: "exec", action: (deltaTime) => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`《実験説明》
            本実験の所要時間は約30分です。
            最初に約15秒間、太鼓を叩く練習を行っていただきます。
            その後、別のワールドに移動し、アバターが割り当てられます。
            そこでは、周囲の参加者とともに太鼓を演奏していただきます。
            演奏は1回あたり100秒で、各回の間に30秒間の休憩を挟みます。
            これを計6回繰り返します。すべての演奏が終了したら、
            アンケートにご回答いただき、退室していただきます。
            （まもなく練習に入ります...）`);
        } }
    ],
    2: [
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`《練習時間》
            太鼓のバチを掴んで、
            目の前に現れた太鼓を
            練習として30秒間叩いてください。`);
        } }
    ],
    3: [
        { type: "exec", action: (deltaTime) => {
            $.subNode('Text').setText(`← 男性　　女性 →
            ご自身の性別に合った側の
            ポータルへ進んでください
            「その他」の方は、いずれかをお選びください`);
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
};


// function Start() { }
// function Update(deltaTime) { }
// $.onCollide((collision) => { });
// $.onGrab((isGrab, isLeftHand, player) => { });
// $.onInteract((player) => { });
// $.onUse((isDown, player) => { });
// $.onPhysicsUpdate((deltaTime) => { });
// $.onReceive((messageType, arg, sender) => { });
const stateEnterActions = {
    0: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
    3: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: () => {
            if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
        } },
    ],
    4: [
        { type: "exec", action: () => {
            // 原点の座標を計算する
            $.state.originPos = $.state.player
              .getHumanoidBonePosition(HumanoidBone.Head).clone()
              .add(new Vector3(0, -0.3, 0.3));
            
            // 練習用のゲインを決める
            if (!$.state.practiceID) $.state.practiceID = 0;
            $.state.gain = practiceGains[$.state.practiceID];
        } },
    ],
    8: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
    ],
    9: [
        { type: "exec", action: () => {
            // 原点の座標を計算する
            $.state.originPos = $.state.player
              .getHumanoidBonePosition(HumanoidBone.Head).clone()
              .add(new Vector3(0, -0.3, 0.3));
        } },
    ],
};

const duringStateActions = {
    3: [
        { type: "exec", action: () => {
            // バーチャルハンドの座標 = 実際の右手の座標
            $.setPosition($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand));
            $.setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand));
        } },
    ],
    4: [
        { type: "exec", action: () => {
            // バーチャルハンドの座標 = 原点の座標 + ゲイン×(実際の右手の座標 - 原点の座標)
            let displacement = $.state.player
              .getHumanoidBonePosition(HumanoidBone.RightHand).clone()
              .sub($.state.originPos);
            $.setPosition(
              $.state.originPos.clone()
                .add(displacement.multiplyScalar($.state.gain))
            );
            $.setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand));
        } },
    ],
    8: [
        { type: "exec", action: () => {
            // バーチャルハンドの座標 = 実際の右手の座標
            $.setPosition($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand));
            $.setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand));
        } },
    ],
    9: [
        { type: "exec", action: () => {
            // バーチャルハンドの座標 = 原点の座標 + ゲイン×(実際の右手の座標 - 原点の座標)
            // ゲインは試行ごとの実験条件の値を使う
            let displacement = $.state.player
              .getHumanoidBonePosition(HumanoidBone.RightHand).clone()
              .sub($.state.originPos);
            $.setPosition(
              $.state.originPos.clone()
                .add(displacement.multiplyScalar(CONDITION["gain"]))
            );
            $.setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand));
        } },
    ],
};

const stateExitActions = {
    4: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
        { type: "exec", action: () => {
            $.state.practiceID += 1;
        } },
    ],
    9: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
};

const practiceGains = [1, 0.75, 1.25];
const stateEnterActions = {
    0: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
    3: [
        { type: "exec", action: () => {
            // 原点より30cm先の座標に配置する
            if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
            let position = $.state.player
                .getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.6));
            $.setPosition(position);
        } },
    ],
    4: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
    ],
    8: [
        { type: "exec", action: () => {
            // 原点より30cm先の座標に配置する
            if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
            let position = $.state.player
                .getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.6));
            $.setPosition(position);
        } },
    ],
    9: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
    ],
};

const duringStateActions = {
};

const stateExitActions = {
    4: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
    9: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
};

$.onCollide(collision => {
  if (collision.handle?.type === "item") { // 衝突対象が別のアイテム（e.g., バーチャルハンド）であれば
    ToNextState(); // 次のステートへ遷移させる
  }
});
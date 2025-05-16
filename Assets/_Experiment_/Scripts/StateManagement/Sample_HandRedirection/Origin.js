const stateEnterActions = {
    0: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
    3: [
        { type: "exec", action: () => {
            // 参加者の手前に配置する
            if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
            let position = $.state.player.getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.3));
            $.setPosition(position);
        } },
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
    ],
    8: [
        { type: "exec", action: () => {
            // 参加者の手前に配置する
            if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
            let position = $.state.player.getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.3));
            $.setPosition(position);
        } },
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
    ],
};

const duringStateActions = {
};

const stateExitActions = {
    3: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
    8: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
};

$.onCollide(collision => {
  if (collision.handle?.type === "item") {
    ToNextState();
  }
});
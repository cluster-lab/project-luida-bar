function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 3) {
    // 原点より30cm先の座標に配置する
if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
let position = $.state.player
    .getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.6));
$.setPosition(position);
  }
  if (STATE_ID === 4) {
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 8) {
    // 原点より30cm先の座標に配置する
if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
let position = $.state.player
    .getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.6));
$.setPosition(position);
  }
  if (STATE_ID === 9) {
    $.setStateCompat('this', 'exp_showItem', true);
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 4) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 9) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}


$.onCollide(collision => {
  if (collision.handle?.type === "item") { // 衝突対象が別のアイテム（e.g., バーチャルハンド）であれば
    ToNextState(); // 次のステートへ遷移させる
  }
});

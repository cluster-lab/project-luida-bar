function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 3) {
    $.setStateCompat('this', 'exp_showItem', true);
    if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
  }
  if (STATE_ID === 4) {
    // 原点の座標を計算する
$.state.originPos = $.state.player
  .getHumanoidBonePosition(HumanoidBone.Head).clone()
  .add(new Vector3(0, -0.3, 0.3));

// 練習用のゲインを決める
if (!$.state.practiceID) $.state.practiceID = 0;
$.state.gain = practiceGains[$.state.practiceID];
  }
  if (STATE_ID === 8) {
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 9) {
    // 原点の座標を計算する
$.state.originPos = $.state.player
  .getHumanoidBonePosition(HumanoidBone.Head).clone()
  .add(new Vector3(0, -0.3, 0.3));
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
    // バーチャルハンドの座標 = 実際の右手の座標
$.setPosition($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand));
$.setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand));
  }
  if (STATE_ID === 4) {
    // バーチャルハンドの座標 = 原点の座標 + ゲイン×(実際の右手の座標 - 原点の座標)
let displacement = $.state.player
  .getHumanoidBonePosition(HumanoidBone.RightHand).clone()
  .sub($.state.originPos);
$.setPosition(
  $.state.originPos.clone()
    .add(displacement.multiplyScalar($.state.gain))
);
$.setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand));
  }
  if (STATE_ID === 8) {
    // バーチャルハンドの座標 = 実際の右手の座標
$.setPosition($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand));
$.setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand));
  }
  if (STATE_ID === 9) {
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
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 4) {
    $.setStateCompat('this', 'exp_showItem', false);
    $.state.practiceID += 1;
  }
  if (STATE_ID === 9) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}


const practiceGains = [1, 0.75, 1.25];

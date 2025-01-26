function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
    $.state.practiceGains = [1, 0.75, 1.25];
$.state.practiceGainID = 0;
  }
  if (STATE_ID === 3) {
    Reset();
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 7) {
    Reset();
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 10) {
    $.sendSignalCompat('this', 'exp_uploadCustomData');
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
    UpdateHandTransform($.state.practiceGains[$.state.practiceGainID]);
  }
  if (STATE_ID === 7) {
    UpdateHandTransform(parseFloat(CONDITION["gain"]));
  }
  if (STATE_ID === 5) {
    UpdateHandTransform($.state.practiceGains[$.state.practiceGainID]);
  }
  if (STATE_ID === 9) {
    UpdateHandTransform(parseFloat(CONDITION["gain"]));
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 7) {
    $.setStateCompat('this', 'exp_showItem', false);
    $.sendSignalCompat('this', 'exp_recordCustomData');
  }
  if (STATE_ID === 5) {
    $.state.practiceGainID = $.state.practiceGainID + 1;
  }
}


function Reset() { // リセット処理
  // プレイヤーの初期状態を設定
  $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
  $.state.handOriginalPos = $.subNode("RightHandAnchor").getPosition().clone();

  // プレイヤーの実際の手（コントローラー）と仮想手の回転のずれを補正するためのオフセットを設定
  $.state.handOffset = new Quaternion().setFromEulerAngles(new Vector3(0, 90, 0));

  // 原点 (StartingPoint) を表示し、ターゲット (TargetPoint) を非表示にする
  $.subNode("StartingPoint").setEnabled(true);
  $.subNode("TargetPoint").setEnabled(false);

  // プレイヤーの頭部位置を基準に、原点を頭の30cm手前＋30cm下に配置
  $.subNode("StartingPoint").setPosition(
    $.state.player.getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.3)));

  // プレイヤーの頭部位置を基準に、ターゲットを頭の60cm手前＋30cm下に配置
  $.subNode("TargetPoint").setPosition(
    $.state.player.getHumanoidBonePosition(HumanoidBone.Head).clone().add(new Vector3(0, -0.3, 0.6)));
}

// 右手アンカー（右手オブジェクトのParent Constraintsが参照しているオブジェクト）の位置を、
// プレイヤーの右手位置に同期させ、さらにゲインを適用する
function UpdateHandTransform(gain) {
  if (!$.state.player || !$.state.handOriginalPos) return;

  // プレイヤーの右手位置とオリジナル位置の差分にゲインを掛け、右手アンカーを移動
  $.subNode("RightHandAnchor").setPosition(
    $.state.handOriginalPos.clone()
      .add($.state.player.getHumanoidBonePosition(HumanoidBone.RightHand).clone()
        .sub($.state.handOriginalPos).multiplyScalar($.state.isReaching ? gain : 1)));

  // プレイヤーの右手の回転に補正オフセットを適用して、右手アンカーを回転
  $.subNode("RightHandAnchor")
    .setRotation($.state.player.getHumanoidBoneRotation(HumanoidBone.RightHand)
      .clone().multiply($.state.handOffset));
}

// 衝突イベントが発生した際に実行される処理
$.onCollide(collision => {
  // 衝突対象が存在しない、または "RightHand" オブジェクトではない場合は処理を中断
  if (!collision.handle || !$.worldItemReference("RightHand") || collision.handle.id !== $.worldItemReference("RightHand").id) return;

  if ($.state.isReaching) {
    // すでにリーチング状態の場合、リーチングを終了する
    $.state.isReaching = false;
    $.subNode("TargetPoint").setEnabled(false);
    ToNextState(); // 次の状態へ遷移
  } else {
    // リーチングが開始されていない場合、ターゲットを表示し、原点を非表示にする
    $.subNode("StartingPoint").setEnabled(false);
    $.subNode("TargetPoint").setEnabled(true);
    // 現在の右手アンカーの位置を記録
    $.state.handOriginalPos = $.subNode("RightHandAnchor").getPosition().clone();
    $.state.isReaching = true;
  }
});

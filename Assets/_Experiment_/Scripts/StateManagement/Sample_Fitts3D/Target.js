function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 4) {
    if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];

// 練習の回数を初期化する
if (!$.state.practiceID) $.state.practiceID = 0;

// ターゲットの座標を設定する
let param = practiceParams[$.state.practiceID];
let originPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head)
.clone().add(new Vector3(0, 0, 0.5));
$.setPosition(originPos.add(new Vector3(param.x / 300, param.y / 300, param.z / 300)));

// ターゲットのサイズを設定する
let s = param.s / 300;
$.getUnityComponent("Transform").unityProp.localScale = new Vector3(s, s, s);
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 8) {
    if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];

// xy距離の実験条件から、目標オブジェクトのxy座標を候補からランダムに決める
let xyCoord = xyCoordCandidates[parseInt(CONDITION["xy"])][Math.floor(Math.random() * 4)];

// 目標オブジェクトを指定された位置まで動かす
let x = xyCoord.x / 300;
let y = xyCoord.y / 300;
let z = parseInt(CONDITION["d"]) / 300;
let originPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head)
.clone().add(new Vector3(0, 0, 0.5));
$.setPosition(originPos.add(new Vector3(x, y, z)));

// 目標オブジェクトのサイズを実験条件に合わせる
let s = parseInt(CONDITION["s"]) / 300;
$.getUnityComponent("Transform").unityProp.localScale = new Vector3(s, s, s);

// xとyの値を保存する
$.setStateCompat("owner", "x", xyCoord.x);
$.setStateCompat("owner", "y", xyCoord.y);

// タスク時間のタイマーを初期化する
$.state.taskTime = 0;
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 10) {
    $.sendSignalCompat('this', 'exp_uploadCustomData');
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 8) {
    $.state.taskTime = $.state.taskTime + deltaTime; // タスク時間のタイマー
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 4) {
    $.setStateCompat('this', 'exp_showItem', false);
    $.state.practiceID = $.state.practiceID + 1;
  }
  if (STATE_ID === 8) {
    $.setStateCompat('this', 'exp_showItem', false);
    $.setStateCompat("owner", "taskTime", $.state.taskTime); // タスク時間を保存する
    $.sendSignalCompat('this', 'exp_recordCustomData');
  }
}


const practiceParams = [
{ x: -50, y: 50, z: 0, s: 16 },
{ x: 10, y: 10, z: -25, s: 2 },
{ x: 30, y: -30, z: 50, s: 48 }
]; // 練習用の実験条件

const xyCoordCandidates = [
[{ x: 10, y: 10 }, { x: -10, y: 10 }, { x: 10 , y: -10 }, { x: -10, y: -10 }],
[{ x: 30, y: 0 }, { x: 0 , y: 30 }, { x: -30, y: 0 }, { x: 0 , y: -30 }],
[{ x: 30, y: 30 }, { x: -30, y: 30 }, { x: 30 , y: -30 }, { x: -30, y: -30 }],
[{ x: 50, y: 0 }, { x: 0 , y: 50 }, { x: -50, y: 0 }, { x: 0 , y: -50 }],
[{ x: 50, y: 50 }, { x: -50, y: 50 }, { x: 50 , y: -50 }, { x: -50, y: -50 }]
]; // 実験条件"xy"が対応している実際のxとyの座標

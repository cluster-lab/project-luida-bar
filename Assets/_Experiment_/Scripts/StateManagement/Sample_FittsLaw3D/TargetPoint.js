function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 3) {
    if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];
if (!$.state.practiceID) $.state.practiceID = 0;

// ターゲットの座標
let param = practiceParams[$.state.practiceID];
let originPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head)
  .clone().add(new Vector3(0, 0, 0.5));
$.setPosition(originPos.add(new Vector3(param.x / 300, param.y / 300, param.z / 300)));

// ターゲットのサイズ
let s = param.s / 300;
$.getUnityComponent("Transform").unityProp.localScale = new Vector3(s, s, s);

// 色を半透明に
$.material("mat").setBaseColor(1, 0, 0, 0.7);
    $.setStateCompat('this', 'exp_showItem', true);
  }
  if (STATE_ID === 6) {
    if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0];

// xy距離の実験条件からxy座標をランダムに決める
let xyCoord = xyCoordCandidates[parseInt(CONDITION["xy"])][Math.floor(Math.random() * 4)];

// ターゲットを指定された位置まで動かす
let x = xyCoord.x / 300;
let y = xyCoord.y / 300;
let z = parseInt(CONDITION["d"]) / 300;
let originPos = $.state.player.getHumanoidBonePosition(HumanoidBone.Head)
  .clone().add(new Vector3(0, 0, 0.5));
$.setPosition(originPos.add(new Vector3(x, y, z)));

// ターゲットのサイズを実験条件に合わせる
let s = parseInt(CONDITION["s"]) / 300;
$.getUnityComponent("Transform").unityProp.localScale = new Vector3(s, s, s);

// xとyの値を保存する
$.setStateCompat("owner", "x", xyCoord.x);
$.setStateCompat("owner", "y", xyCoord.y);

// 色を半透明に
$.material("mat").setBaseColor(1, 0, 0, 0.7);
    $.setStateCompat('this', 'exp_showItem', true);
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
    // ホバー時は不透明にし、非ホバー時は半透明にする
if ($.state.isHovered !== $.getStateCompat("this", "isHovered", "boolean")) {
  $.material("mat").setBaseColor(1, 0, 0, $.state.isHovered ? 0.7 : 1);
  $.state.isHovered = !$.state.isHovered;
  $.log($.state.isHovered);
}
  }
  if (STATE_ID === 6) {
    // ホバー時は不透明にし、非ホバー時は半透明にする
if ($.state.isHovered !== $.getStateCompat("this", "isHovered", "boolean")) {
  $.material("mat").setBaseColor(1, 0, 0, $.state.isHovered ? 0.7 : 1);
  $.state.isHovered = !$.state.isHovered;
}
  }
}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 3) {
    $.setStateCompat('this', 'exp_showItem', false);
    $.state.practiceID = $.state.practiceID + 1;
  }
  if (STATE_ID === 6) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}


const practiceParams = [
  { x: -50, y: 50, z: 0, s: 16 },
  { x: 10, y: 10, z: -25, s: 2 },
  { x: 30, y: -30, z: 50, s: 48 }
];

const xyCoordCandidates = [
   [{ x: 10, y: 10 }, { x: -10, y: 10 }, { x: 10 , y: -10 }, { x: -10, y: -10 }],
   [{ x: 30, y: 0  }, { x: 0  , y: 30 }, { x: -30, y: 0   }, { x: 0  , y: -30 }],
   [{ x: 30, y: 30 }, { x: -30, y: 30 }, { x: 30 , y: -30 }, { x: -30, y: -30 }],
   [{ x: 50, y: 0  }, { x: 0  , y: 50 }, { x: -50, y: 0   }, { x: 0  , y: -50 }],
   [{ x: 50, y: 50 }, { x: -50, y: 50 }, { x: 50 , y: -50 }, { x: -50, y: -50 }]
];

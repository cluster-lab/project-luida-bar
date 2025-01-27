function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`これより30秒間、鏡を見ながら、
準備体操などをして体を動かしましょう。
「開始」ボタンを押して始めてください。
`);
  }
  if (STATE_ID === 2) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`これより4分間、
時々鏡やご自身の腕を見ながら、
先程の動画の中の演奏者と同様の気分で
ジャンベドラムの演奏を行っていただきます。
それでは「開始」ボタンをクリックして
演奏を始めてください。`);
  }
  if (STATE_ID === 7) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`お疲れ様です。いい演奏でした！
最後にアンケートにご記入ください。`);
  }
  if (STATE_ID === 10) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`実験は以上になります。
ご参加いただきありがとうございました！
謝礼は後日に付与します。
目の前にあるポータルに潜って
退室してください。`);
$.setStateCompat("owner", "exp_participantMoveSpeed", 1);
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 2) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 7) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




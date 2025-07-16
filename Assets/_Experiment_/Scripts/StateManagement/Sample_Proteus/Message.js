function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`本実験では、
アフリカンジャンベドラムという楽器を用いて、
4分間のセッションを2回演奏していただきます。
まず、雰囲気をつかんでいただくために、
ジャンベドラムの演奏動画を再生しますので、
最後までご視聴ください。`);
  }
  if (STATE_ID === 3) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`手前に現れたジャンベドラムを
試しに叩いてみてください。
その後「次へ」ボタンを押し、
先程の動画の中の演奏者と同様の気分で、
4分間の演奏を始めてください。`);
  }
  if (STATE_ID === 5) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`練習は以上になります。
お疲れ様でした。`);
  }
  if (STATE_ID === 6) {
    $.subNode("Text").setText(`目の前にあるポータルに潜って、
次のセッションに移動してください。`);
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
  if (STATE_ID === 3) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




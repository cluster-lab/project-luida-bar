function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`
Start
`);
    $.sendSignalCompat('this', 'exp_recordCustomData');
  }
  if (STATE_ID === 3) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`手前に現れたドラムを試しに叩いてみてください。
その後「次へ」ボタンを押し、先程の動画の中の演奏者と同様の気分で、
4分間の演奏を始めてください。
隣に他の人が居ても、その人に合わせる必要はありません。`);
    $.sendSignalCompat('this', 'exp_uploadCustomData');
  }
  if (STATE_ID === 5) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`練習は以上になります。
お疲れ様でした。`);
  }
  if (STATE_ID === 6) {
    $.subNode("Text").setText(`練習は以上になります。
目の前にあるポータルに潜って、
次のセッションに移動してください。`);
  }
  if (STATE_ID === 1) {
    $.sendSignalCompat('this', 'exp_recordCustomData');
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




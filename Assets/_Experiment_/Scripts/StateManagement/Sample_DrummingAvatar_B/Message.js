function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode('Text').setText(`本実験では、アフリカンジャンベドラムの演奏を
4分間のセッション×2回で行っていただきます。
まず、雰囲気をつかんでいただくために、
ドラムの演奏動画を再生しますので、
「次へ」ボタンをクリックし、
最後までご視聴ください。`);
  }
  if (STATE_ID === 3) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode('Text').setText(`手前に現れたドラムを
試しに叩いてみてください。
その後「開始」ボタンをクリックし、
先程の動画の中の演奏者と同様の気分で、
4分間の演奏を始めてください。`);
  }
  if (STATE_ID === 5) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode('Text').setText(`お疲れ様です。
しばらく休憩していただいて大丈夫です。`);
  }
  if (STATE_ID === 6) {
    $.subNode('Text').setText(`目の前にあるポータルに潜って、
次のセッションに移動してください。`);
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




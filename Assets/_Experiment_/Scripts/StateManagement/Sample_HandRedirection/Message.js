function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 2) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode('Text').setText(``);
  }
  if (STATE_ID === 3) {
    $.subNode('Text').setText(`右手の人差し指で、
目の前に現れた緑の玉に触れてください。`);
  }
  if (STATE_ID === 4) {
    $.subNode('Text').setText(`玉は30cm先まで移動しました。
もう一度右手の人差し指で触れてください。`);
  }
  if (STATE_ID === 5) {
    $.subNode('Text').setText(`Q: バーチャル空間の中の手は実身体の手より
速く動いたか？遅く動いたか？
分からない場合はどちらかのボタンを押してください`);
  }
  if (STATE_ID === 6) {
    $.subNode('Text').setText(`手を下ろしてください。
今は右手を体の前に置かないでください。`);
  }
  if (STATE_ID === 7) {
    $.subNode('Text').setText(`練習は以上になります。
ここからは本番です。
同じ手順でタスクを22回行ってください。
準備ができたら、前を見て開始ボタンを押してください`);
  }
  if (STATE_ID === 8) {
    $.subNode('Text').setText(`右手の人差し指で、
目の前に現れた緑の玉に触れてください。`);
  }
  if (STATE_ID === 9) {
    $.subNode('Text').setText(`玉は30cm先まで移動しました。
もう一度右手の人差し指で触れてください。`);
  }
  if (STATE_ID === 10) {
    $.subNode('Text').setText(`Q: バーチャル空間の中の手は実身体の手より
速く動いたか？遅く動いたか？
分からない場合はどちらかのボタンを押してください`);
  }
  if (STATE_ID === 11) {
    $.subNode('Text').setText(`手を下ろしてください。
今は右手を体の前に置かないでください。`);
  }
  if (STATE_ID === 12) {
    $.subNode('Text').setText(`タスクは以上になります。
お疲れ様です。
最後に質問紙にご記入をお願いします。
「次へ」ボタンをクリックして進んでください。`);
  }
  if (STATE_ID === 14) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode('Text').setText(`実験は以上になります。
ご参加いただきありがとうございました！`);
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 12) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




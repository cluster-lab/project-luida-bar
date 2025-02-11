function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 2) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode('Text').setText(`タスク内容は、カーソルを掴み、
ターゲットまで動かしたらボタンを押すことになります。
まずは3回練習を行い、その後本番を100回行います。
準備ができたら、設定画面でコントローラを非表示にし、
前を見て「練習」ボタンを押してください。`);
  }
  if (STATE_ID === 3) {
    $.subNode('Text').setText(`灰色のカーソルを掴み（コントローラの中指ボタンを押す）、
黄色のキューブまで動かしたら、
コントローラの人差し指ボタンを押してください。`);
  }
  if (STATE_ID === 4) {
    $.subNode('Text').setText(`カーソルを掴んだまま、
赤いキューブまで動かしたら、
コントローラの人差し指ボタンを押してください。`);
  }
  if (STATE_ID === 5) {
    $.subNode('Text').setText(`手を下ろしてください。
休憩を取ってから再開していただいて大丈夫です。`);
  }
  if (STATE_ID === 6) {
    $.subNode('Text').setText(`これからは本番の試行を100回行います。
準備ができたら前を向いて、
「開始」ボタンを押してください。`);
  }
  if (STATE_ID === 7) {
    $.subNode('Text').setText(`灰色のカーソルを掴み（コントローラの中指ボタンを押す）、
黄色のキューブまで動かしたら、
コントローラの人差し指ボタンを押してください。`);
  }
  if (STATE_ID === 8) {
    $.subNode('Text').setText(`カーソルを掴んだまま、
赤いキューブまで動かしたら、
コントローラの人差し指ボタンを押してください。`);
  }
  if (STATE_ID === 9) {
    $.subNode('Text').setText(`手を下ろしてください。
休憩を取ってから再開していただいて大丈夫です。`);
  }
  if (STATE_ID === 10) {
    $.subNode('Text').setText(`タスクは以上になります。
お疲れ様です。
最後に質問紙にご記入をお願いします。
「次へ」ボタンをクリックして進んでください。`);
  }
  if (STATE_ID === 12) {
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

  if (STATE_ID === 10) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




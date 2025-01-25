function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 2) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`タスク内容は、カーソルを掴み、
ターゲットまで動かしたらボタンを押すことになります。
まずは3回練習を行い、その後本番を96回行います。
準備ができたら、設定画面でコントローラを非表示にし、
前を見て「練習」ボタンを押してください。`);
  }
  if (STATE_ID === 3) {
    $.subNode("Text").setText(`灰色のカーソルを掴み（コントローラの中指ボタンを押す）、
黄色のリセットキューブまで動かしたら、
コントローラの人差し指ボタンを押す。
次に赤いターゲットキューブまで動かしたら、
コントローラの人差し指ボタンを押す。`);
  }
  if (STATE_ID === 4) {
    $.subNode("Text").setText(`手を下ろしてください。
休憩を取ってから再開していただいて大丈夫です。`);
  }
  if (STATE_ID === 5) {
    $.subNode("Text").setText(`これからは本番の試行を96回行います。
準備ができたら前を向いて、
「開始」ボタンを押してください。`);
  }
  if (STATE_ID === 6) {
    $.subNode("Text").setText(`灰色のカーソルを掴み（コントローラの中指ボタンを押す）、
黄色のリセットキューブまで動かしたら、
コントローラの人差し指ボタンを押す。
次に赤いターゲットキューブまで動かしたら、
コントローラの人差し指ボタンを押す。`);
  }
  if (STATE_ID === 8) {
    $.subNode("Text").setText(`手を下ろしてください。
休憩を取ってから再開していただいて大丈夫です。`);
  }
  if (STATE_ID === 9) {
    $.subNode("Text").setText(`タスクは以上になります。
お疲れ様です。
最後に質問紙にご記入をお願いします。
「次へ」ボタンをクリックして進んでください。`);
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 11) {
    $.setStateCompat('this', 'exp_showItem', true);
    $.subNode("Text").setText(`実験は以上になります。
ご参加いただきありがとうございました！
謝礼は後日に付与します。
目の前のゲートに潜って退室してください。`);
  }
}


function DuringState(deltaTime) {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}


function OnStateExit() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

}




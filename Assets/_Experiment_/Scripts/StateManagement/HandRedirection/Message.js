function OnStateEnter() {
  const STATE_ID = $.state.state_id;
  const CONDITION = $.groupState.currentCondition;

  if (STATE_ID === 0) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
  if (STATE_ID === 2) {
    $.setStateCompat('this', 'exp_showItem', true);
    let sentences = ["これからは、右手の人差し指で緑の玉を触って、",
"質問に答える、というタスクを行っていただきます。",
"まずは3回練習しましょう。",
"準備ができたら、設定画面でコントローラを非表示にし、",
"前を見て「開始」ボタンを押してください。"];
$.subNode("Text").setText(sentences.join("\n"));
  }
  if (STATE_ID === 3) {
    let sentences = ["右手の人差し指で、",
"目の前に現れた緑の玉に触れてください。",
"玉は触れられたら30cm先まで移動します。",
"移動後の玉にもう一度触れてください。"];
$.subNode("Text").setText(sentences.join("\n"));
  }
  if (STATE_ID === 4) {
    let sentences = ["Q: バーチャル空間の中の手は実身体の手より",
"速く動いたか？遅く動いたか？",
"分からない場合はどちらかのボタンを押してください"];
$.subNode("Text").setText(sentences.join("\n"));
  }
  if (STATE_ID === 5) {
    $.subNode("Text").setText("手をおろしてください");
  }
  if (STATE_ID === 6) {
    let sentences = ["練習は以上になります。",
"ここからは本番です。",
"同じ手順でタスクを22回行ってください。",
"準備ができたら、前を見て開始ボタンを押してください"];
$.subNode("Text").setText(sentences.join("\n"));
  }
  if (STATE_ID === 7) {
    let sentences = ["右手の人差し指で、",
"目の前に現れた緑の玉に触れてください。",
"玉は触れられたら30cm先まで移動します。",
"移動後の玉にもう一度触れてください。"];
$.subNode("Text").setText(sentences.join("\n"));
$.log(CONDITION["gain"]);
  }
  if (STATE_ID === 8) {
    let sentences = ["Q: バーチャル空間の中の手は実身体の手より",
"速く動いたか？遅く動いたか？",
"分からない場合はどちらかのボタンを押してください"];
$.subNode("Text").setText(sentences.join("\n"));
  }
  if (STATE_ID === 9) {
    $.subNode("Text").setText("手をおろしてください");
  }
  if (STATE_ID === 10) {
    let sentences = ["タスクは以上になります。",
"お疲れ様です。",
"最後に質問紙にご記入をお願いします。",
"「次へ」ボタンをクリックして進んでください。"];
$.subNode("Text").setText(sentences.join("\n"));
  }
  if (STATE_ID === 12) {
    $.setStateCompat('this', 'exp_showItem', true);
    let sentences = ["実験は以上になります。",
"ご参加いただきありがとうございました。",
"目の前のゲートに潜って退室してください。",
"謝礼は後日に付与します。"];
$.subNode("Text").setText(sentences.join("\n"));
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
    
$.setStateCompat("owner", "exp_participantMoveSpeed", 1);
  }
  if (STATE_ID === 10) {
    $.setStateCompat('this', 'exp_showItem', false);
  }
}




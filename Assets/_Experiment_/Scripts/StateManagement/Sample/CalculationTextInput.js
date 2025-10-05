const stateEnterActions = {
    2: [
        { type: "exec", action: (deltaTime) => {
            PARTICIPANTS[1].requestTextInput(
              "ask_to_calculate",
              getRandomInt(100) + "+" + getRandomInt(100) + "=?"
            );
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
};


function getRandomInt(max) { // 乱数の整数を生成する関数を定義する
  return Math.floor(Math.random() * max);
}
$.onTextInput((text, meta, status) => {
  if (status === TextInputStatus.Success) {
    ToNextState(); // 参加者からのテキスト入力を受け付けたら次のステートへ遷移させる
    // メモ：ただ計算させるだけなので、正解かどうかを確認しない。確認したい場合はご自身でスクリプトを編集してください。
  }
});
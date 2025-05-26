const stateEnterActions = {
    0: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
    2: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: () => {
            $.subNode('Text').setText(`これからは、右手の人差し指で緑の玉を触って、
            質問に答える、というタスクを行っていただきます。
            まずは何回か練習しましょう。
            準備ができたら、設定画面でコントローラを非表示にし、
            前を見て「開始」ボタンを押してください。`);
        } },
    ],
    3: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`右手の人差し指で、
            目の前に現れた緑の玉に触れてください。`);
        } },
    ],
    4: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`玉は30cm先まで移動しました。
            もう一度右手の人差し指で触れてください。`);
        } },
    ],
    5: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`Q: バーチャル空間の中の手は実身体の手より
            速く動いたか？遅く動いたか？
            分からない場合はどちらかのボタンを押してください`);
        } },
    ],
    6: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`手を下ろしてください。
            今は右手を体の前に置かないでください。`);
        } },
    ],
    7: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`練習は以上になります。
            ここからは本番です。
            同じ手順でタスクを22回行ってください。
            準備ができたら、前を見て開始ボタンを押してください`);
        } },
    ],
    8: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`右手の人差し指で、
            目の前に現れた緑の玉に触れてください。`);
        } },
    ],
    9: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`玉は30cm先まで移動しました。
            もう一度右手の人差し指で触れてください。`);
        } },
    ],
    10: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`Q: バーチャル空間の中の手は実身体の手より
            速く動いたか？遅く動いたか？
            分からない場合はどちらかのボタンを押してください`);
        } },
    ],
    11: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`手を下ろしてください。
            今は右手を体の前に置かないでください。`);
        } },
    ],
    12: [
        { type: "exec", action: () => {
            $.subNode('Text').setText(`タスクは以上になります。
            お疲れ様です。
            最後に質問紙にご記入をお願いします。
            「次へ」ボタンをクリックして進んでください。`);
        } },
    ],
    14: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', true);
        } },
        { type: "exec", action: () => {
            $.subNode('Text').setText(`実験は以上になります。
            ご参加いただきありがとうございました！`);
        } },
    ],
};

const duringStateActions = {
};

const stateExitActions = {
    12: [
        { type: "exec", action: () => {
            $.setStateCompat('this', 'exp_showItem', false);
        } },
    ],
};


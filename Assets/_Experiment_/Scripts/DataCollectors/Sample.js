return {
    d: CONDITION['depth'], // 該当試行のdepth条件（参加者間変数depthの該当試行における値）
    req: CONDITION['request'], // 該当試行のrequest条件（参加者内変数requestの該当試行における値）
    font: CONDITION['font'], // 該当試行のfont条件（参加者内変数fontの該当試行における値）
    text: CONDITION['text'], // 該当試行のtext条件（参加者内変数textの該当試行における値）
    ans: $.getStateCompat('this', 'isRed', 'boolean') ? "R" : "B",　// 回答（赤か青）
    time: COLLECTED_DATA['timer']　// 答えるのに使った時間
};
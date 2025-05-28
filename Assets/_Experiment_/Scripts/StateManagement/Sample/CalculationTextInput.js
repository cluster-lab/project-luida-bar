const stateEnterActions = {
    2: [
        { type: "exec", action: () => {
            let player = $.getPlayersNear(
              new Vector3(0,0,0), Infinity)[0];
            player.requestTextInput(
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


function getRandomInt(max) {
  return Math.floor(Math.random() * max);
}
$.onTextInput((text, meta, status) => {
  if (status === TextInputStatus.Success) {
    ToNextState();
  }
});
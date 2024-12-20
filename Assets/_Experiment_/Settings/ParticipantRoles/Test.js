const roleSettings = [
  { role: "role1", number: 2 },
];

$.onStart(() => {
  $.groupState.roles = [];
  $.groupState.rolePlayers = [];
  $.groupState.playersByRole = {};
})

$.onUpdate(() => {
  if ($.groupState.rolePlayers.length <= 0 && $.getStateCompat("global", "state_currentID", "integer") > 0) {
    initRoles();
  }
})

function initRoles() {
  randomInitRoles();
  // TODO: allow initializing roles by customized calculation
}

function randomInitRoles() {
  const players = $.getPlayersNear($.getPosition(), Infinity);
  shuffle(players);
  const roles = [];
  const rolePlayers = [];
  const playersByRole = {};

  let startIndex = 0;
  roleSettings.forEach(roleSetting => {
    roles.push(roleSetting.role);
    const endIndex = startIndex + roleSetting.number;
    const playersOfThisRole = players.slice(startIndex, endIndex);
    rolePlayers.push(playersOfThisRole);
    playersByRole[roleSetting.role] = playersOfThisRole;
    startIndex = endIndex;
  });

  $.groupState.roles = roles;
  $.groupState.rolePlayers = rolePlayers;
  $.groupState.playersByRole = playersByRole;
}

function shuffle(array) {
  for (let i = array.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [array[i], array[j]] = [array[j], array[i]];
  }
}

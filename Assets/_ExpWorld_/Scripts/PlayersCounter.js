const worldID = "67a71729-1985-4b57-bac5-72ff96d1b9b4";
const countPlayersInterval = 60;

$.onStart(() => {
    $.state.timer = 0;
    $.state.lastPlayersCount = 0;
    $.state.currentPlayersCount = 0;
})

$.onUpdate(() => {
    if ($.state.timer > countPlayersInterval) {
        $.state.timer = 0;
        updatePlayersCount();
    } else {
        $.state.timer = $.state.timer + 1;
    }
})

function updatePlayersCount () {
    $.state.lastPlayersCount = $.state.currentPlayersCount;
    $.state.currentPlayersCount = $.getPlayersNear($.getPosition().clone(), 100);
    if ($.state.lastPlayersCount !== $.state.currentPlayersCount) {
        $.callExternal(JSON.stringify({ type: "updatePlayersCount", expIdentifier: worldID, playersCount: $.state.currentPlayersCount }), "playersCountUpdated");
    }
}
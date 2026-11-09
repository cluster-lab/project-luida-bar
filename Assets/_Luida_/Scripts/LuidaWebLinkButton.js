// LUIDA 公式サイトへのリンクボタン（Item Script）
// クリックしたプレイヤーに、このアイテムの Player Script（LuidaWebLinkPlayer.js）を設定する。
// Player Script は設定されるたびに新しく起動し直すので、押すたびに URL が開く。
$.onInteract((player) => {
    $.setPlayerScript(player);
});

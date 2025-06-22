const haptics = _.hapticsHandle;

_.onReceive((id, body, sender) => {
    switch (id) {
        case "haptics":
            if (haptics.isAvailable()) {
                const effect = new HapticsEffect();
                effect.frequency = body.frequency;
                effect.amplitude = body.amplitude;
                effect.duration = body.duration;
                haptics.playEffect(effect, body.target);
            }
            break;
        case "initializeParticipant":
            _.setMoveSpeedRate(1);
            _.sendTo(sender, "envInfoResponse", {
                isAndroid: _.isAndroid,
                isDesktop: _.isDesktop,
                isIos: _.isIos,
                isMacOs: _.isMacOs,
                isMobile: _.isMobile,
                isVr: _.isVr,
                isWindows: _.isWindows
            });
            break;
        default:
            break;
    }
});

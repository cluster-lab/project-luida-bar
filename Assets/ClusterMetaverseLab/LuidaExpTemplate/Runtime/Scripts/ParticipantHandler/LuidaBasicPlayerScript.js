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
        default:
            break;
    }
});

const stateEnterActions = {
    1: [
        { type: "exec", action: (deltaTime) => {
            PARTICIPANTS[1].setPosition(new Vector3(0, 0, -2));
        } },
        { type: "exec", action: (deltaTime) => {
            $.worldItemReference('LUIDA-AvatarSpawner').send('luida_unassign_avatar', { participantIndex: 1 });
        } },
        { type: "sleep", value: 3 },
        { type: "exec", action: (deltaTime) => {
            if (CONDITION['avatar'] === 'elder') {
              $.worldItemReference('LUIDA-AvatarSpawner').send('luida_assign_avatar', { avatarID: 'ElderAvatar', participantIndex: 1 });
            }
        } },
        { type: "exec", action: (deltaTime) => {
            if (CONDITION['avatar'] === 'young') {
              $.worldItemReference('LUIDA-AvatarSpawner').send('luida_assign_avatar', { avatarID: 'YoungAvatar', participantIndex: 1 });
            }
        } }
    ]
};

const duringStateActions = {
};

const stateExitActions = {
    2: [
        { type: "exec", action: (deltaTime) => {
            $.worldItemReference('LUIDA-AvatarSpawner').send('luida_unassign_avatar', { participantIndex: 1 });
        } }
    ]
};
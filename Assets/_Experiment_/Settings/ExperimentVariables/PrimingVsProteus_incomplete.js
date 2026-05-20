const trialsCountForEachUniqueCondition = 1;
const within_subjects_variables = [
];
const between_subjects_variables = [
    { name: "avatar", values: ["young", "elder"], isRandom: true, debugValue: null },
    { name: "video", values: ["young", "elder"], isRandom: true, debugValue: null },
];
const state_names = ["Start", "Avatar", "Video", "Trial - Start", "Trial - Rest", "Outro", "End"];

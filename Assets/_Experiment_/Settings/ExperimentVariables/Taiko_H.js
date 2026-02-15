const trialsCountForEachUniqueCondition = 1;
const within_subjects_variables = [
    { name: "otherAvatar", values: ["happi", "random"], isRandom: true },
    { name: "number", values: ["3", "25", "100"], isRandom: true },
];
const between_subjects_variables = [
    { name: "selfAvatar", values: ["happi"], isRandom: true, debugValue: null },
];
const state_names = ["Acclimatization", "AvatarQuestionnaire", "Prepare", "Trial - Start", "Trial - Questionnaire", "Trial - Rest", "Outro", "End"];

const trialsCountForEachUniqueCondition = 2;
const within_subjects_variables = [
    { name: "question", values: ["font", "meaning"], isRandom: false },
    { name: "word", values: ["R", "G", "B"], isRandom: true },
];
const between_subjects_variables = [
    { name: "lang", values: ["ja", "en"], isRandom: false },
];

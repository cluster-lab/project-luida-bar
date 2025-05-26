const trialsCountForEachUniqueCondition = 1;
const within_subjects_variables = [
    { name: "request", values: ["material", "text"], isRandom: false },
    { name: "material", values: ["R", "B"], isRandom: true },
    { name: "text", values: ["R", "B"], isRandom: true },
];
const between_subjects_variables = [
    { name: "lang", values: ["en", "ja"], isRandom: true },
];

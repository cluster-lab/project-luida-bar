const trialsCountForEachUniqueCondition = 2;
const within_subjects_variables = [
    { name: "request", values: ["font", "meaning"], isRandom: false },
    { name: "font", values: ["R", "B"], isRandom: true },
    { name: "text", values: ["Red", "Blue"], isRandom: true },
];
const between_subjects_variables = [
    { name: "depth", values: ["near", "far"], isRandom: true, debugValue: null },
];
const state_names = ["Start", "Intro", "CalculationTask", "Trial - Start", "Trial - Rest", "Outro", "End"];

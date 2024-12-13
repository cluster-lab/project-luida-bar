function GetBetweenSubjectsCondition(questionnaireAnswersFromPreviousParticipants) {
    let betweenSubjectsCondition = {};
    try {
        between_subjects_variables.forEach(v => {
            if (v.isRandom) {
                betweenSubjectsCondition[v.name] = v.values[Math.floor(Math.random() * v.values.length)];
            } else {
                // The line below randomly assigns between-subjects condition.
                // Comment it out and add your implementation here to assign between-subjects condition
                // You can make use of the `questionnaireAnswersFromPreviousParticipants` variable in your implementation.
                betweenSubjectsCondition[v.name] = v.values[Math.floor(Math.random() * v.values.length)];

                /* Your implementation here */
            }
        });
    } catch (e) {
        $.log("Between-subjects variables are not defined. Return empty object.");
    }
    return betweenSubjectsCondition;
}
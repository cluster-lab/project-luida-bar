/*
# Available variables:
  PARTICIPANTS
    - An array of PlayerHandle of the participants joining this experiment.
    - Use `PARTICIPANTS[1]` to retrieve the first participant, `PARTICIPANTS[2]` to retrieve the second participant, etc.

# Available variables only if you have enabled the LUIDA experiment progress automation feature:
  CONDITION
    - Values are determined by your configured experimental variables and vary across trials.
    - Only available during Trial states. Use `CONDITION["variable_name"]` to reference a specific condition within the current trial.
  COLLECTED_DATA
    - The collected data you send to the LUIDA data collector using the SendDataToCollector action/function.
    - Use `COLLECTED_DATA[your_data_label]` to retrieve the value.

# Warning: Ensure returning something in the end of the code block.
    e.g., `return { score: 100 };`
    e.g., `const answer = $.getStateCompat('global', 'count', 'integer') > 5; return { isLarger: answer }`
*/
return {
    stateLog: COLLECTED_DATA ? COLLECTED_DATA["stateLog"] : "", // Include log of the current state by default (state name, id, and the timestamp when the state starts)
    cond: CONDITION || {}, // Include conditions in the collected data by default (if you have enabled the experiment automation feature)
    someCckState: $.getStateCompat("global", "someCckState", "boolean"), // Replace with your in-scene state triggered by CCK components
    foo: 'bar' // Replace with your custom collected data
};

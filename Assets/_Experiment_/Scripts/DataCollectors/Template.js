/*
# Available variables:
  PARTICIPANTS
    - An array of PlayerHandle of the participants joining this experiment.
    - Use `PARTICIPANTS[0]` to retrieve the first participant, `PARTICIPANTS[1]` to retrieve the second participant, etc.

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
return { foo: 'bar' };
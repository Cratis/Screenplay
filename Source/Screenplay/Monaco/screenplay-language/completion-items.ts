// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface CompletionEntry {
    label: string;
    insertText: string;
    documentation: string;
}

const fenced = (tag: string) => `${tag}\n\`\`\`\n\${1}\n\`\`\``;

export const topLevelItems: CompletionEntry[] = [
    { label: 'import', insertText: 'import ${1:Module}.${2:Type}', documentation: 'Imports a type from another module by its qualified name.' },
    { label: 'concept', insertText: 'concept ${1:Name} : ${2|Uuid,String,Int,Decimal,Bool,Date,DateTime|}', documentation: 'Declares a formalized value type wrapping a primitive.' },
    { label: 'concept (enum)', insertText: 'concept ${1:Name} : Enum\n    ${2:value}', documentation: 'Declares an enumeration concept with a fixed set of values.' },
    { label: 'policy', insertText: 'policy ${1:Name}\n    require ${2:authenticated}', documentation: 'Declares a named authorization rule for commands and queries.' },
    { label: 'module', insertText: 'module ${1:Name}\n    ', documentation: 'Declares the top-level namespace — maps to a bounded context.' },
];

export const moduleItems: CompletionEntry[] = [
    { label: 'layout', insertText: 'layout ${1:Name}\n    template\n        ${2:slot}', documentation: 'Declares a reusable screen template with named slots.' },
    { label: 'feature', insertText: 'feature ${1:Name}\n    ', documentation: 'Groups related slices into a vertical feature.' },
];

export const featureItems: CompletionEntry[] = [
    { label: 'feature', insertText: 'feature ${1:Name}\n    ', documentation: 'Declares a nested sub-feature.' },
    { label: 'slice StateChange', insertText: 'slice StateChange ${1:Name}\n    ', documentation: 'A command → events flow; something that changes the system.' },
    { label: 'slice StateView', insertText: 'slice StateView ${1:Name}\n    ', documentation: 'A query + projection + screen; something that reads the system.' },
    { label: 'slice Automation', insertText: 'slice Automation ${1:Name}\n    ', documentation: 'A reactor or reducer; something that reacts to events.' },
    { label: 'slice Translate', insertText: 'slice Translate ${1:Name}\n    ', documentation: 'A capture; converts external data into events.' },
];

export const sliceItems: CompletionEntry[] = [
    { label: 'event', insertText: 'event ${1:Name}\n    ${2:property} ${3:Type}', documentation: 'Declares an event type — an immutable, past-tense fact.' },
    { label: 'command', insertText: 'command ${1:Name}\n    ${2:property} ${3:Type}', documentation: 'Declares a command — an imperative intent that produces events.' },
    { label: 'query', insertText: 'query ${1:Name} => ${2:ReadModel}', documentation: 'Declares a read-side entry point mapping to a return type.' },
    { label: 'projection', insertText: 'projection ${1:Name} => ${2:ReadModel}\n    from ${3:EventType}', documentation: 'Declares how events project into a read model (PDL).' },
    { label: 'capture', insertText: 'capture ${1:Name}\n    source ${2:api}', documentation: 'Declares a change data capture converting external data into events (CDL).' },
    { label: 'reactor', insertText: 'reactor ${1:Name}\n    on ${2:EventType}', documentation: 'Declares an event reaction rule.' },
    { label: 'screen', insertText: 'screen ${1:Name}\n    data ${2:ReadModel} via query ${3:QueryName}', documentation: 'Declares a UI screen.' },
    { label: 'constraint', insertText: 'constraint ${1:Name}\n    unique ${2:property} on ${3:EventType}', documentation: 'Declares a server-side rule enforced before events are committed.' },
];

export const commandItems: CompletionEntry[] = [
    { label: 'authorize', insertText: 'authorize ${1:PolicyName}', documentation: 'References the policies that must pass for the command to execute.' },
    { label: 'validate', insertText: 'validate\n    ${1:property} not empty message "${2:message}"', documentation: 'Declarative validation rules with messages.' },
    { label: 'validate csharp', insertText: `validate ${fenced('csharp')}`, documentation: 'Imperative validation in C#, yielding ValidationError.' },
    { label: 'produces', insertText: 'produces ${1:EventType}\n    ${2:property} = ${3:source}', documentation: 'Declares the event the command emits, with property mappings.' },
    { label: 'produces when', insertText: 'produces when ${1:condition}\n    ${2:EventType}\n        ${3:property} = ${4:source}', documentation: 'Conditionally emits an event when the condition holds.' },
    { label: 'handler', insertText: 'handler\n    ', documentation: 'Fully imperative command implementation — file reference or inline C#, instead of produces.' },
];

export const producesItems: CompletionEntry[] = [
    { label: 'when', insertText: 'when ${1:condition}', documentation: 'Guards the produced event with a condition.' },
];

export const handlerItems: CompletionEntry[] = [
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Delegates the command implementation to an external C# file.' },
    { label: 'csharp', insertText: fenced('csharp'), documentation: 'Inline C# returning the events to append.' },
];

export const queryItems: CompletionEntry[] = [
    { label: 'by', insertText: 'by ${1:param} ${2:Type}', documentation: 'Declares the identifying parameter of the query.' },
    { label: 'filter', insertText: 'filter ${1:param} ${2:Type}?', documentation: 'Declares an optional filter parameter.' },
    { label: 'authorize', insertText: 'authorize ${1:PolicyName}', documentation: 'References the policies that must pass for the query to execute.' },
];

export const constraintItems: CompletionEntry[] = [
    { label: 'unique', insertText: 'unique ${1:property} on ${2:EventType}', documentation: 'Enforces a unique property value across an event type.' },
    { label: 'unique event', insertText: 'unique event ${1:EventType}', documentation: 'Enforces that the event type occurs at most once per event source.' },
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Delegates the constraint to a custom C# implementation.' },
];

export const reactorItems: CompletionEntry[] = [
    { label: 'on', insertText: 'on ${1:EventType}', documentation: 'Declares the event the reactor reacts to.' },
];

export const reactorOnItems: CompletionEntry[] = [
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Delegates the reaction to an external C# file.' },
    { label: 'csharp', insertText: fenced('csharp'), documentation: 'Inline C# returning event side effects.' },
];

export const policyItems: CompletionEntry[] = [
    { label: 'require authenticated', insertText: 'require authenticated', documentation: 'Requires an authenticated caller.' },
    { label: 'require role', insertText: 'require role "${1:role}"', documentation: 'Requires the caller to have a role.' },
    { label: 'require claim', insertText: 'require claim "${1:claim}" matches ${2:subject}', documentation: 'Requires a claim to match the subject or a value.' },
    { label: 'csharp', insertText: fenced('csharp'), documentation: 'Fully custom policy logic in C#, returning PolicyResult.' },
];

export const validateItems: CompletionEntry[] = [
    { label: 'not empty', insertText: '${1:property} not empty message "${2:message}"', documentation: 'The property must have a value.' },
    { label: 'max', insertText: '${1:property} max ${2:500} message "${3:message}"', documentation: 'Maximum length or value.' },
    { label: 'min', insertText: '${1:property} min ${2:1} message "${3:message}"', documentation: 'Minimum length or value.' },
    { label: 'matches', insertText: '${1:property} matches "${2:pattern}" message "${3:message}"', documentation: 'The property must match a regular expression.' },
    { label: 'length ==', insertText: '${1:property} length == ${2:3} message "${3:message}"', documentation: 'The property must have an exact length.' },
    { label: 'all >', insertText: '${1:collection}.${2:property} all > ${3:0} message "${4:message}"', documentation: 'Every element of a collection must satisfy the comparison.' },
];

export const screenItems: CompletionEntry[] = [
    { label: 'data', insertText: 'data ${1:ReadModel} via query ${2:QueryName}', documentation: 'Binds a read model to the screen through a query.' },
    { label: 'action', insertText: 'action ${1:CommandName}', documentation: 'Makes a command available as an action on the screen.' },
    { label: 'layout', insertText: 'layout ${1:LayoutName}', documentation: 'Uses a layout template and fills its slots.' },
    { label: 'section', insertText: 'section ${1:name}', documentation: 'A named structural section of the screen.' },
    { label: 'table', insertText: 'table ${1:name}\n    column ${2:property} label "${3:text}"', documentation: 'A table widget over a read model or collection.' },
    { label: 'summary', insertText: 'summary ${1:ReadModel}\n    field ${2:property} label "${3:text}"', documentation: 'A summary widget showing labeled fields.' },
    { label: 'title', insertText: 'title "${1:text}"', documentation: 'The title of the screen or section.' },
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Full external implementation — Stage uses the referenced file.' },
    { label: 'react', insertText: fenced('react'), documentation: 'Inline React/TSX component receiving the data contract as Props.' },
    { label: 'typescript', insertText: fenced('typescript'), documentation: 'Inline plain TypeScript.' },
    { label: 'html', insertText: fenced('html'), documentation: 'Inline static HTML.' },
];

export const actionItems: CompletionEntry[] = [
    { label: 'navigate to', insertText: 'navigate to ${1:ScreenName}', documentation: 'Navigates to a screen after the action completes.' },
    { label: 'label', insertText: 'label "${1:text}"', documentation: 'The display label of the action.' },
];

export const tableItems: CompletionEntry[] = [
    { label: 'column', insertText: 'column ${1:property} label "${2:text}"', documentation: 'A column bound to a property.' },
    { label: 'on row-click', insertText: 'on row-click navigate to ${1:ScreenName} by ${2:param}', documentation: 'Navigates when a row is clicked.' },
];

export const contextVariableItems: CompletionEntry[] = [
    { label: '$context.occurred', insertText: '$context.occurred', documentation: 'Timestamp of the event.' },
    { label: '$context.identity.id', insertText: '$context.identity.id', documentation: 'Subject of the caller from the auth token.' },
    { label: '$env.', insertText: '$env.${1:VAR_NAME}', documentation: 'An environment variable.' },
    { label: '$eventContext.occurred', insertText: '$eventContext.occurred', documentation: 'Timestamp of the event being projected (PDL).' },
];

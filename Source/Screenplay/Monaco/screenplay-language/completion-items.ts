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
    { label: 'concept (@pii with reason)', insertText: 'concept ${1:Name} : ${2|String,Uuid,Int,Decimal,Bool,Date,DateTime|} @pii\n    pii reason "${3:why this is personal data, its purpose and lawful basis}"', documentation: 'Declares a personal-data concept together with the reason it is personal data.' },
    { label: 'type', insertText: 'type ${1:Name}\n    ${2:property} ${3:Type}', documentation: 'Declares a composite value type — a named shape built from several properties.' },
    { label: 'policy', insertText: 'policy ${1:Name}\n    require ${2:authenticated}', documentation: 'Declares a named authorization rule for commands and queries.' },
    { label: 'module', insertText: 'module ${1:Name}\n    ', documentation: 'Declares the top-level namespace — maps to a bounded context.' },
];

export const conceptItems: CompletionEntry[] = [
    { label: 'pii reason', insertText: 'pii reason "${1:why this is personal data, its purpose and lawful basis}"', documentation: 'Records why the `@pii` marker applies — purpose, lawful basis, whose subject it lives under.' },
    { label: 'sensitive reason', insertText: 'sensitive reason "${1:why this value is sensitive}"', documentation: 'Records why the `@sensitive` marker applies.' },
    { label: 'validate', insertText: 'validate\n    ${1:not empty} message "${2:message}"', documentation: 'Validation rules that travel with the value everywhere it appears.' },
];

export const typeItems: CompletionEntry[] = [
    { label: 'description', insertText: 'description "${1:what this shape represents}"', documentation: 'A human-readable description of the type.' },
    { label: 'property', insertText: '${1:property} ${2:Type}', documentation: 'A property of the type — a name and a type reference.' },
];

export const moduleItems: CompletionEntry[] = [
    { label: 'layout', insertText: 'layout ${1:Name}\n    template\n        ${2:slot}', documentation: 'Declares a reusable screen template with named slots.' },
    { label: 'feature', insertText: 'feature ${1:Name}\n    ', documentation: 'Groups related slices into a vertical feature.' },
];

export const featureItems: CompletionEntry[] = [
    { label: 'feature', insertText: 'feature ${1:Name}\n    ', documentation: 'Declares a nested sub-feature.' },
    { label: 'slice StateChange', insertText: 'slice StateChange ${1:Name}\n    ', documentation: 'A command → events flow; something that changes the system.' },
    { label: 'slice StateView', insertText: 'slice StateView ${1:Name}\n    ', documentation: 'A query + projection + screen; something that reads the system.' },
    { label: 'slice Automation', insertText: 'slice Automation ${1:Name}\n    ', documentation: 'A reaction or reducer; something that runs when something happens.' },
    { label: 'slice Translate', insertText: 'slice Translate ${1:Name}\n    ', documentation: 'A capture; converts external data into events.' },
];

export const sliceItems: CompletionEntry[] = [
    { label: 'event', insertText: 'event ${1:Name}\n    ${2:property} ${3:Type}', documentation: 'Declares an event type — an immutable, past-tense fact.' },
    { label: 'command', insertText: 'command ${1:Name}\n    ${2:property} ${3:Type}', documentation: 'Declares a command — an imperative intent that produces events.' },
    { label: 'query', insertText: 'query ${1:Name} => ${2:ReadModel}', documentation: 'Declares a read-side entry point mapping to a return type.' },
    { label: 'query observable', insertText: 'query ${1:Name} => observable ${2:ReadModel}', documentation: 'Declares a live read — the query keeps pushing as the read model changes, instead of answering once.' },
    { label: 'projection', insertText: 'projection ${1:Name} => ${2:ReadModel}\n    from ${3:EventType}', documentation: 'Declares how events project into a read model (PDL).' },
    { label: 'capture', insertText: 'capture ${1:Name}\n    source ${2:api}', documentation: 'Declares a change data capture converting external data into events (CDL).' },
    { label: 'reaction', insertText: 'reaction ${1:Name}\n    when ${2:Trigger}', documentation: 'Declares behavior that runs when something happens.' },
    { label: 'screen', insertText: 'screen ${1:Name}\n    data ${2:ReadModel} via query ${3:QueryName}', documentation: 'Declares a UI screen.' },
    { label: 'constraint', insertText: 'constraint ${1:Name}\n    unique ${2:property} on ${3:EventType}', documentation: 'Declares a server-side rule enforced before events are committed.' },
];

export const commandItems: CompletionEntry[] = [
    { label: 'identifier property', insertText: '${1:property} ${2:Type} identifier', documentation: 'Marks the property a runtime resolves the event source id from. At most one per command.' },
    { label: 'authorize', insertText: 'authorize ${1:PolicyName}', documentation: 'References the policies that must pass for the command to execute.' },
    { label: 'validate', insertText: 'validate\n    ${1:property} not empty message "${2:message}"', documentation: 'Declarative validation rules with messages.' },
    { label: 'validate csharp', insertText: `validate ${fenced('csharp')}`, documentation: 'Imperative validation in C#, yielding the message of every rule the artifact breaks.' },
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
    { label: 'description', insertText: 'description "${1:what this query is trying to accomplish}"', documentation: 'What the query is for, in prose — what a generator or reviewer works from.' },
    { label: 'by', insertText: 'by ${1:param} ${2:Type}', documentation: 'Declares the identifying parameter of the query.' },
    { label: 'filter', insertText: 'filter ${1:param} ${2:Type}?', documentation: 'Declares an optional filter parameter supplied by the caller.' },
    { label: 'filter from context', insertText: 'filter ${1:param} ${2:Type} from $context.${3|tenant,causedBy.subject,occurred|}', documentation: 'Declares a parameter filled from the query context instead of the caller.' },
    { label: 'authorize', insertText: 'authorize ${1:PolicyName}', documentation: 'References the policies that must pass for the query to execute.' },
    { label: 'performer', insertText: 'performer\n    ', documentation: 'The code that performs the query — a file reference or an inline csharp/sql block.' },
];

export const performerItems: CompletionEntry[] = [
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Delegates the query implementation to an external file.' },
    { label: 'csharp', insertText: fenced('csharp'), documentation: 'Inline C# returning the query result, with the QueryContext in scope as context.' },
    { label: 'sql', insertText: fenced('sql'), documentation: 'Inline SQL returning the query result.' },
];

export const constraintItems: CompletionEntry[] = [
    { label: 'unique', insertText: 'unique ${1:property} on ${2:EventType}', documentation: 'Enforces a unique property value across an event type.' },
    { label: 'unique event', insertText: 'unique event ${1:EventType}', documentation: 'Enforces that the event type occurs at most once per event source.' },
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Delegates the constraint to a custom C# implementation.' },
];

export const reactionItems: CompletionEntry[] = [
    { label: 'description', insertText: 'description "${1:what this reaction does}"', documentation: 'What the reaction does — a complete statement of intent before any code exists.' },
    { label: 'when', insertText: 'when ${1:Trigger}', documentation: 'An event, a declared trigger or a host signal that sets the reaction off. A trigger needs no body.' },
    { label: 'every', insertText: 'every ${1:15} ${2|seconds,minutes,hours,days|}', documentation: 'Runs the reaction on an interval.' },
    { label: 'at', insertText: 'at ${1:08:00}', documentation: 'Runs the reaction at a time of day — every day unless narrowed with `on <Weekday>` or `on day <n>`.' },
    { label: 'where', insertText: 'where ${1:condition}', documentation: 'Narrows which occurrences actually run the reaction.' },
];

export const reactionTriggerItems: CompletionEntry[] = [
    { label: 'description', insertText: 'description "${1:what this reaction does}"', documentation: 'What this particular reaction does — enough on its own, with no file to point at.' },
    { label: 'produces', insertText: 'produces ${1:EventType}', documentation: 'An event the reaction appends.' },
    { label: 'invokes', insertText: 'invokes ${1:Command}', documentation: 'A command the reaction hands on. A command is asked for, not produced — it may still be rejected.' },
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Delegates the reaction to an external C# file.' },
    { label: 'csharp', insertText: fenced('csharp'), documentation: 'Inline C# returning event side effects.' },
];

export const triggerItems: CompletionEntry[] = [
    { label: 'description', insertText: 'description "${1:when this occurs}"', documentation: 'What makes an occurrence of this trigger happen.' },
    { label: 'value', insertText: '${1:name} ${2:Type}', documentation: 'A value an occurrence hands the reaction. The type is optional.' },
];

export const specificationItems: CompletionEntry[] = [
    { label: 'given', insertText: 'given ${1:EventType}\n    ${2:property} = ${3:value}', documentation: 'Establishes prior state by replaying an event before the command runs.' },
    { label: 'given readmodel', insertText: 'given readmodel ${1:ReadModelType}\n    ${2:property} = ${3:value}', documentation: 'Establishes prior read model state directly.' },
    { label: 'when', insertText: 'when ${1:CommandType}\n    ${2:property} = ${3:value}', documentation: 'The command being exercised.' },
    { label: 'then', insertText: 'then ${1:EventType}\n    ${2:property} = ${3:value}', documentation: 'An event expected to be produced by the command.' },
    { label: 'then readmodel', insertText: 'then readmodel ${1:ReadModelType}\n    ${2:property} = ${3:value}', documentation: 'The read model state expected after the command.' },
    { label: 'then error', insertText: 'then error', documentation: 'A rejection, for a reason this specification does not name.' },
    { label: 'then error "..."', insertText: 'then error "${1:reason}"', documentation: 'A rejection, for the named reason.' },
];

export const ruleItems: CompletionEntry[] = [
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Gives the named predicate its implementation in an external C# file.' },
    { label: 'csharp', insertText: fenced('csharp'), documentation: 'Gives the named predicate its implementation as inline C# returning a bool.' },
];

export const policyItems: CompletionEntry[] = [
    { label: 'require authenticated', insertText: 'require authenticated', documentation: 'Requires an authenticated caller.' },
    { label: 'require role', insertText: 'require role "${1:role}"', documentation: 'Requires the caller to have a role.' },
    { label: 'require claim', insertText: 'require claim "${1:claim}" matches ${2:subject}', documentation: 'Requires a claim to match the subject or a value.' },
    { label: 'csharp', insertText: fenced('csharp'), documentation: 'Fully custom policy logic in C#, returning a bool.' },
];

export const validateItems: CompletionEntry[] = [
    { label: 'not empty', insertText: '${1:property} not empty message "${2:message}"', documentation: 'The property must have a value.' },
    { label: 'max', insertText: '${1:property} max ${2:500} message "${3:message}"', documentation: 'Maximum length or value.' },
    { label: 'min', insertText: '${1:property} min ${2:1} message "${3:message}"', documentation: 'Minimum length or value.' },
    { label: 'matches', insertText: '${1:property} matches "${2:pattern}" message "${3:message}"', documentation: 'The property must match a regular expression.' },
    { label: 'rule', insertText: '${1:property} rule ${2:PredicateName} message "${3:message}"', documentation: 'Names a predicate — optionally followed by an indented `file` reference or inline `csharp` block giving it a body; bare, it just states that a constraint exists.' },
    { label: '==', insertText: '${1:property} == ${2:value} message "${3:message}"', documentation: 'The property must equal the value.' },
    { label: '!=', insertText: '${1:property} != ${2:value} message "${3:message}"', documentation: 'The property must not equal the value.' },
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
    { label: '$context.occurred', insertText: '$context.occurred', documentation: 'When the command or query was received.' },
    { label: '$context.tenant', insertText: '$context.tenant', documentation: 'The tenant the command or query is executing for.' },
    { label: '$context.command.', insertText: '$context.command.${1:property}', documentation: 'A property of the command being handled.' },
    { label: '$context.arguments.', insertText: '$context.arguments.${1:name}', documentation: 'An argument of the query being performed.' },
    { label: '$context.causedBy.subject', insertText: '$context.causedBy.subject', documentation: 'Subject of the identity that caused the command or query.' },
    { label: '$context.causedBy.name', insertText: '$context.causedBy.name', documentation: 'Display name of the identity that caused the command or query.' },
    { label: '$context.causedBy.userName', insertText: '$context.causedBy.userName', documentation: 'User name of the identity that caused the command or query.' },
    { label: '$context.causation.type', insertText: '$context.causation.type', documentation: 'What caused this — a command, a reactor, a schedule.' },
    { label: '$context.identity.id', insertText: '$context.identity.id', documentation: 'The identifier of the caller from the auth token.' },
    { label: '$context.identity.name', insertText: '$context.identity.name', documentation: 'The display name of the caller.' },
    { label: '$context.identity.userName', insertText: '$context.identity.userName', documentation: 'The user name of the caller.' },
    { label: '$context.identity.isAuthenticated', insertText: '$context.identity.isAuthenticated', documentation: 'Whether the caller is authenticated.' },
    { label: '$context.identity.roles', insertText: '$context.identity.roles', documentation: 'The roles the caller holds.' },
    { label: '$context.identity.claims.', insertText: '$context.identity.claims.${1:name}', documentation: 'The value of a claim the caller carries.' },
    { label: '$env.', insertText: '$env.${1:VAR_NAME}', documentation: 'An environment variable.' },
    { label: '$eventContext.occurred', insertText: '$eventContext.occurred', documentation: 'Timestamp of the event being projected (PDL).' },
];

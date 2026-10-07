// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { eventContextPaths } from './event-context';

export const optionalTypeItems: CompletionEntry[] = [
    { label: 'optional', insertText: 'optional', documentation: 'Allows the complete value, including a collection, to be absent.' },
];

export interface CompletionEntry {
    label: string;
    insertText: string;
    documentation: string;
}

const fenced = (tag: string) => `\`\`\`${tag}\n\${1}\n\`\`\``;

export const exampleDeclarationItems: CompletionEntry[] = [
    { label: 'example', insertText: 'example ${1:Name} : ${2:Type}\n    ${3:property} = ${4:value}', documentation: 'One possibly partial event, command or read-model fixture. Step assignments override its values; no implicit defaults.' },
];

export const topLevelItems: CompletionEntry[] = [
    ...exampleDeclarationItems,
    { label: 'eventsource', insertText: 'eventsource ${1:Name}\n    identifier ${2:Type}\n    stream ${3:Name}', documentation: 'Application-owned source with nested streams; not admitted by any supported executable model (ESM) version yet (PLAY0268).' },
    { label: 'system', insertText: 'system ${1:Name}\n    description "${2:external system}"', documentation: 'Application-scoped external system; not admitted by any supported executable model (ESM) version yet.' },
    { label: 'import', insertText: 'import ${1:Module}.${2:Type}', documentation: 'Imports a type from another module by its qualified name.' },
    { label: 'import "…"', insertText: 'import "${1:**/*.play}"', documentation: 'Imports other `.play` files by path or glob, relative to this file\'s folder, as whole documents of the application.' },
    { label: 'concept', insertText: 'concept ${1:Name} : ${2|Uuid,String,Int,Decimal,Bool,Date,DateTime|}', documentation: 'Declares a formalized value type wrapping a primitive.' },
    { label: 'concept (enum)', insertText: 'concept ${1:Name} : Enum\n    ${2:value}', documentation: 'Declares an enumeration concept with a fixed set of values.' },
    { label: 'concept (@pii with reason)', insertText: 'concept ${1:Name} : ${2|String,Uuid,Int,Decimal,Bool,Date,DateTime|} @pii\n    pii reason "${3:why this is personal data, its purpose and lawful basis}"', documentation: 'Declares a personal-data concept together with the reason it is personal data.' },
    { label: 'type', insertText: 'type ${1:Name}\n    ${2:property} ${3:Type}', documentation: 'Declares a composite value type — a named shape built from several properties.' },
    { label: 'policy', insertText: 'policy ${1:Name}\n    require ${2:authenticated}', documentation: 'Declares a named authorization rule for commands and queries.' },
    { label: 'module', insertText: 'module ${1:Name}\n    ', documentation: 'Declares the top-level namespace — maps to a bounded context.' },
];

export const operationItems: CompletionEntry[] = [
    { label: 'uses', insertText: 'uses ${1:System}', documentation: 'Exactly one application-scoped external system.' },
    { label: 'description', insertText: 'description "${1:intent}"', documentation: 'Describes operation intent, without requiring code.' },
    { label: 'input', insertText: '${1:input} ${2:Type}', documentation: 'Typed operation input; no identifier or generated modifier.' },
    { label: 'execute', insertText: 'execute\n    description "${1:effect}"', documentation: 'Optional execution intent; not admitted by any supported executable model (ESM) version yet.' },
    { label: 'compensate', insertText: 'compensate\n    description "${1:undo intent}"', documentation: 'Optional compensation intent; not admitted by any supported executable model (ESM) version yet.' },
];

export const operationImplementationItems: CompletionEntry[] = [
    { label: 'hint', insertText: 'hint "${1:implementation guidance}"', documentation: 'Ordered authoring guidance; not an executable implementation role.' },
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Selects the phase’s sole source attachment.' },
    ...['csharp', 'typescript', 'react', 'html', 'sql'].map(language => ({ label: language, insertText: fenced(language), documentation: 'Selects the phase’s sole inline source; not admitted by any supported executable model (ESM) version yet.' })),
];

export const operationPhaseItems: CompletionEntry[] = [
    { label: 'description', insertText: 'description "${1:phase intent}"', documentation: 'A description-only phase is valid, pending intent.' },
    { label: 'implementation', insertText: 'implementation\n    hint "${1:guidance}"', documentation: 'Hints with optional source; phase owns File/Code, wrapper owns hints only.' },
    ...operationImplementationItems.filter(item => item.label !== 'hint'),
];

export const conceptItems: CompletionEntry[] = [
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Names the repository relative file this declaration is realized by, so the document can be navigated back to the code.' },
    { label: 'pii reason', insertText: 'pii reason "${1:why this is personal data, its purpose and lawful basis}"', documentation: 'Records why the `@pii` marker applies — purpose, lawful basis, whose subject it lives under.' },
    { label: 'sensitive reason', insertText: 'sensitive reason "${1:why this value is sensitive}"', documentation: 'Records why the `@sensitive` marker applies.' },
    { label: 'validate', insertText: 'validate\n    ${1:not empty} message "${2:message}"', documentation: 'Validation rules that travel with the value everywhere it appears.' },
];

export const typeItems: CompletionEntry[] = [
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Names the repository relative file this declaration is realized by, so the document can be navigated back to the code.' },
    { label: 'description', insertText: 'description "${1:what this shape represents}"', documentation: 'A human-readable description of the type.' },
    { label: 'property', insertText: '${1:property} ${2:Type}', documentation: 'A property of the type — a name and a type reference.' },
];

export const moduleItems: CompletionEntry[] = [
    ...exampleDeclarationItems,
    { label: 'import "…"', insertText: 'import "${1:*/*.play}"', documentation: 'Imports `.play` files into this module — their top level is the module\'s body, so they hold features and module members without restating the module.' },
    { label: 'layout', insertText: 'layout ${1:Name}\n    template\n        ${2:slot}', documentation: 'Declares a reusable screen template with named slots.' },
    { label: 'feature', insertText: 'feature ${1:Name}\n    ', documentation: 'Groups related slices into a vertical feature.' },
];

export const featureItems: CompletionEntry[] = [
    ...exampleDeclarationItems,
    { label: 'import "…"', insertText: 'import "${1:*.play}"', documentation: 'Imports `.play` files into this feature — their top level is the feature\'s body, so they hold slices and nested features without restating where they belong.' },
    { label: 'feature', insertText: 'feature ${1:Name}\n    ', documentation: 'Declares a nested sub-feature.' },
    { label: 'slice StateChange', insertText: 'slice StateChange ${1:Name}\n    ', documentation: 'A command → events flow; something that changes the system.' },
    { label: 'slice StateView', insertText: 'slice StateView ${1:Name}\n    ', documentation: 'A query + projection + screen; something that reads the system.' },
    { label: 'slice Automation', insertText: 'slice Automation ${1:Name}\n    ', documentation: 'A reaction or reducer; something that runs when something happens.' },
    { label: 'slice Translate', insertText: 'slice Translate ${1:Name}\n    ', documentation: 'A capture; converts external data into events.' },
];

export const sliceItems: CompletionEntry[] = [
    ...exampleDeclarationItems,
    { label: 'operation', insertText: 'operation ${1:Name}\n    uses ${2:System}\n    ${3:input} ${4:Type}', documentation: 'Reusable slice-owned operation intent; not admitted by any supported executable model (ESM) version yet.' },
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Names the repository relative file this declaration is realized by, so the document can be navigated back to the code.' },
    { label: 'event', insertText: 'event ${1:Name}\n    ${2:property} ${3:Type}', documentation: 'Declares an event type — an immutable, past-tense fact.' },
    { label: 'event generation', insertText: 'event ${1:Name} generation ${2:2}\n    ${3:property} ${4:Type}', documentation: 'Declares a complete numbered event generation; start at 1 and do not skip a number.' },
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
    { label: 'produces operation', insertText: 'produces operation ${1:Name}\n    uses ${2:System}\n    ${3:input} ${4:Type} = ${5:source}', documentation: 'Declares ordered operation intent; not admitted by any supported executable model (ESM) version yet (PLAY0268).' },
    { label: 'returns property', insertText: 'returns @${1:property}', documentation: 'Scalar response from a direct command property, only on acceptance. Executable as ESM v7.' },
    { label: 'returns block', insertText: 'returns\n    ${1:field} = ${2:property}', documentation: 'Unnamed record response with inferred or explicit field types, only on acceptance. Executable as ESM v7.' },
    { label: 'produces event', insertText: 'produces event ${1:Name}\n    ${2:property} ${3:Type} = ${4:source}', documentation: 'Declares a slice-owned generation-1 event and maps its properties. An omitted for uses the command identifier.' },
    { label: 'identifier property', insertText: '${1:property} ${2:Type} identifier', documentation: 'Marks the property a runtime resolves the event source id from. At most one per command.' },
    { label: 'authorize', insertText: 'authorize ${1:PolicyName}', documentation: 'References the policies that must pass for the command to execute.' },
    { label: 'reads', insertText: 'reads ${1:View} by ${2:property}', documentation: 'Declares a view the command consults. Executable binding is not yet supported.' },
    { label: 'reads as', insertText: 'reads ${1:View} as ${2:alias} by ${3:property}', documentation: 'Names one instance of a view; every instance needs a unique alias when reading the same view more than once.' },
    { label: 'validate', insertText: 'validate\n    ${1:property} not empty message "${2:message}"', documentation: 'Declarative validation rules with messages.' },
    { label: 'validate csharp', insertText: `validate\n    ${fenced('csharp')}`, documentation: 'Imperative validation in C#, yielding the message of every rule the artifact breaks.' },
    { label: 'produces', insertText: 'produces ${1:EventType}\n    ${2:property} = ${3:source}', documentation: 'Declares the event the command emits, with property mappings.' },
    { label: 'produces when', insertText: 'produces when ${1:condition}\n    ${2:EventType}\n        ${3:property} = ${4:source}', documentation: 'Conditionally emits an event when the condition holds.' },
    { label: 'handler', insertText: 'handler\n    ', documentation: 'Fully imperative command implementation — file reference or inline C#, instead of produces.' },
];

export const eventItems: CompletionEntry[] = [
    { label: 'description', insertText: 'description "${1:what happened}"', documentation: 'Authoring metadata, as one line or a text/markdown fence.' },
    { label: 'documentation', insertText: 'documentation\n    ```markdown\n    ${1:Details}\n    ```', documentation: 'Authoring-only Markdown documentation.' },
    { label: 'id', insertText: 'id "${1:OldName}"', documentation: 'Preserves a previous persisted event name after a rename. Leave absent for new events.' },
    { label: 'tag', insertText: 'tag ${1:audit}', documentation: 'An event-type tag stamped on every occurrence.' },
];

export const inlineEventItems: CompletionEntry[] = [
    ...eventItems,
    { label: 'for', insertText: 'for ${1:identifier}', documentation: 'Explicitly names the command identifier used as the event source.' },
    { label: 'property mapping', insertText: '${1:property} ${2:Type} = ${3:source}', documentation: 'Declares and maps an event property on one line.' },
];

export const producesItems: CompletionEntry[] = [
    { label: 'when', insertText: 'when ${1:condition}', documentation: 'Guards the produced event with a condition.' },
];

export const implementationItems: CompletionEntry[] = [
    { label: 'hint', insertText: 'hint "${1:implementation guidance}"', documentation: 'Ordered nonblank guidance for a handler implementation. Not an execution guarantee.' },
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Selects one existing model-relative attachment.' },
    ...['csharp', 'typescript', 'react', 'html', 'sql'].map(language => ({ label: language, insertText: fenced(language), documentation: 'Selects one inline payload. Handler execution remains unsupported.' })),
];

export const handlerItems: CompletionEntry[] = [
    { label: 'implementation', insertText: 'implementation\n    hint "${1:implementation guidance}"', documentation: 'Handler-only implementation intent with optional file or tagged fence. With no payload it is pending.' },
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Delegates the command implementation to an external C# file.' },
    { label: 'csharp', insertText: fenced('csharp'), documentation: 'Inline C# returning the events to append.' },
];

export const queryItems: CompletionEntry[] = [
    { label: 'description', insertText: 'description "${1:what this query is trying to accomplish}"', documentation: 'What the query is for, in prose — what a generator or reviewer works from.' },
    { label: 'by', insertText: 'by ${1:param} ${2:Type}', documentation: 'Declares the identifying parameter of the query.' },
    { label: 'filter', insertText: 'filter ${1:param} ${2:Type} optional', documentation: 'Declares an optional filter parameter supplied by the caller.' },
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
    { label: 'reads', insertText: 'reads ${1:View}', documentation: 'The view this trigger consults. Optionally use `as <alias> by <trigger value>`; clock triggers take no values and cannot use `by`.' },
    { label: 'description', insertText: 'description "${1:what this reaction does}"', documentation: 'What this particular reaction does — enough on its own, with no file to point at.' },
    { label: 'produces', insertText: 'produces ${1:EventType}', documentation: 'An event the reaction appends.' },
    { label: 'invokes', insertText: 'invokes ${1:Command}', documentation: 'A command the reaction hands on. A command is asked for, not produced — it may still be rejected.' },
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Delegates the reaction to an external C# file.' },
    { label: 'csharp', insertText: fenced('csharp'), documentation: 'Inline C# returning event side effects.' },
];

export const triggerItems: CompletionEntry[] = [
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Names the repository relative file this declaration is realized by, so the document can be navigated back to the code.' },
    { label: 'description', insertText: 'description "${1:when this occurs}"', documentation: 'What makes an occurrence of this trigger happen.' },
    { label: 'value', insertText: '${1:name} ${2:Type}', documentation: 'A value an occurrence hands the reaction. The type is optional.' },
];

export const specificationItems: CompletionEntry[] = [
    { label: 'given operation fails', insertText: 'given operation ${1:Name} fails', documentation: 'Failure fixture leaf; not admitted by any supported executable model (ESM) version yet (PLAY0268).' },
    { label: 'then operation', insertText: 'then operation ${1:Name}\n    ${2:input} = ${3:value}', documentation: 'Partial requested-operation assertion; not admitted by any supported executable model (ESM) version yet (PLAY0268).' },
    { label: 'then compensated', insertText: 'then compensated ${1:Name}', documentation: 'Compensation assertion leaf; not admitted by any supported executable model (ESM) version yet (PLAY0268).' },
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Names the repository relative file this declaration is realized by, so the document can be navigated back to the code.' },
    { label: 'given', insertText: 'given ${1:EventType}\n    ${2:property} = ${3:value}', documentation: 'Establishes prior state by replaying an event before the command runs.' },
    { label: 'given readmodel', insertText: 'given readmodel ${1:ReadModelType}\n    ${2:property} = ${3:value}', documentation: 'Establishes prior read model state directly.' },
    { label: 'when', insertText: 'when ${1:CommandType}\n    ${2:property} = ${3:value}', documentation: 'The command being exercised.' },
    { label: 'then', insertText: 'then ${1:EventType}\n    ${2:property} = ${3:value}', documentation: 'An event expected to be produced by the command.' },
    { label: 'then readmodel', insertText: 'then readmodel ${1:ReadModelType}\n    ${2:property} = ${3:value}', documentation: 'The read model state expected after the command.' },
    { label: 'then no readmodel', insertText: 'then no readmodel ${1:ReadModelType} for ${2:key}', documentation: 'Asserts that precisely one keyed read-model instance does not exist; another key may still be present.' },
    { label: 'then query', insertText: 'then query ${1:QueryName}\n    arguments\n        ${2:argument} = ${3:value}\n    result\n        ${4:property} = ${5:value}', documentation: 'The ordered query results expected for explicit arguments. Remove the result block to assert an empty result.' },
    { label: 'then error', insertText: 'then error', documentation: 'A rejection, for a reason this specification does not name.' },
    { label: 'then error "..."', insertText: 'then error "${1:reason}"', documentation: 'A rejection, for the named reason.' },
    { label: 'given clock', insertText: 'given clock "${1:2026-10-05T08:00:00Z}"', documentation: 'The instant the scenario happens at - the occurrence time of everything it does. At most once.' },
    { label: 'given capture', insertText: 'given capture ${1:Capture}\n    ${2:field} = ${3:value}', documentation: 'A record the capture\'s source held before.' },
    { label: 'when clock', insertText: 'when clock "${1:2026-10-05T09:00:00Z}"', documentation: 'The clock reaches an instant, as the action.' },
    { label: 'when trigger', insertText: 'when trigger ${1:Trigger}\n    ${2:value} = ${3:value}', documentation: 'An application trigger fires, as the action.' },
    { label: 'when capture', insertText: 'when capture ${1:Capture}\n    ${2:field} = ${3:value}', documentation: 'The record a capture\'s source holds now, as the action.' },
    { label: 'when query', insertText: 'when query ${1:Query}\n    ${2:argument} = ${3:value}', documentation: 'Performs a query, as the action.' },
    { label: 'then result', insertText: 'then result\n    ${1:property} = ${2:value}', documentation: 'One result the query performed by `when query` returns, in order.' },
    { label: 'then no result', insertText: 'then no result', documentation: 'The query performed by `when query` returns nothing.' },
];

// What follows a 'given', 'when' or 'then' already typed in a specification.
export const specificationStepItems: Record<'given' | 'when' | 'then', CompletionEntry[]> = {
    given: [
        { label: 'operation', insertText: 'operation ${1:Name} fails', documentation: 'Syntax-only failure fixture; no children.' },
        { label: 'clock', insertText: 'clock "${1:2026-10-05T08:00:00Z}"', documentation: 'The instant the scenario happens at - the occurrence time of everything it does.' },
        { label: 'capture', insertText: 'capture ${1:Capture}\n    ${2:field} = ${3:value}', documentation: 'A record the capture\'s source held before. Repeat it for several records.' },
        { label: 'readmodel', insertText: 'readmodel ${1:ReadModelType}\n    ${2:property} = ${3:value}', documentation: 'Establishes prior read model state directly.' },
        { label: 'caller', insertText: 'caller\n    ${1:authenticated}', documentation: 'The identity the scenario runs as.' },
    ],
    when: [
        { label: 'clock', insertText: 'clock "${1:2026-10-05T09:00:00Z}"', documentation: 'The clock reaches an instant - what a scheduled reaction responds to.' },
        { label: 'trigger', insertText: 'trigger ${1:Trigger}\n    ${2:value} = ${3:value}', documentation: 'An application trigger fires, with the values it carries.' },
        { label: 'capture', insertText: 'capture ${1:Capture}\n    ${2:field} = ${3:value}', documentation: 'The record a capture\'s source holds now.' },
        { label: 'query', insertText: 'query ${1:Query}\n    ${2:argument} = ${3:value}', documentation: 'Performs a query; assert what it returns with `then result` or `then no result`.' },
        { label: 'append', insertText: 'append ${1:EventType}\n    ${2:property} = ${3:value}', documentation: 'An event occurs, instead of a command being executed.' },
    ],
    then: [
        { label: 'operation', insertText: 'operation ${1:Name}\n    ${2:input} = ${3:value}', documentation: 'Syntax-only partial operation assertion; not admitted by any supported executable model (ESM) version yet.' },
        { label: 'compensated', insertText: 'compensated ${1:Name}', documentation: 'Syntax-only compensation assertion; no children.' },
        { label: 'returns value', insertText: 'returns ${1:value}', documentation: 'Scalar response expectation, compared by semantic value equality. Executable as ESM v7.' },
        { label: 'returns block', insertText: 'returns\n    ${1:field} = ${2:value}', documentation: 'Nonempty subset of response fields asserted by name. Executable as ESM v7.' },
        { label: 'result', insertText: 'result\n    ${1:property} = ${2:value}', documentation: 'One result the query performed by `when query` returns, in order.' },
        { label: 'result exactly', insertText: 'result exactly\n    ${1:property} = ${2:value}', documentation: 'One result, with every property asserted.' },
        { label: 'no result', insertText: 'no result', documentation: 'The query performed by `when query` returns nothing.' },
        { label: 'readmodel', insertText: 'readmodel ${1:ReadModelType}\n    ${2:property} = ${3:value}', documentation: 'The read model state expected afterwards.' },
        { label: 'error', insertText: 'error "${1:reason}"', documentation: 'A rejection, for the named reason.' },
        { label: 'denied', insertText: 'denied', documentation: 'The action is denied to the caller.' },
    ],
};

export const namedRuleImplementationItems: CompletionEntry[] = [
    { label: 'hint', insertText: 'hint "${1:predicate guidance}"', documentation: 'Ordered nonblank authoring guidance. Does not execute or confirm the predicate.' },
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'The sole named-rule predicate attachment.' },
    { label: 'csharp', insertText: fenced('csharp'), documentation: 'Opaque named-rule predicate returning a bool. Requires an admitting target.' },
];

export const commandRuleItems: CompletionEntry[] = [
    { label: 'implementation', insertText: 'implementation\n    hint "${1:predicate guidance}"', documentation: 'Command named-rule intent with optional file or tagged fence. Pending intent cannot bind.' },
    ...namedRuleImplementationItems.filter(item => item.label !== 'hint'),
];

export const ruleItems: CompletionEntry[] = [
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Gives the named predicate its implementation in an external C# file.' },
    { label: 'csharp', insertText: fenced('csharp'), documentation: 'Gives the named predicate its implementation as inline C# returning a bool.' },
];

export const policyItems: CompletionEntry[] = [
    { label: 'require authenticated', insertText: 'require authenticated', documentation: 'Requires an authenticated caller.' },
    { label: 'require role', insertText: 'require role "${1:role}"', documentation: 'Requires the caller to have a role.' },
    { label: 'require not role', insertText: 'require not role "${1:role}"', documentation: 'Excludes callers with a role. Add authenticated to require a signed-in caller.' },
    { label: 'require not claim', insertText: 'require not claim "${1:claim}" matches ${2:subject}', documentation: 'Requires that no claim value matches the subject or a value.' },
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
    // Every $eventContext path the event-context catalog lists, for projections - the underlying value of a concept
    // is left out, it reads the same as the member it belongs to.
    ...eventContextPaths
        .filter((path) => path.name !== 'value')
        .map((path) => ({ label: `$eventContext.${path.path}`, insertText: `$eventContext.${path.path}`, documentation: `${path.description} (PDL)` })),
];

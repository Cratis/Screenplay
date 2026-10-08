// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CompletionEntry } from './completion-items';
import { exampleDeclarationItems } from './example-declaration-items';

const interactions = ['click', 'double click', 'select', 'submit', 'change', 'load', 'unload', 'enter', 'leave'];

const interactionItems: CompletionEntry[] = [
    { label: 'on', insertText: 'on ${1|click,select,submit,change,load,unload,enter,leave|}\n    ${2:execute command}', documentation: 'Wires what happens on an interaction - a click, a submit, entering or leaving the screen.' },
    { label: 'uses', insertText: 'uses ${1:Behavior}', documentation: 'Attaches a named behavior declared at the top level.' },
];

export const moduleScopeItems: CompletionEntry[] = [
    ...exampleDeclarationItems,
    { label: 'description', insertText: 'description "${1:what this module is for}"', documentation: 'A human-readable description.' },
    { label: 'authorize', insertText: 'authorize ${1:PolicyName}', documentation: 'Policies that must pass for everything in the module.' },
    { label: 'depends on', insertText: 'depends on ${1:Name}', documentation: 'Declares an allowed module or feature dependency. Opts this container into independent explicit-reference checks.' },
    { label: 'import "…"', insertText: 'import "${1:*/*.play}"', documentation: 'Imports `.play` files into this module — their top level is the module\'s body.' },
    { label: 'feature', insertText: 'feature ${1:Name}\n    ', documentation: 'Groups related slices into a vertical feature.' },
    { label: 'screen', insertText: 'screen ${1:Name}\n    data ${2:ReadModel} via query ${3:QueryName}', documentation: 'Declares a module-level UI screen.' },
    { label: 'screen template', insertText: 'screen template ${1:Name}\n    fits slot ${2:content}\n\n    ${3:slot}', documentation: 'Declares a reusable screen shape that fits a slot of the shell.' },
    { label: 'dialog template', insertText: 'dialog template ${1:Name}\n    ${2:body}\n    ${3:actions}', documentation: 'Declares a reusable dialog shape that opens over the shell.' },
    { label: 'form', insertText: 'form ${1:Name} for ${2:Command}\n    field ${3:property} label "${4:Label}"', documentation: 'Declares a form bound to a command.' },
    { label: 'contribute', insertText: 'contribute to ${1:Navigation}\n    navigate to ${2:Screen}\n    label "${3:Label}"', documentation: 'Contributes an entry to a slot of the shell, such as navigation.' },
    ...interactionItems,
];

export const featureScopeItems: CompletionEntry[] = [
    ...exampleDeclarationItems,
    { label: 'description', insertText: 'description "${1:what this feature is for}"', documentation: 'A human-readable description.' },
    { label: 'authorize', insertText: 'authorize ${1:PolicyName}', documentation: 'Policies that must pass for everything in the feature.' },
    { label: 'depends on', insertText: 'depends on ${1:Name}', documentation: 'Declares an allowed module or feature dependency. Sibling feature names resolve before module names.' },
    { label: 'import "…"', insertText: 'import "${1:*.play}"', documentation: 'Imports `.play` files into this feature — their top level is the feature\'s body.' },
    { label: 'feature', insertText: 'feature ${1:Name}\n    ', documentation: 'Declares a nested sub-feature.' },
    { label: 'slice StateChange', insertText: 'slice StateChange ${1:Name}\n    ', documentation: 'A command → events flow; something that changes the system.' },
    { label: 'slice StateView', insertText: 'slice StateView ${1:Name}\n    ', documentation: 'A query + projection + screen; something that reads the system.' },
    { label: 'slice Automation', insertText: 'slice Automation ${1:Name}\n    ', documentation: 'A reaction or reducer; something that runs when something happens.' },
    { label: 'slice Translate', insertText: 'slice Translate ${1:Name}\n    ', documentation: 'A capture; converts external data into events.' },
    { label: 'contribute', insertText: 'contribute to ${1:Navigation}\n    navigate to ${2:Screen}\n    label "${3:Label}"', documentation: 'Contributes an entry to a slot of the shell, such as navigation.' },
    ...interactionItems,
];

export const readModelItems: CompletionEntry[] = [
    { label: 'description', insertText: 'description "${1:what one instance holds}"', documentation: 'A human-readable description.' },
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Names the file this read model is realized by.' },
    { label: 'property', insertText: '${1:property} ${2:Type}', documentation: 'A property of the read model — a name and a type reference.' },
];

export const reducerItems: CompletionEntry[] = [
    { label: 'description', insertText: 'description "${1:what the reducer does}"', documentation: 'A human-readable description.' },
    { label: 'on', insertText: 'on ${1:EventType}\n    ```csharp\n    ${2}\n    ```', documentation: 'A rule applied when the event occurs, in code or from a file.' },
];

export const formItems: CompletionEntry[] = [
    { label: 'populate via query', insertText: 'populate via query ${1:Query} by ${2:parameter}', documentation: 'Fills the form from a query before it is shown.' },
    { label: 'populate from item', insertText: 'populate from item', documentation: 'Fills the form from the item the screen was opened for.' },
    { label: 'field', insertText: 'field ${1:property} label "${2:Label}"', documentation: 'A form field bound to a command property.' },
    ...interactionItems,
];

export const contributeItems: CompletionEntry[] = [
    { label: 'navigate to', insertText: 'navigate to ${1:Screen}', documentation: 'The screen the entry opens.' },
    { label: 'label', insertText: 'label "${1:Label}"', documentation: 'The text of the entry.' },
    { label: 'order', insertText: 'order ${1:10}', documentation: 'Where the entry sits among the others; lower comes first.' },
];

export const behaviorItems: CompletionEntry[] = [
    { label: 'description', insertText: 'description "${1:what the behavior does}"', documentation: 'A human-readable description.' },
    { label: 'parameter', insertText: 'parameter ${1:name} ${2:Type}', documentation: 'A value the behavior is given where it is attached.' },
    { label: 'order', insertText: 'order ${1:10}', documentation: 'The order behaviors attached to the same element run in.' },
    { label: 'on', insertText: 'on ${1|click,select,submit,change,load,unload,enter,leave|}\n    ${2:execute command}', documentation: 'Wires what happens on an interaction.' },
];

const step = (label: string, insertText: string, documentation: string): CompletionEntry => ({ label, insertText, documentation });

// What an interaction does, and what can follow an action's outcome.
export const actionStepItems: CompletionEntry[] = [
    step('execute', 'execute ${1:command}', 'Runs a command.'),
    step('navigate to', 'navigate to ${1:Screen}', 'Opens a screen.'),
    step('open', 'open ${1:Dialog}', 'Opens a dialog.'),
    step('close', 'close', 'Closes the dialog.'),
    step('refresh', 'refresh ${1:query}', 'Reloads a query.'),
    step('set', 'set ${1:name} ${2:value}', 'Sets a value.'),
    step('notify', 'notify ${1|info,warning,error|} "${2:message}"', 'Shows a notification.'),
    step('confirm', 'confirm ${1:message}\n    on success\n        ${2:execute command}', 'Asks before continuing.'),
    step('raise', 'raise ${1:Trigger}', 'Raises a trigger.'),
];

export const outcomeItems: CompletionEntry[] = [
    step('on success', 'on success\n    ${1:notify info "Done"}', 'What happens when it succeeds.'),
    step('on failure', 'on failure\n    ${1:notify error "Failed"}', 'What happens when it fails.'),
    ...actionStepItems,
];

export const personaItems: CompletionEntry[] = [
    { label: 'description', insertText: 'description "${1:who they are}"', documentation: 'A human-readable description.' },
    { label: 'policy', insertText: 'policy ${1:PolicyName}', documentation: 'A policy this persona holds.' },
];

export const authenticationItems: CompletionEntry[] = [
    { label: 'provider', insertText: 'provider ${1:Name}', documentation: 'An identity provider.' },
    { label: 'provider (named)', insertText: 'provider ${1:Kind} name ${2:Name}', documentation: 'An identity provider of a kind, under its own name.' },
];

export const seedItems: CompletionEntry[] = [
    { label: 'for', insertText: 'for ${1:"event-source-id"}\n    ${2:EventType}\n        ${3:property} = ${4:value}', documentation: 'Events to seed for one event source id.' },
];

export const themeItems: CompletionEntry[] = [
    { label: 'compatible with', insertText: 'compatible with ${1:Package}', documentation: 'A component package the theme is compatible with.' },
];

export const uiProfileItems: CompletionEntry[] = [
    { label: 'target platform', insertText: 'target platform ${1:web}', documentation: 'The platforms the profile is for.' },
    { label: 'target size', insertText: 'target size ${1|compact,regular|}', documentation: 'The screen sizes the profile is for.' },
    { label: 'packages', insertText: 'packages\n    ${1:Package}', documentation: 'The component packages the profile uses.' },
    { label: 'layout', insertText: 'layout ${1:Layout}', documentation: 'The layout of the profile.' },
    { label: 'theme', insertText: 'theme ${1:Theme}', documentation: 'The theme of the profile.' },
];

export const layoutItems: CompletionEntry[] = [
    { label: 'slot', insertText: '${1:slot}', documentation: 'A named slot of the shell.' },
    { label: 'slot contributes', insertText: '${1:slot} contributes ${2:Name}', documentation: 'A slot other declarations contribute to, such as `navigation contributes Navigation`.' },
    { label: 'arrangement', insertText: 'arrangement flow\n    column\n        ${1:slot}', documentation: 'How the slots are placed.' },
];

export const templateItems: CompletionEntry[] = [
    { label: 'fits slot', insertText: 'fits slot ${1:content}', documentation: 'The slot of the shell the template fits.' },
    ...layoutItems,
    { label: 'on', insertText: 'on ${1|enter,leave,load,unload|}\n    ${2:refresh query}', documentation: 'Wires what happens on an interaction.' },
];

export const interactionNames = interactions;

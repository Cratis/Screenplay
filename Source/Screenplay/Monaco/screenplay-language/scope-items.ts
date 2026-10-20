// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CompletionEntry } from './completion-items';
import { exampleDeclarationItems } from './example-declaration-items';
import { documentationItem } from './documentation-item';
import { purposeReferenceItem } from './purpose-items';

const interactions = ['click', 'double click', 'select', 'submit', 'change', 'load', 'unload', 'enter', 'leave'];

const interactionItems: CompletionEntry[] = [
    { label: 'on', insertText: 'on ${1|click,select,submit,change,load,unload,enter,leave|}\n    ${2:execute command}', documentation: 'Wires what happens on an interaction - a click, a submit, entering or leaving the screen.' },
    { label: 'uses', insertText: 'uses ${1:Behavior}', documentation: 'Attaches a named behavior declared at the top level.' },
];

export const moduleScopeItems: CompletionEntry[] = [
    purposeReferenceItem,
    documentationItem,
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
    { label: 'navigation sidebar extends', insertText: 'navigation ${1|sidebar,topbar|} ${2|extends,overrides,inherits|}\n    item ${3:id} title "${4:Title}" target ${5:Module.Feature}', documentation: 'Changes a navigation region for this module: `extends` adds items after the application\'s, `overrides` replaces them, `inherits` shows the application\'s.' },
    ...interactionItems,
];

export const featureScopeItems: CompletionEntry[] = [
    purposeReferenceItem,
    documentationItem,
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
    documentationItem,
    { label: 'description', insertText: 'description "${1:what one instance holds}"', documentation: 'A human-readable description.' },
    { label: 'file', insertText: 'file ${1:Path}', documentation: 'Names the file this read model is realized by.' },
    { label: 'property', insertText: '${1:property} ${2:Type}', documentation: 'A property of the read model — a name and a type reference.' },
];

export const reducerItems: CompletionEntry[] = [
    { label: 'from', insertText: 'from ${1:Source.Stream}', documentation: 'Observe matching routed facts only, without filtering stream ids. ESM v10.' },
    { label: 'description', insertText: 'description "${1:what the reducer does}"', documentation: 'A human-readable description.' },
    { label: 'on', insertText: 'on ${1:EventType}\n    ```csharp\n    ${2}\n    ```', documentation: 'A rule applied when the event occurs, in code or from a file.' },
];

export const formItems: CompletionEntry[] = [
    { label: 'description', insertText: 'description "${1:input surface intent}"', documentation: 'Report-only description of the form.' },
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
    { label: 'layout', insertText: 'layout ${1:Layout}', documentation: 'The layout of the profile. Older form of `root template`; prefer `root template`.' },
    { label: 'theme', insertText: 'theme ${1:Theme}', documentation: 'The theme of the profile, named by a declared theme\'s identifier.' },
    { label: 'theme (package-provided)', insertText: 'theme "${1:Scene Default Light}"', documentation: 'The theme of the profile, named by the quoted display name of a theme a component package provides.' },
    { label: 'root template', insertText: 'root template ${1:Template}\n    option ${2:navigationMode} ${3:slim}', documentation: 'The template the whole application is drawn in: an own template, or `blueprint <Blueprint> <Name> [version "<range>"]`, with the options the shell exposes.' },
    { label: 'root template blueprint', insertText: 'root template blueprint ${1:Blueprint} ${2:Template} version "${3:^1.0}"', documentation: 'A root template a blueprint package ships, pinned to a semver range.' },
    { label: 'identity', insertText: 'identity\n    name "${1:Name}"\n    logo ${2|initials "AB",icon box,image "data:image/svg+xml;base64,..."|}\n    show name ${3|true,false|}', documentation: 'What the application calls itself and the mark it wears: `name`, `logo initials|icon|image` and `show name`.' },
    { label: 'appearance', insertText: 'appearance\n    color scheme ${1|light,dark,system|}\n    accent "${2:#4f46e5}"\n    corners ${3|sharp,default,round|}\n    font ${4:inter}', documentation: 'How the application looks regardless of the shell: colour scheme, accent colour, corners and font.' },
    { label: 'new screens use template', insertText: 'new screens use template ${1:Template}', documentation: 'The template a new screen starts from in the designer.' },
    { label: 'sample data', insertText: 'sample data\n    active ${1:Pack}', documentation: 'The sample data the whole application previews with, chosen once here.' },
];

export const profileRootTemplateItems: CompletionEntry[] = [
    { label: 'option', insertText: 'option ${1:name} ${2:value}', documentation: 'Sets one of the shell\'s own options, for example `option navigationMode slim`. The value is a word, a quoted string, a number, `true` or `false`.' },
];

export const profileAppearanceItems: CompletionEntry[] = [
    { label: 'color scheme', insertText: 'color scheme ${1|light,dark,system|}', documentation: 'The colour scheme of the application: light, dark or follow the system.' },
    { label: 'accent', insertText: 'accent "${1:#4f46e5}"', documentation: 'The accent colour of the application.' },
    { label: 'corners', insertText: 'corners ${1|sharp,default,round|}', documentation: 'How rounded the application\'s corners are: sharp, default or round.' },
    { label: 'font', insertText: 'font ${1|inter,ibm-plex-sans,source-serif-4,system-ui|}', documentation: 'The font of the application, an id from Scene\'s font catalog.' },
];

export const profileIdentityItems: CompletionEntry[] = [
    { label: 'name', insertText: 'name "${1:Name}"', documentation: 'What the application calls itself in its shell; leave it out to take the application\'s own name.' },
    { label: 'logo initials', insertText: 'logo initials "${1:AB}"', documentation: 'A logo drawn from one or two characters.' },
    { label: 'logo icon', insertText: 'logo icon ${1|box,layers,home,users,receipt,package,database,palette|}', documentation: 'A logo drawn from one of Scene\'s logo icons.' },
    { label: 'logo image', insertText: 'logo image "data:image/svg+xml;base64,${1}"', documentation: 'A logo image (PNG, JPG, SVG or WebP, up to 400 KB) as a base64 data URI.' },
    { label: 'show name', insertText: 'show name ${1|true,false|}', documentation: 'Whether the name is drawn beside the logo.' },
];

export const profileSampleDataItems: CompletionEntry[] = [
    { label: 'active', insertText: 'active ${1:Pack}', documentation: 'The active sample data pack of the whole application: one declared in the document, or `blueprint <id>`.' },
    { label: 'shape maps to', insertText: 'shape ${1:Shape} maps to ${2:ReadModel}', documentation: 'Maps a sample data shape to the read model it supplies rows for.' },
    { label: 'bind', insertText: 'bind ${1:ReadModel} to ${2:Pack}', documentation: 'A rare override: binds one read model to a pack.' },
];

export const sampleDataItems: CompletionEntry[] = [
    { label: 'title', insertText: 'title "${1:Title}"', documentation: 'The display title of the pack.' },
    { label: 'locale', insertText: 'locale ${1:en-GB}', documentation: 'The BCP 47 locale tag of the data.' },
    { label: 'shape', insertText: 'shape ${1:ReadModel}\n    row\n        ${2:field} = ${3:"value"}', documentation: 'The rows for one shape: the read model of the same name, or the one a ui profile maps it to.' },
];

export const sampleShapeItems: CompletionEntry[] = [
    { label: 'row', insertText: 'row\n    ${1:field} = ${2:"value"}', documentation: 'One row of the shape.' },
];

export const layoutItems: CompletionEntry[] = [
    { label: 'slot', insertText: '${1:slot}', documentation: 'A named slot of the shell.' },
    { label: 'slot contributes', insertText: '${1:slot} contributes ${2:Name}', documentation: 'A slot other declarations contribute to, such as `navigation contributes Navigation`.' },
    { label: 'navigation topbar', insertText: 'navigation ${1|topbar,sidebar|}\n    logo image "${2:/logo.svg}" title "${3:Application}"\n    item ${4:id} title "${5:Title}" target ${6:Module}', documentation: 'The application\'s navigation region: a logo and items that target a module or feature.' },
    { label: 'arrangement', insertText: 'arrangement flow\n    column\n        ${1:slot}', documentation: 'How the slots are placed.' },
];

export const toolbarItems: CompletionEntry[] = [
    { label: 'button', insertText: 'button ${1:id} title "${2:Title}" slice ${3:Module.Feature.Slice}', documentation: 'A button that invokes a State Change slice. The slice names the command and the dialog it opens over the page.' },
    { label: 'button (with emphasis)', insertText: 'button ${1:id} title "${2:Title}" slice ${3:Module.Feature.Slice} emphasis ${4|primary,secondary,quiet|} icon ${5:icon}', documentation: 'A toolbar button with an emphasis and an icon.' },
    { label: 'item action', insertText: 'item ${1:name} action ${2:Command}', documentation: 'A toolbar entry that runs a command.' },
    { label: 'item navigate', insertText: 'item ${1:name} navigate to ${2:Screen}', documentation: 'A toolbar entry that navigates to a screen.' },
];

export const navigationItems: CompletionEntry[] = [
    { label: 'logo', insertText: 'logo image "${1:/logo.svg}" title "${2:Application}"', documentation: 'The application logo. Declared once, in a layout.' },
    { label: 'item', insertText: 'item ${1:id} title "${2:Title}" target ${3:Module.Feature}', documentation: 'A navigation item with a title, an optional `icon`, a `target` module or feature and an optional rendering `slot`. Sidebar items may nest `item` lines.' },
    { label: 'item (with slot)', insertText: 'item ${1:id} title "${2:Title}" icon ${3:icon} target ${4:Module.Feature} slot ${5:content}', documentation: 'A navigation item that renders its target into a named layout slot.' },
];

export const templateItems: CompletionEntry[] = [
    { label: 'fits slot', insertText: 'fits slot ${1:content}', documentation: 'The slot of the shell the template fits.' },
    ...layoutItems,
    { label: 'on', insertText: 'on ${1|enter,leave,load,unload|}\n    ${2:refresh query}', documentation: 'Wires what happens on an interaction.' },
];

export const interactionNames = interactions;

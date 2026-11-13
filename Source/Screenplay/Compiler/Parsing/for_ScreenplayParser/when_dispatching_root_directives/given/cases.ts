// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The root directive characterization matrix, shared with the native specification of the same name. Every case ends in a
// sibling concept ('concept Marker : String') so recovery and the reader position after each directive are pinned. Each
// expectation is the diagnostics (severity, code, line:column, text) in order, then the application state that was built.
export interface Case {
    readonly name: string;
    readonly scope: readonly string[];
    readonly source: string;
    readonly expected: string;
}

export const rootDirectiveCases: readonly Case[] = [
    {
        name: 'every preamble directive once',
        scope: [],
        source: 'command reads protected\ntime zone "Europe/Oslo"\nreaction batches atomic\ndomain Shop',
        expected: 'commandReadMode=Protected timeZone=Europe/Oslo/false reaction=Atomic domain=Shop imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'command reads twice',
        scope: [],
        source: 'command reads protected\ncommand reads protected',
        expected: 'error PLAY0780 2:1 Declare \'command reads protected\' once at the application root.\ncommandReadMode=Protected timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'command reads legacy after protected',
        scope: [],
        source: 'command reads protected\ncommand reads legacy',
        expected: 'error PLAY0780 2:1 Declare \'command reads protected\' once at the application root.\ncommandReadMode=Protected timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'command reads with an extra word',
        scope: [],
        source: 'command reads protected extra',
        expected: 'error PLAY0780 1:1 Declare \'command reads protected\' once at the application root.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'command named Submit at the root',
        scope: [],
        source: 'command Submit\n  value String',
        expected: 'error PLAY0001 1:1 Unexpected \'command\' at the top level - expected domain, import, concept, type, policy, audience, availability, persona, sticky note, authentication, onboarding, identity, context, module, seed, trigger, behavior, ui profile, design system, theme, sample data, layout, statemachine or workflow\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'time zone twice',
        scope: [],
        source: 'time zone "Europe/Oslo"\ntime zone "Asia/Tokyo"',
        expected: 'error PLAY0812 2:1 Declare \'time zone "<id>"\' or \'time zone configurable default "<id>"\' once at the application root.\ncommandReadMode=none timeZone=Europe/Oslo/false reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'time zone nonsense',
        scope: [],
        source: 'time zone nonsense',
        expected: 'error PLAY0812 1:1 Declare \'time zone "<id>"\' or \'time zone configurable default "<id>"\' once at the application root.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'time zone configurable default',
        scope: [],
        source: 'time zone configurable default "Europe/Oslo"',
        expected: 'commandReadMode=none timeZone=Europe/Oslo/true reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'time zone with an unknown id',
        scope: [],
        source: 'time zone "Not/AZone"',
        expected: 'commandReadMode=none timeZone=Not/AZone/false reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'reaction batches twice',
        scope: [],
        source: 'reaction batches atomic\nreaction batches atomic',
        expected: 'error PLAY0693 2:1 Declare \'reaction batches atomic\' once at the application root.\ncommandReadMode=none timeZone=none reaction=Atomic domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'reaction batches eventual',
        scope: [],
        source: 'reaction batches eventual',
        expected: 'error PLAY0693 1:1 Declare \'reaction batches atomic\' once at the application root.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'domain first',
        scope: [],
        source: 'domain Shop',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=Shop imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'domain after an import',
        scope: [],
        source: 'import A.B\ndomain Shop',
        expected: 'error PLAY0004 2:1 \'domain\' must be declared before any other construct\ncommandReadMode=none timeZone=none reaction=none domain=Shop imports=[A.B@] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'domain after a concept',
        scope: [],
        source: 'concept Money : Decimal\ndomain Shop',
        expected: 'error PLAY0004 2:1 \'domain\' must be declared before any other construct\ncommandReadMode=none timeZone=none reaction=none domain=Shop imports=[] fileImports=[] concepts=[Money,Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'domain after a policy',
        scope: [],
        source: 'policy Admin\ndomain Shop',
        expected: 'error PLAY0004 2:1 \'domain\' must be declared before any other construct\ncommandReadMode=none timeZone=none reaction=none domain=Shop imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:1,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'domain after a module',
        scope: [],
        source: 'module M\ndomain Shop',
        expected: 'error PLAY0004 2:1 \'domain\' must be declared before any other construct\ncommandReadMode=none timeZone=none reaction=none domain=Shop imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'domain twice',
        scope: [],
        source: 'domain Shop\ndomain Other',
        expected: 'error PLAY0003 2:1 The document already declares a domain - a document can have at most one\ncommandReadMode=none timeZone=none reaction=none domain=Shop imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'qualified import',
        scope: [],
        source: 'import A.B',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[A.B@] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'import with an origin',
        scope: [],
        source: 'import A.B from "x"',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[A.B@x] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'import with a blank origin',
        scope: [],
        source: 'import A.B from ""',
        expected: 'error PLAY0005 1:1 An import origin must be a nonblank quoted string\nerror PLAY0005 1:1 An event or import origin must be a nonblank quoted string.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[A.B@] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'malformed import',
        scope: [],
        source: 'import 1bad',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[1bad@] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'file import',
        scope: [],
        source: 'import "other.play"',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[other.play] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'file import then import',
        scope: [],
        source: 'import "other.play"\nimport A.B',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[A.B@] fileImports=[other.play] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'runs at the root',
        scope: [],
        source: 'runs Foo\n  value String',
        expected: 'error PLAY0647 1:1 \'runs as\' belongs directly in a reaction body, not under a trigger or invocation.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'unknown scope keyword slice',
        scope: [],
        source: 'slice StateChange S\n  value String',
        expected: 'error PLAY0001 1:1 Unexpected \'slice\' at the top level - \'slice\' belongs in a module or feature; wrap it in one, or import this file from inside one\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'unknown scope keyword feature with a body',
        scope: [],
        source: 'feature F\n  description "x"',
        expected: 'error PLAY0001 1:1 Unexpected \'feature\' at the top level - \'feature\' belongs in a module or feature; wrap it in one, or import this file from inside one\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'unknown word',
        scope: [],
        source: 'frobnicate\n  nested 1',
        expected: 'error PLAY0001 1:1 Unexpected \'frobnicate\' at the top level - expected domain, import, concept, type, policy, audience, availability, persona, sticky note, authentication, onboarding, identity, context, module, seed, trigger, behavior, ui profile, design system, theme, sample data, layout, statemachine or workflow\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'concept',
        scope: [],
        source: 'concept Money : Decimal',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Money,Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'type',
        scope: [],
        source: 'type Address\n  street String',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[Address] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'policy',
        scope: [],
        source: 'policy Admin',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:1,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'persona',
        scope: [],
        source: 'persona Buyer',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[Buyer] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'sticky note',
        scope: [],
        source: 'sticky note on personas\n  goal "x"',
        expected: 'error PLAY0032 2:3 Unexpected \'goal\' in sticky note body - expected description, colour, position or size\nerror PLAY0030 1:1 A sticky note needs a description (the text on it) and a position\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'purpose',
        scope: [],
        source: 'purpose Billing',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:1,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'example',
        scope: [],
        source: 'example Basic',
        expected: 'error PLAY0518 1:1 Invalid example declaration \'example Basic\' - expected \'example <Name> : <EventOrCommandOrReadModel>\'\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:1,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'example with a subject',
        scope: [],
        source: 'example Basic : Opened',
        expected: 'error PLAY0520 1:1 Example \'Basic\' type \'Opened\' is unknown or ambiguous; qualify a declared event, command, or read model.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:1,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'policy twice',
        scope: [],
        source: 'policy Admin\npolicy Admin',
        expected: 'error PLAY0168 2:1 Duplicate declaration of policy \'Admin\' - policy names must be unique\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:2,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'identity twice',
        scope: [],
        source: 'identity\nidentity',
        expected: 'error PLAY0634 2:1 The document already declares an identity block - a document can have at most one\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:true,context:false}'
    },
    {
        name: 'context twice',
        scope: [],
        source: 'context\ncontext',
        expected: 'error PLAY0854 2:1 Declare \'context\' once at the application root.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:true}'
    },
    {
        name: 'persona twice',
        scope: [],
        source: 'persona Buyer\npersona Buyer',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[Buyer,Buyer] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'eventsource',
        scope: [],
        source: 'eventsource Basket',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:1} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'system',
        scope: [],
        source: 'system Billing',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:1,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'statemachine',
        scope: [],
        source: 'statemachine Life',
        expected: 'error PLAY0834 1:1 State machine \'Life\' must follow a read model: \'follows <ReadModel>\'.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:1,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'workflow',
        scope: [],
        source: 'workflow Flow',
        expected: 'error PLAY0843 1:1 Workflow \'Flow\' must name its subject: \'for <ReadModel>\'.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:1,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'seed',
        scope: [],
        source: 'seed "Starter"',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:1,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'ui profile',
        scope: [],
        source: 'ui profile Compact',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'sample data',
        scope: [],
        source: 'sample data Pack',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'theme',
        scope: [],
        source: 'theme Nordic',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:1,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'design system',
        scope: [],
        source: 'design system Nordic\n  default theme Nordic\ntheme Nordic\n  implements Nordic',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:1,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'trigger',
        scope: [],
        source: 'trigger Ping',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:1,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'layout',
        scope: [],
        source: 'layout Shell',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'template',
        scope: [],
        source: 'template Card',
        expected: 'error PLAY0001 1:1 Unexpected \'template\' at the top level - expected domain, import, concept, type, policy, audience, availability, persona, sticky note, authentication, onboarding, identity, context, module, seed, trigger, behavior, ui profile, design system, theme, sample data, layout, statemachine or workflow\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'behavior',
        scope: [],
        source: 'behavior Confirm',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'exposure',
        scope: [],
        source: 'exposure Items',
        expected: 'error PLAY0621 1:1 Invalid exposure declaration \'exposure Items\' - expected \'exposure for <Owner>\'\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:1,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'instance',
        scope: [],
        source: 'instance Board',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:1,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'identity',
        scope: [],
        source: 'identity',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:true,context:false}'
    },
    {
        name: 'authentication',
        scope: [],
        source: 'authentication',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'onboarding',
        scope: [],
        source: 'onboarding',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'context',
        scope: [],
        source: 'context',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:true}'
    },
    {
        name: 'module',
        scope: [],
        source: 'module M',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'two modules',
        scope: [],
        source: 'module M\nmodule N',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M,N] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'constructs after the domain are accepted in any order',
        scope: [],
        source: 'domain Shop\nconcept Money : Decimal\ntype Address\n  street String\nmodule M',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=Shop imports=[] fileImports=[] concepts=[Money,Marker] types=[Address] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: command reads',
        scope: ['M'],
        source: 'command reads protected',
        expected: 'error PLAY0780 1:1 Declare \'command reads protected\' once at the application root.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: time zone',
        scope: ['M'],
        source: 'time zone "Europe/Oslo"',
        expected: 'error PLAY0812 1:1 Declare \'time zone "<id>"\' or \'time zone configurable default "<id>"\' once at the application root.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: reaction batches',
        scope: ['M'],
        source: 'reaction batches atomic',
        expected: 'error PLAY0693 1:1 Declare \'reaction batches atomic\' once at the application root.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: domain',
        scope: ['M'],
        source: 'domain Shop',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=Shop imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: purpose',
        scope: ['M'],
        source: 'purpose Billing',
        expected: 'warning PLAY0600 1:1 Unknown purpose \'Billing\' - declare it with \'purpose Billing\'\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: example',
        scope: ['M'],
        source: 'example Basic',
        expected: 'error PLAY0518 1:1 Invalid example declaration \'example Basic\' - expected \'example <Name> : <EventOrCommandOrReadModel>\'\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: file import',
        scope: ['M'],
        source: 'import "other.play"',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: qualified import',
        scope: ['M'],
        source: 'import A.B',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[A.B@] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: module restated',
        scope: ['M'],
        source: 'module M\n  description "x"',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: other module',
        scope: ['M'],
        source: 'module Other',
        expected: 'error PLAY0459 1:1 This file is imported into module \'M\', so it cannot declare module \'Other\' - import it at the top level of a document instead\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: context',
        scope: ['M'],
        source: 'context',
        expected: 'error PLAY0854 1:1 Declare \'context\' once at the application root.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: slice',
        scope: ['M'],
        source: 'slice StateChange S\n  value String',
        expected: 'error PLAY0460 1:1 Unexpected \'slice\' in a file imported into module \'M\' - expected an application declaration or description, documentation, depends on <Name>, authorize, available, provide, import, screen template, dialog template, form, contribute, outlet, feature, example, \'on <trigger>\' or \'uses <Behavior>\'\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: description',
        scope: ['M'],
        source: 'description "x"',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: runs',
        scope: ['M'],
        source: 'runs Foo',
        expected: 'error PLAY0647 1:1 \'runs as\' belongs directly in a reaction body, not under a trigger or invocation.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M: unknown word',
        scope: ['M'],
        source: 'frobnicate\n  nested 1',
        expected: 'error PLAY0460 1:1 Unexpected \'frobnicate\' in a file imported into module \'M\' - expected an application declaration or description, documentation, depends on <Name>, authorize, available, provide, import, screen template, dialog template, form, contribute, outlet, feature, example, \'on <trigger>\' or \'uses <Behavior>\'\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: command reads',
        scope: ['M', 'F'],
        source: 'command reads protected',
        expected: 'error PLAY0780 1:1 Declare \'command reads protected\' once at the application root.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: time zone',
        scope: ['M', 'F'],
        source: 'time zone "Europe/Oslo"',
        expected: 'error PLAY0812 1:1 Declare \'time zone "<id>"\' or \'time zone configurable default "<id>"\' once at the application root.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: reaction batches',
        scope: ['M', 'F'],
        source: 'reaction batches atomic',
        expected: 'error PLAY0693 1:1 Declare \'reaction batches atomic\' once at the application root.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: domain',
        scope: ['M', 'F'],
        source: 'domain Shop',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=Shop imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: purpose',
        scope: ['M', 'F'],
        source: 'purpose Billing',
        expected: 'warning PLAY0600 1:1 Unknown purpose \'Billing\' - declare it with \'purpose Billing\'\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: example',
        scope: ['M', 'F'],
        source: 'example Basic',
        expected: 'error PLAY0518 1:1 Invalid example declaration \'example Basic\' - expected \'example <Name> : <EventOrCommandOrReadModel>\'\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: file import',
        scope: ['M', 'F'],
        source: 'import "other.play"',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: qualified import',
        scope: ['M', 'F'],
        source: 'import A.B',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[A.B@] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: module restated',
        scope: ['M', 'F'],
        source: 'module M\n  description "x"',
        expected: 'error PLAY0459 1:1 This file is imported into feature \'M.F\', so it cannot declare module \'M\' - import it at the top level of a document instead\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: other module',
        scope: ['M', 'F'],
        source: 'module Other',
        expected: 'error PLAY0459 1:1 This file is imported into feature \'M.F\', so it cannot declare module \'Other\' - import it at the top level of a document instead\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: context',
        scope: ['M', 'F'],
        source: 'context',
        expected: 'error PLAY0854 1:1 Declare \'context\' once at the application root.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: slice',
        scope: ['M', 'F'],
        source: 'slice StateChange S\n  value String',
        expected: 'warning PLAY0029 2:3 Unknown construct \'value\' in slice \'S\'\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: description',
        scope: ['M', 'F'],
        source: 'description "x"',
        expected: 'commandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: runs',
        scope: ['M', 'F'],
        source: 'runs Foo',
        expected: 'error PLAY0647 1:1 \'runs as\' belongs directly in a reaction body, not under a trigger or invocation.\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    },
    {
        name: 'placed in M.F: unknown word',
        scope: ['M', 'F'],
        source: 'frobnicate\n  nested 1',
        expected: 'error PLAY0460 1:1 Unexpected \'frobnicate\' in a file imported into feature \'M.F\' - expected an application declaration or description, documentation, depends on <Name>, authorize, available, provide, import, feature, slice, contribute, outlet, example, \'on <trigger>\' or \'uses <Behavior>\'\ncommandReadMode=none timeZone=none reaction=none domain=none imports=[] fileImports=[] concepts=[Marker] types=[] personas=[] modules=[M] counts={policies:0,purposes:0,stickyNotes:0,examples:0,seeds:0,uiProfiles:0,sampleDataPacks:0,themes:0,triggers:0,layouts:0,templates:0,behaviors:0,exposures:0,instanceContributions:0,declaredTriggers:0,systems:0,stateMachines:0,workflows:0,eventSources:0} present={authentication:false,onboarding:false,identity:false,context:false}'
    }
];

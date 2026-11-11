// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { planCompletions } from '../completion-planner';

// What is offered on an empty line at the end of the text - which must be only what that scope can hold.
const offered = (text: string): string[] => {
    const lines = text.split('\n');
    const last = lines.length - 1;
    const plan = planCompletions(lines, last, lines[last]);
    return plan.kind === 'entries' ? plan.entries.map(entry => entry.label).sort() : [`<${plan.kind}>`];
};

const scopes: Record<string, [string, string[]]> = {
    'the root': ['', ['agents', 'context', 'time zone', 'time zone configurable default', 'identity', 'authentication', 'onboarding', 'behavior', 'concept', 'concept (pii with reason)', 'concept (enum)', 'design system', 'domain', 'eventsource', 'example', 'import', 'import from', 'import "…"', 'layout', 'module', 'persona', 'policy', 'sample data', 'secret', 'seed', 'sink', 'statemachine', 'sticky', 'storyline', 'system', 'theme', 'trigger', 'type', 'ui profile', 'user', 'workflow']],
    'a domain': ['domain Catalog\n  ', ['context', 'time zone', 'time zone configurable default', 'identity', 'authentication', 'onboarding', 'behavior', 'concept', 'concept (pii with reason)', 'concept (enum)', 'design system', 'eventsource', 'example', 'import', 'import from', 'import "…"', 'layout', 'module', 'persona', 'policy', 'sample data', 'secret', 'seed', 'sink', 'statemachine', 'sticky', 'storyline', 'system', 'theme', 'trigger', 'type', 'ui profile', 'user', 'workflow']],
    'a module': ['module M\n  ', ['authorize', 'contribute', 'depends on', 'description', 'documentation', 'dialog template', 'example', 'feature', 'form', 'import "…"', 'on', 'outlet extends', 'screen', 'screen template', 'uses']],
    'a feature': ['module M\n  feature F\n    ', ['authorize', 'contribute', 'depends on', 'description', 'documentation', 'example', 'feature', 'import "…"', 'on', 'outlet extends', 'slice Automation', 'slice StateChange', 'slice StateView', 'slice Translate', 'uses']],
    'a state change slice': ['module M\n  feature F\n    slice StateChange S\n      ', ['command', 'constraint', 'documentation', 'event', 'event from', 'event generation', 'example', 'file', 'operation', 'public event', 'screen', 'shows event', 'shows readmodel', 'specification']],
    'a state view slice': ['module M\n  feature F\n    slice StateView S\n      ', ['documentation', 'event', 'event from', 'event generation', 'example', 'file', 'graph', 'projection', 'projection graph', 'public event', 'query', 'query observable', 'readmodel', 'reducer', 'screen', 'shows event', 'shows readmodel', 'specification']],
    'an automation slice': ['module M\n  feature F\n    slice Automation S\n      ', ['command', 'documentation', 'event', 'event from', 'event generation', 'example', 'file', 'operation', 'public event', 'reaction', 'readmodel', 'reducer', 'shows event', 'shows readmodel', 'specification']],
    'a translate slice': ['module M\n  feature F\n    slice Translate S\n      ', ['capture', 'direction', 'documentation', 'event', 'event from', 'event generation', 'example', 'file', 'public event', 'shows event', 'shows readmodel', 'specification']],
    'a read model': ['module M\n  feature F\n    slice StateView S\n      readmodel R\n        ', ['description', 'documentation', 'file', 'passive', 'property', 'sink']],
    'a reducer': ['module M\n  feature F\n    slice StateView S\n      reducer R => V\n        ', ['description', 'from', 'on']],
    'a form': ['module M\n  form F for C\n    ', ['description', 'field', 'field (picker)', 'on', 'populate from item', 'populate via query', 'uses']],
    'a contribution': ['module M\n  contribute to Navigation\n    ', ['label', 'navigate to', 'order']],
    'a behavior': ['behavior B\n  ', ['description', 'on', 'order', 'parameter']],
    'a persona': ['persona P\n  ', ['description', 'goal', 'policy', 'position', 'size']],
    'application context': ['context\n  ', ['time zone member', 'description']],
    'agent exposure': ['agents\n  ', ['tokens personal', 'command', 'query']],
    'an exposed command': ['agents\n  command Sales.Issue\n    ', ['scope write', 'description']],
    'an exposed query': ['agents\n  query Sales.List.One\n    ', ['scope read', 'scope write', 'description']],
    'authentication': ['authentication\n  ', ['provider', 'provider (named)']],
    'onboarding': ['onboarding\n  ', ['signup (creates tenant)', 'signup (joins tenant)', 'invite tenant', 'invite member', 'legal terms', 'offering', 'emails']],
    'a member invitation': ['onboarding\n  invite member\n    ', ['by', 'grant']],
    'legal terms': ['onboarding\n  legal terms\n    ', ['file']],
    'emails': ['onboarding\n  emails\n    ', ['invitation', 'welcome', 'signupDeclined', 'images']],
    'a seed': ['seed\n  ', ['for', 'position']],
    'a theme': ['theme T\n  ', ['compatible with', 'implements', 'palette', 'values']],
    'a ui profile': ['ui profile P\n  ', ['appearance', 'identity', 'layout', 'new screens use template', 'packages', 'root template', 'root template blueprint', 'sample data', 'target platform', 'target size', 'theme', 'theme (package-provided)']],
    'a ui profile root template': ['ui profile P\n  root template T\n    ', ['option']],
    'a ui profile appearance block': ['ui profile P\n  appearance\n    ', ['color scheme']],
    'a ui profile identity block': ['ui profile P\n  identity\n    ', ['logo icon', 'logo image', 'logo initials', 'name', 'show name']],
    'a sample data pack': ['sample data P\n  ', ['locale', 'shape', 'title']],
    'a sample data shape': ['sample data P\n  shape S\n    ', ['row']],
    'a sample data row': ['sample data P\n  shape S\n    row\n      ', ['collection', 'field']],
    'a sample collection header': ['sample data P\n  shape S\n    row\n      lines\n        ', ['row']],
    'a nested sample row': ['sample data P\n  shape S\n    row\n      lines\n        row\n          ', ['collection', 'field']],
    'a recursive sample collection header': ['sample data P\n  shape S\n    row\n      lines\n        row\n          children\n            ', ['row']],
    'a ui profile sample data block': ['ui profile P\n  sample data\n    ', ['active', 'bind', 'shape maps to']],
    'an outlet declaration': ['layout L\n  outlet sidebar of navigation\n    ', ['default button', 'default filter', 'default item', 'default stat', 'default tab', 'logo']],
    'an outlet change': ['module M\n  outlet sidebar extends\n    ', ['button', 'button (opens)', 'button (runs)', 'filter', 'group', 'hide', 'item', 'stat', 'tab']],
    'a toolbar': ['module M\n  feature F\n    slice StateView S\n      screen Sc\n        toolbar t\n          ', ['button', 'button (opens)', 'button (runs)', 'button (with emphasis)', 'item action', 'item navigate']],
    'a layout': ['layout L\n  ', ['arrangement', 'outlet', 'outlet of navigation', 'slot']],
};

describe('when completing an empty line in each scope', () => {
    for (const [scope, [text, expected]] of Object.entries(scopes)) {
        const supportsPurpose = ['the root', 'a domain', 'a module', 'a feature', 'a state change slice', 'a state view slice', 'an automation slice', 'a translate slice'].includes(scope);
        it(`should offer only what ${scope} can hold`, () => {
            offered(text).should.deep.equal([...expected, ...supportsPurpose ? ['purpose'] : [], ...['the root', 'a domain'].includes(scope) ? ['command reads protected', 'reaction batches atomic'] : []].sort());
        });
    }

    it('should offer the events when a seed names the event source', () => {
        offered('seed\n  for "id"\n    ').should.deep.equal(['<events>']);
    });

    it('should offer nothing generic where nothing is known to belong', () => {
        offered('module M\n  feature F\n    slice StateChange S\n      event E\n        property String\n          ').should.deep.equal([]);
    });
});

describe('when completing a command', () => {
    it('should expand the whole block from what the command and its events declare', () => {
        offered('concept ProductId : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command AddProduct\n        ')
            .should.include.members(['productId ProductId identifier', 'property', 'identifier property']);
    });
});

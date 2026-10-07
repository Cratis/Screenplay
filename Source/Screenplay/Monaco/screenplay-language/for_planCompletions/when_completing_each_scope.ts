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
    'the root': ['', ['authentication', 'behavior', 'concept', 'concept (@pii with reason)', 'concept (enum)', 'domain', 'eventsource', 'import', 'import "…"', 'layout', 'module', 'persona', 'policy', 'seed', 'system', 'theme', 'trigger', 'type', 'ui profile']],
    'a domain': ['domain Catalog\n  ', ['authentication', 'behavior', 'concept', 'concept (@pii with reason)', 'concept (enum)', 'eventsource', 'import', 'import "…"', 'layout', 'module', 'persona', 'policy', 'seed', 'system', 'theme', 'trigger', 'type', 'ui profile']],
    'a module': ['module M\n  ', ['authorize', 'contribute', 'description', 'dialog template', 'feature', 'form', 'import "…"', 'on', 'screen', 'screen template', 'uses']],
    'a feature': ['module M\n  feature F\n    ', ['authorize', 'contribute', 'description', 'feature', 'import "…"', 'on', 'slice Automation', 'slice StateChange', 'slice StateView', 'slice Translate', 'uses']],
    'a state change slice': ['module M\n  feature F\n    slice StateChange S\n      ', ['command', 'constraint', 'event', 'event generation', 'file', 'operation', 'screen', 'specification']],
    'a state view slice': ['module M\n  feature F\n    slice StateView S\n      ', ['event', 'event generation', 'file', 'projection', 'query', 'query observable', 'readmodel', 'reducer', 'screen', 'specification']],
    'an automation slice': ['module M\n  feature F\n    slice Automation S\n      ', ['command', 'event', 'event generation', 'file', 'operation', 'reaction', 'readmodel', 'reducer', 'specification']],
    'a translate slice': ['module M\n  feature F\n    slice Translate S\n      ', ['capture', 'event', 'event generation', 'file', 'specification']],
    'a read model': ['module M\n  feature F\n    slice StateView S\n      readmodel R\n        ', ['description', 'file', 'property']],
    'a reducer': ['module M\n  feature F\n    slice StateView S\n      reducer R => V\n        ', ['description', 'on']],
    'a form': ['module M\n  form F for C\n    ', ['field', 'on', 'populate from item', 'populate via query', 'uses']],
    'a contribution': ['module M\n  contribute to Navigation\n    ', ['label', 'navigate to', 'order']],
    'a behavior': ['behavior B\n  ', ['description', 'on', 'order', 'parameter']],
    'a persona': ['persona P\n  ', ['description', 'policy']],
    'authentication': ['authentication\n  ', ['provider', 'provider (named)']],
    'a seed': ['seed\n  ', ['for']],
    'a theme': ['theme T\n  ', ['compatible with']],
    'a ui profile': ['ui profile P\n  ', ['layout', 'packages', 'target platform', 'target size', 'theme']],
    'a layout': ['layout L\n  ', ['arrangement', 'slot', 'slot contributes']],
};

describe('when completing an empty line in each scope', () => {
    for (const [scope, [text, expected]] of Object.entries(scopes)) {
        it(`should offer only what ${scope} can hold`, () => {
            offered(text).should.deep.equal([...expected].sort());
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

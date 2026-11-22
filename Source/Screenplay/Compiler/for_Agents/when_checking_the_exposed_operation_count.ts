// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parseForAuthoring } from '../ScreenplayCompiler';

const code = 'PLAY1167';

function source(count: number): string {
    const operations = Array.from({ length: count }, (_, index) => ({ kind: index % 2 === 0 ? 'command' : 'query', name: `Operation${index}` }));
    const declarations = operations.map(operation => operation.kind === 'command' ? `      command ${operation.name}\n        id ItemId identifier` : `      query ${operation.name} => Row[]`).join('\n');
    const exposures = operations.map(operation => `  ${operation.kind} ${operation.name}\n    scope ${operation.kind === 'command' ? 'write' : 'read'}\n    description "Operation"`).join('\n');
    return `concept ItemId : Uuid\npolicy Staff\n  require role "Staff"\nmodule M\n  authorize Staff\n  feature F\n    slice StateChange S\n      readmodel Row\n        id ItemId\n${declarations}\nagents\n  tokens personal\n${exposures}`;
}

describe('when checking the exposed operation count', () => {
    it.each([0, 1, 19, 20, 21, 30])('should warn only above twenty distinct operations (%s)', count => {
        const result = parseForAuthoring(source(count), 'agents.play', undefined, true);
        result.success.should.equal(true);
        result.diagnostics.filter(diagnostic => diagnostic.code === code).length.should.equal(count > 20 ? 1 : 0);
    });

    it('should not count unexposed declarations', () => {
        const text = source(21);
        const model = text.slice(0, text.indexOf('agents\n'));
        parseForAuthoring(model, 'agents.play', undefined, true).diagnostics.should.deep.equal([]);
        parseForAuthoring(`${model}agents\n  tokens personal`, 'agents.play', undefined, true).diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0977', 'PLAY0976']);
    });

    it('should report the count at the block with typed arguments', () => {
        const text = source(21);
        const warnings = parseForAuthoring(text, 'agents.play', undefined, true).diagnostics.filter(diagnostic => diagnostic.code === code);
        warnings.length.should.equal(1);
        warnings[0].severity.should.equal('warning');
        warnings[0].message.should.equal('The agents block exposes 21 operations; expose at most 20 focused operations to keep agent tool selection effective');
        warnings[0].location.should.deep.equal({ path: 'agents.play', line: text.split('\n').indexOf('agents') + 1, column: 1 });
        warnings[0].arguments.should.deep.equal({ count: { kind: 'text', value: '21' }, recommendedMaximum: { kind: 'text', value: '20' } });
    });
});

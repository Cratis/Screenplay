// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse, parseForAuthoring } from '../../ScreenplayCompiler';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';
import { compileApplication } from '../PlayApplicationAssembly';

const projection = 'module M\n  feature F\n    slice StateView View\n      projection P\n        from E\n        from E\n    slice StateChange Write\n      event E';

const cases = [
    { name: 'a state view projects from its right', source: projection, expected: ['PLAY0516@5'] },
    { name: 'a reaction is triggered from its right', source: 'module M\n  feature F\n    slice Automation React\n      reaction R\n        when E\n    slice StateChange Write\n      event E', expected: ['PLAY0516@5'] },
    { name: 'modules use each others events', source: 'module A\n  feature F\n    slice StateView ViewA\n      event EA\n      projection PA\n        from EB\nmodule B\n  feature F\n    slice StateView ViewB\n      event EB\n      projection PB\n        from EA', expected: ['PLAY0517@6'] },
    { name: 'a parent slice consumes from its sub feature', source: 'module M\n  feature F\n    feature Child\n      slice StateChange Write\n        event E\n    slice StateView View\n      projection P\n        from E', expected: ['PLAY0516@8'] },
    { name: 'the producer is external', source: 'import Contracts.E\nmodule M\n  feature F\n    slice StateView View\n      event Own\n      projection P\n        from E\n        from Own', expected: [] },
    { name: 'the producer is to the left', source: 'module M\n  feature F\n    slice StateChange Write\n      event E\n    slice StateView View\n      projection P\n        from E', expected: [] },
];
for (const test of cases) {
    describe(`when ${test.name}`, () => {
        it('should report precisely the timeline findings', () => {
            const result = parse(test.source);
            result.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(test.expected);
            result.diagnostics.every(diagnostic => diagnostic.severity === 'information').should.be.true;
            result.success.should.be.true;
        });
    });
}

describe('when a folder has no ordering root', () => {
    it('should not invent ranks or report information per physical file', () => {
        const result = compileApplication(new Map([
            ['a.play', projection], ['z.play', 'module Z'],
        ]));
        result.diagnostics.should.deep.equal([]);
    });
});

describe('when the pass runs after merging', () => {
    it('should leave syntax bytes unchanged and not run during physical parsing', () => {
        const before = parseForAuthoring(projection, undefined, [], false);
        const after = parse(projection);
        JSON.stringify(toSyntaxJson(after.value)).should.equal(JSON.stringify(toSyntaxJson(before.value)));
        JSON.stringify(after.value).should.equal(JSON.stringify(before.value));
        before.diagnostics.should.deep.equal([]);
        after.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0516']);
    });
});

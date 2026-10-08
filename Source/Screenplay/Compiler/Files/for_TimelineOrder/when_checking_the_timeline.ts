// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';
import { compileApplication, parsePlacedDocuments } from '../PlayApplicationAssembly';
import { timelineOrderDiagnostics } from '../TimelineOrder';

const projection = 'module M\n  feature F\n    slice StateView View\n      projection P\n        from E\n        from E\n    slice StateChange Write\n      event E';

const cases = [
    { name: 'feedback preserves the backward event edge with a builder drawn first', source: 'module M\n  feature F\n    slice StateView Build\n      reducer R => Items\n        on E\n    slice StateChange Read\n      event E\n      command C\n        reads Items', expected: ['PLAY0516@5'] },
    { name: 'reads resolve projection variants', source: 'module M\n  feature F\n    slice StateChange Read\n      command C\n        reads Items\n    slice StateView Build\n      event E\n      projection P\n        variant Items\n          enters on E\n          from E', expected: ['PLAY0516@5'] },
    { name: 'reads report own sub feature producers', source: 'module M\n  feature F\n    slice StateChange Read\n      command C\n        reads Items\n    feature Child\n      slice StateView Build\n        readmodel Items', expected: ['PLAY0516@5'] },
    { name: 'a reducer uses its right once at the first rule', source: 'module M\n  feature F\n    slice StateView View\n      reducer R => Items\n        on E\n        on E\n    slice StateChange Write\n      event E', expected: ['PLAY0516@5'] },
    { name: 'a command reads a later builder instead of an earlier declaration', source: 'module M\n  feature F\n    slice StateChange Shape\n      readmodel Items\n    slice StateChange Read\n      command C\n        reads Items as first\n        reads Items as second\n    slice StateView Build\n      event E\n      projection P => Items\n        from E', expected: ['PLAY0516@7'] },
    { name: 'a reaction reads a later builder', source: 'module M\n  feature F\n    slice Automation Read\n      reaction R\n        every 15 minutes\n          reads Items\n    slice StateView Build\n      event E\n      reducer P => Items\n        on E', expected: ['PLAY0516@6'] },
    { name: 'a command reads a projection of its own events', source: 'module M\n  feature F\n    slice StateChange Read\n      event E\n      command C\n        reads Items\n    slice StateView Build\n      readmodel Items\n      projection P => Items\n        from E', expected: [] },
    { name: 'a command reads a reducer of its own events', source: 'module M\n  feature F\n    slice StateChange Read\n      event E\n      command C\n        reads Items\n    slice StateView Build\n      readmodel Items\n      reducer P => Items\n        on E', expected: [] },
    { name: 'non feedback reads form a cycle', source: 'module M\n  feature F\n    slice StateChange A\n      readmodel Left\n      command C\n        reads Right\n    slice StateChange B\n      readmodel Right\n      command D\n        reads Left', expected: ['PLAY0517@6'] },
    { name: 'events and read models with the same name are distinct', source: 'module M\n  feature F\n    slice Automation Read\n      command C\n        reads Items\n      reducer R => Other\n        on Items\n    slice StateView Build\n      readmodel Items\n    slice StateChange Write\n      event Items', expected: ['PLAY0516@5', 'PLAY0516@7'] },
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
            for (const group of result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0517')) {
                group.message.should.contain("depend on each other's events or read models; reordering these members cannot make every dependency flow left to right.");
            }
            result.success.should.be.true;
        });
    });
}

describe('when read model ownership has duplicate slice scopes', () => {
    it('should use the earliest owner rather than the last duplicate for feedback', () => {
        const application = parse('module M\n  feature F\n    slice StateChange Read\n      event E\n      command C\n        reads Items\n    slice StateView Build\n      readmodel Items\n    slice StateView Build\n      projection Other\n        from E').value;
        timelineOrderDiagnostics(application).map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0517@6']);
    });
});

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
        const after = compileApplication(new Map([
            ['application.play', 'module M\n  feature F\n    import "z.play"\n    import "a.play"'],
            ['z.play', 'slice StateView View\n  projection P\n    from E'],
            ['a.play', 'slice StateChange Write\n  event E'],
        ]), ['application.play']);
        const before = parsePlacedDocuments(after.documents);
        JSON.stringify(toSyntaxJson(after.value)).should.equal(JSON.stringify(toSyntaxJson(before.value)));
        JSON.stringify(after.value).should.equal(JSON.stringify(before.value));
        before.diagnostics.should.deep.equal([]);
        after.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0516']);
    });
});

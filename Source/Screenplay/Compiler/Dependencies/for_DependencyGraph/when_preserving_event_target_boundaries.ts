// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { graphOf } from './given/a_model';
import { DependencyGraph } from '../DependencyGraph';
import { compileApplication } from '../../Files/PlayApplicationAssembly';

const local = '      public event E\n';
const projection = '      projection P => E\n        from Input\n';
const prefix = 'module M\n  feature F\n    slice Translate Publish\n      direction outbound\n';
const cases: readonly [name: string, body: string, suffix: string, target: string, builder: boolean][] = [
    ['same slice readmodel', local + '      readmodel E\n' + projection, '', 'E', true],
    ['later sibling readmodel', local + projection, '    slice StateView Other\n      readmodel E\n', 'E', true],
    ['nested feature readmodel', local + projection, '    feature Nested\n      slice StateView Other\n        readmodel E\n', 'E', true],
    ['other module readmodel', local + projection, 'module N\n  feature G\n    slice StateView Other\n      readmodel E\n', 'E', true],
    ['nonlocal event', projection, '    slice StateChange Other\n      event E\n', 'E', true],
    ['qualified target', local + projection.replace('=> E', '=> M.F.Publish.E'), '', 'E', false],
    ['duplicate local events', local + '      event E\n' + projection, '', 'E', true],
    ['duplicate nonlocal event', local + projection, '    slice StateChange Other\n      event E\n', 'E', true],
    ['missing target', projection, '', 'E', true],
    ['case only readmodel', local + '      readmodel e\n' + projection, '', 'E', true],
    ['case only event', '      public event e\n' + projection, '', 'E', true],
    ['ordinary explicit view', '      readmodel E\n' + projection, '', 'E', true],
    ['inferred view', projection, '', 'E', true],
    ['projection name fallback', '      public event P\n      projection P\n        from Input\n', '', 'P', true],
    ['event reducer', local + '      reducer P => E\n        on Input\n          value = value\n', '', 'E', true],
    ['projection variant', local + '      projection P => E\n        variant V\n          enters on Input\n', '', 'V', true]
];

describe('when preserving event target boundaries across separate documents', () => {
    const result = compileApplication(new Map([
        ['a-targets.play', prefix + local + projection + '      event Input\n'],
        ['z-readmodel.play', 'module N\n  feature G\n    slice StateView Other\n      readmodel E\n'],
        ['zz-consumer.play', 'module M\n  feature F\n    slice StateChange Consumer\n      command C\n        reads E\n']
    ]));
    const graph = DependencyGraph.for(result.value);

    it('should keep the publisher ahead of the later declared readmodel', () => {
        graph.edges.filter(edge => edge.kind === 'decidesFrom').flatMap(edge => edge.evidence).map(evidence => `${evidence.consumer.address}:${evidence.producer.address}:${evidence.role}:${evidence.name}:${evidence.ambiguous}:${evidence.alternatives.length}:${evidence.location.path}:${evidence.location.line}:${evidence.location.column}`).should.deep.equal(['M.F.Consumer:M.F.Publish:reads:E:false:0:zz-consumer.play:5:9']);
    });
    it('should not report an unresolved read', () => {
        graph.unresolved.filter(reference => reference.role === 'reads').should.be.empty;
    });
});

describe('when preserving event target boundary admission diagnostics', () => {
    for (const name of ['same slice readmodel', 'projection name fallback']) {
        const [, body, suffix] = cases.find(value => value[0] === name)!;
        const result = compileApplication(new Map([['model.play', prefix + body + '      event Input\n' + suffix]]));
        it(`should keep the outbound admission error for ${name}`, () => {
            result.success.should.be.false;
            result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0609').map(diagnostic => `${diagnostic.code}:${diagnostic.severity}:${diagnostic.location.path}:${diagnostic.location.line}:${diagnostic.location.column}:${diagnostic.message}`).should.deep.equal(["PLAY0609:error:model.play:3:5:Outbound Translate slice 'Publish' must produce exactly one local public event type; found 0."]);
        });
    }
});

describe('when preserving event target boundaries', () => {
    for (const [name, body, suffix, target, builder] of cases) {
        const graph = graphOf(prefix + body + '      event Input\n' + suffix + 'module M\n  feature F\n    slice StateChange Consumer\n      command C\n        reads ' + target);
        it(`should keep builder precedence and evidence for ${name}`, () => {
            graph.edges.filter(edge => edge.kind === 'decidesFrom').flatMap(edge => edge.evidence).map(evidence => `${evidence.consumer.address}:${evidence.producer.address}:${evidence.role}:${evidence.name}:${evidence.ambiguous}:${evidence.alternatives.length}:${evidence.location.path}`).should.deep.equal(builder ? [`M.F.Consumer:M.F.Publish:reads:${target}:false:0:model.play`] : []);
        });
        it(`should keep unresolved reads for ${name}`, () => {
            graph.unresolved.filter(reference => reference.role === 'reads').map(reference => reference.name).should.deep.equal(builder ? [] : [target]);
        });
    }
});

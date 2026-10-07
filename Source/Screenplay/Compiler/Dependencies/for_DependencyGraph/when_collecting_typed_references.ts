// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DependencyGraph } from '../DependencyGraph';
import { graphOf, producer } from './given/a_model';

const cases = [
    ['projection P\n        from E', 'usesFactsFrom', 'from'],
    ['projection P\n        join value on id\n          with E', 'usesFactsFrom', 'join'],
    ['projection P\n        remove with E', 'usesFactsFrom', 'remove'],
    ['projection P\n        clear with E', 'usesFactsFrom', 'clear'],
    ['projection P\n        remove via join on E', 'usesFactsFrom', 'removeViaJoin'],
    ['projection P\n        children items identified by id\n          from E', 'usesFactsFrom', 'from'],
    ['projection P\n        nested item\n          from E', 'usesFactsFrom', 'from'],
    ['projection P\n        variant Variant\n          enters on E\n          from E', 'usesFactsFrom', 'entersOn'],
    ['reducer Fold => View\n        on E', 'usesFactsFrom', 'reduces'],
    ['constraint Unique\n        unique event E', 'usesFactsFrom', 'uniqueEvent'],
    ['constraint Unique\n        unique id on E', 'usesFactsFrom', 'uniqueProperty'],
    ['command D\n        concurrency\n          events E', 'usesFactsFrom', 'concurrency'],
    ['command D\n        reads R', 'decidesFrom', 'reads'],
    ['reaction React\n        when E\n          reads R', 'decidesFrom', 'reads'],
    ['reaction React\n        when E\n          invokes C', 'reactsTo', 'trigger'],
    ['reaction React\n        when E\n          invokes C', 'asks', 'invokes'],
    ['screen V\n        action C', 'asks', 'action'],
    ['screen V\n        data R via query Q', 'shows', 'dataQuery'],
    ['screen V\n        action C\n          navigate to S', 'shows', 'navigate'],
    ['specification T\n        given E\n        when C\n        then E', 'verifiedWith', 'givenEvent'],
    ['specification T\n        when append E', 'verifiedWith', 'whenAppendedEvent'],
    ['specification T\n        then E', 'verifiedWith', 'thenEvent'],
    ['specification T\n        when C', 'verifiedWith', 'whenCommand'],
];
for (const [body, kind, role] of cases) {
    describe(`when collecting ${role} from ${body.split('\n')[0]}`, () => {
        let graph: DependencyGraph;
        beforeEach(() => { graph = graphOf(`${producer}  feature B\n    slice StateView Consumer\n      ${body}\n`); });
        it('should resolve the typed reference to its producer', () => {
            graph.edges.some(edge => edge.consumer.address === 'M.B.Consumer' && edge.producer.address === 'M.A.Producer' && edge.kind === kind && edge.evidence.some(item => item.role === role)).should.be.true;
        });
        it('should retain the explicit source location', () => {
            const item = graph.edges.flatMap(edge => edge.evidence).find(item => item.role === role)!;
            item.location.path!.should.equal('model.play');
            item.location.line.should.be.greaterThan(10);
        });
    });
}

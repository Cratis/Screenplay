// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DependencyGraph } from '../DependencyGraph';
import { parse } from '../../ScreenplayCompiler';
import { authoredOrderKey } from '../../Files/AuthoredOrder';
import { graphOf } from './given/a_model';

const source = 'concept Id : Uuid\ntype Details\n  id Id\npolicy Allowed\ntrigger E\nimport Outside.E\nmodule M\n  feature F\n    slice StateView Declared\n      readmodel R\n    slice StateView Built\n      event E\n      projection P => R\n        from E\n    slice StateView Variants\n      projection Variants\n        variant First\n          enters on E\n          children items identified by id\n            from E\n          nested item\n            from E\n        variant Second\n          enters on E\n    slice StateChange Decide\n      command C\n        id Id\n        details Details\n        authorize Allowed\n        reads R\n        reads First\n        reads Second\n      reaction React\n        when E\n          id Id';
describe('when resolving builders and shared foundations', () => {
    let graph: DependencyGraph;
    beforeEach(() => { graph = graphOf(source); });
    it('should prefer a builder over an earlier read model declaration', () => {
        graph.edges.find(edge => edge.kind === 'decidesFrom' && edge.evidence.some(item => item.name === 'R'))!.producer.address.should.equal('M.F.Built');
    });
    it('should register each variant rather than its enclosing projection', () => {
        graph.edges.find(edge => edge.kind === 'decidesFrom' && edge.producer.address === 'M.F.Variants')!.evidence.map(item => item.name).should.deep.equal(['First', 'Second']);
    });
    it('should prefer a local event over a trigger foundation or import', () => {
        graph.edges.find(edge => edge.kind === 'reactsTo')!.producer.address.should.equal('M.F.Built');
        graph.unusedImports.should.deep.equal(['Outside.E']);
    });
    it('should count only application types and policies as excluded shared references', () => {
        graph.excludedReferences.should.equal(4);
    });
    it('should report syntax fallback when no authored ranks are available', () => {
        graph.orderSource.should.equal('syntax');
    });
});

describe('when authored ranks reverse producer precedence', () => {
    let graph: DependencyGraph;
    beforeEach(() => {
        const application = parse('module M\n  feature F\n    slice StateChange A\n      event E\n    slice StateChange B\n      event E\n    slice StateView V\n      projection P\n        from E').value;
        graph = DependencyGraph.for(application, new Map([[authoredOrderKey(['M', 'F', 'B']), 0], [authoredOrderKey(['M', 'F', 'A']), 1], [authoredOrderKey(['M', 'F', 'V']), 2]]));
    });
    it('should pick the first authored producer and report the other alternative', () => {
        graph.edges[0].producer.address.should.equal('M.F.B');
        graph.edges[0].evidence[0].alternatives.map(node => node.address).should.deep.equal(['M.F.A']);
        graph.orderSource.should.equal('authored');
    });
});

describe('when querying an empty application', () => {
    let graph: DependencyGraph;
    beforeEach(() => { graph = DependencyGraph.for(parse('').value); });
    it('should return empty views and an unchanged application order', () => {
        graph.nodes.should.have.lengthOf(0);
        graph.edges.should.have.lengthOf(0);
        graph.implied('slice', 'slice').should.have.lengthOf(0);
        graph.siblingGroups().should.have.lengthOf(0);
        graph.traverse('Missing', 'outgoing').should.have.lengthOf(0);
        graph.suggestedOrder().should.deep.equal({ containers: [{ container: { kind: 'application', address: '', scope: [], rank: -1, key: 'application:' }, children: [], changed: false }], slices: [] });
    });
});

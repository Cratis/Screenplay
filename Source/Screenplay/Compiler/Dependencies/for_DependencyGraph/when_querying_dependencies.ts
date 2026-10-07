// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DependencyGraph } from '../DependencyGraph';
import { InvalidDependencyQuery } from '../InvalidDependencyQuery';
import { graphOf } from './given/a_model';

const source = 'module Consumer\n  feature Parent\n    slice StateView View\n      projection P\n        from E\n        clear with E\n    feature Child\n      slice StateChange Own\n        event Local\n  feature Other\n    slice StateView Second\n      projection Q\n        from E\nmodule Producer\n  feature Facts\n    slice StateChange Write\n      event E\n';
describe('when implying mixed levels and counting evidence', () => {
    let graph: DependencyGraph;
    beforeEach(() => { graph = graphOf(source); });
    it('should imply feature to module dependencies', () => {
        graph.implied('feature', 'module').map(edge => [edge.source.address, edge.target.address]).should.deep.equal([['Consumer.Parent', 'Producer'], ['Consumer.Other', 'Producer']]);
    });
    it('should count distinct slice pairs and all references', () => {
        const edge = graph.implied('module', 'module', undefined, false, 1)[0];
        [edge.sliceEdges, edge.references, edge.consumers.length, edge.producers.length, edge.evidenceCount].should.deep.equal([2, 3, 2, 1, 3]);
        edge.byKind.should.deep.equal({ usesFactsFrom: 3 });
        edge.evidence.should.have.lengthOf(1);
        edge.evidenceTruncated.should.be.true;
        edge.evidence[0].role.should.equal('from');
    });
    it('should reject invalid query inputs', () => {
        (() => graph.implied('context', 'slice')).should.throw(InvalidDependencyQuery);
        (() => graph.implied('slice', 'unknown')).should.throw(InvalidDependencyQuery);
        (() => graph.implied('slice', 'slice', ['unknown'])).should.throw(InvalidDependencyQuery);
        (() => graph.implied('slice', 'slice', undefined, false, -1)).should.throw(InvalidDependencyQuery);
        (() => graph.cycles('context')).should.throw(InvalidDependencyQuery);
        (() => graph.traverse('Consumer.Parent.View', 'unknown')).should.throw(InvalidDependencyQuery);
    });
});

describe('when a slice and sibling feature share a name', () => {
    let graph: DependencyGraph;
    beforeEach(() => { graph = graphOf('module M\n  feature F\n    slice StateView Same\n      projection P\n        from E\n    feature Same\n      slice StateChange Write\n        event E'); });
    it('should use typed ancestry rather than dotted address prefixes', () => {
        graph.implied('slice', 'feature').map(edge => [edge.source.key, edge.target.key]).should.deep.equal([['slice:M.F.Same', 'feature:M.F.Same']]);
    });
    it('should omit overlapping ancestors', () => {
        graph.implied('feature', 'feature').should.have.lengthOf(0);
        graph.implied('feature', 'module').should.have.lengthOf(0);
    });
});

describe('when finding cycles and suggesting order', () => {
    let graph: DependencyGraph;
    beforeEach(() => { graph = graphOf('module M\n  feature F\n    slice StateView A\n      event AEvent\n      projection A\n        from BEvent\n    slice StateView Unrelated\n    slice StateView B\n      event BEvent\n      projection B\n        from AEvent\n    slice StateView Consumer\n      projection C\n        from Later\n    slice StateChange Producer\n      event Later'); });
    it('should find slice and sibling groups in authored order', () => {
        graph.cycles('slice').map(group => group.members.map(node => node.address)).should.deep.equal([['M.F.A', 'M.F.B']]);
        graph.siblingGroups().map(group => [group.container!.address, group.members.map(node => node.address)]).should.deep.equal([['M.F', ['M.F.A', 'M.F.B']]]);
    });
    it('should retain cycle member order without displacing unrelated siblings', () => {
        graph.suggestedOrder().slices.map(node => node.address).should.deep.equal(['M.F.A', 'M.F.Unrelated', 'M.F.B', 'M.F.Producer', 'M.F.Consumer']);
        graph.suggestedOrder().containers.find(item => item.container.address === 'M.F')!.changed.should.be.true;
    });
    it('should traverse both directions without returning the starting node', () => {
        graph.traverse('M.F.A', 'outgoing').map(node => node.address).should.deep.equal(['M.F.B']);
        graph.traverse('M.F.Producer', 'incoming').map(node => node.address).should.deep.equal(['M.F.Consumer']);
    });
    it('should ignore non ordering kinds for cycles and order', () => {
        graph.cycles('slice', ['asks']).should.have.lengthOf(0);
        graph.suggestedOrder(['asks']).slices.map(node => node.address).should.deep.equal(['M.F.A', 'M.F.Unrelated', 'M.F.B', 'M.F.Consumer', 'M.F.Producer']);
    });
});

describe('when resolving imports, tests and shared foundations', () => {
    let graph: DependencyGraph;
    beforeEach(() => { graph = graphOf('trigger Tick\nimport Z.Recorded\nimport A.Recorded\nimport Unused.Event\nmodule M\n  feature F\n    slice StateView V\n      event Local\n      projection P\n        from Local\n        from Recorded\n        from Missing\n      reaction R\n        when Tick\n      specification T\n        given Recorded'); });
    it('should retain unresolved names without creating edges', () => {
        graph.unresolved.map(item => item.name).should.deep.equal(['Missing']);
        graph.excludedReferences.should.equal(1);
        graph.unusedImports.should.deep.equal(['Unused.Event']);
    });
    it('should resolve ambiguous imports in source location order', () => {
        const item = graph.edges[0].evidence[0];
        item.producer.address.should.equal('context:Z');
        item.ambiguous.should.be.true;
        item.alternatives.map(node => node.address).should.deep.equal(['context:A']);
    });
    it('should hide imported specification evidence by default', () => {
        graph.implied('slice', 'context')[0].references.should.equal(1);
        graph.implied('slice', 'context', undefined, true)[0].references.should.equal(2);
    });
});

describe('when names are duplicated', () => {
    let graph: DependencyGraph;
    beforeEach(() => { graph = graphOf('module M\n  feature F\n    slice StateChange P\n      event E\n    slice StateChange P\n      event Other\n    slice StateChange Second\n      event e\n    slice StateView V\n      projection P\n        from E\n        from Other'); });
    it('should merge presentation nodes and retain all declarations', () => {
        graph.nodes.filter(node => node.kind === 'slice').map(node => node.address).should.deep.equal(['M.F.P', 'M.F.Second', 'M.F.V']);
        graph.edges[0].evidence.map(item => item.name).should.deep.equal(['E', 'Other']);
        graph.suggestedOrder().slices.should.have.lengthOf(3);
    });
    it('should choose the earliest case insensitive producer and report alternatives', () => {
        graph.edges[0].evidence[0].ambiguous.should.be.true;
        graph.edges[0].evidence[0].alternatives.map(node => node.address).should.deep.equal(['M.F.Second']);
    });
});

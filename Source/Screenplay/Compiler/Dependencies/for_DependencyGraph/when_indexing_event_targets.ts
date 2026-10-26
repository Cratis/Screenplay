// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DependencyGraph } from '../DependencyGraph';
import { graphOf } from './given/a_model';

const source = 'module M\n  feature F\n    slice StateChange Input\n      event Input\n    slice Translate Publish\n      direction outbound\n      public event E\n      projection P => E\n        from Input\n    slice StateView View\n      readmodel R\n      projection View => R\n        from E\n    slice StateChange Consumer\n      command C\n        reads E\n        reads R\n';

describe('when indexing event targets', () => {
    let graph: DependencyGraph;
    beforeEach(() => { graph = graphOf(source); });
    it('should not offer an event publisher as a readmodel builder', () => {
        graph.edges.filter(edge => edge.kind === 'decidesFrom').map(edge => edge.producer.address).should.deep.equal(['M.F.View']);
    });
    it('should retain the unresolved read and its original source location', () => {
        graph.unresolved.map(reference => `${reference.consumer.address}:${reference.kind}:${reference.role}:${reference.name}:${reference.location.path}:${reference.location.line}:${reference.location.column}`).should.deep.equal(['M.F.Consumer:decidesFrom:reads:E:model.play:16:9']);
    });
    it('should keep the genuine input event dependency and evidence', () => {
        graph.edges.filter(edge => edge.kind === 'usesFactsFrom' && edge.consumer.address === 'M.F.Publish').flatMap(edge => edge.evidence).map(evidence => `${evidence.producer.address}:${evidence.role}:${evidence.name}:${evidence.ambiguous}:${evidence.alternatives.length}:${evidence.location.line}`).should.deep.equal(['M.F.Input:from:Input:false:0:9']);
    });
    it('should keep the real view builder evidence', () => {
        graph.edges.filter(edge => edge.kind === 'decidesFrom').flatMap(edge => edge.evidence).filter(evidence => evidence.name === 'R').map(evidence => `${evidence.producer.address}:${evidence.role}:${evidence.ambiguous}:${evidence.alternatives.length}:${evidence.location.line}`).should.deep.equal(['M.F.View:reads:false:0:17']);
    });
});

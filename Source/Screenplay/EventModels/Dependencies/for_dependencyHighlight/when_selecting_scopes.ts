// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DependencyGraph } from '@cratis/screenplay-compiler';
import { dependencyHighlight } from '../dependencyHighlight';
import { sampleApplication } from '../for_DependencyMap/given/a_sample';

const graph = DependencyGraph.for(sampleApplication('TimeTracking'));

describe('when selecting a containing module', () => {
    const highlight = dependencyHighlight(graph, 'Payroll');
    it('should select the module and all of its descendants', () => highlight.selected.should.deep.equal(graph.nodes.filter(node => node.scope[0] === 'Payroll').map(node => node.key)));
    it('should find dependencies across modules', () => highlight.dependencies.some(key => key.startsWith('slice:Timesheets.')).should.be.true);
    it('should not count selected slices as dependencies', () => highlight.dependencies.some(key => highlight.selected.includes(key)).should.be.false);
});

describe('when selecting a feature', () => {
    const feature = graph.nodes.find(node => node.kind === 'feature')!;
    it('should select its nested scope', () => dependencyHighlight(graph, feature.address).selected.should.include(feature.key));
});

describe('when selecting verification dependencies', () => {
    it('should allow test-only dependencies explicitly', () => dependencyHighlight(graph, 'Payroll', ['verifiedWith']).dependencies.length.should.be.greaterThan(0));
});

describe('when selecting an unknown address', () => {
    it('should leave no highlights', () => dependencyHighlight(graph, 'Missing').should.deep.equal({ selected: [], dependencies: [], dependants: [], edges: [] }));
});

describe('when selecting an external consumer', () => {
    const imported = DependencyGraph.for(sampleApplication('Invoicing'));
    const consumer = imported.edges.find(edge => edge.kind === 'outsideTheModel' && edge.evidence.some(item => !item.testOnly))!.consumer;
    const highlight = dependencyHighlight(imported, consumer.address, ['outsideTheModel']);
    it('should include and mark external dependencies', () => highlight.edges.some(edge => edge.crossing && edge.target.startsWith('context:')).should.be.true);
});

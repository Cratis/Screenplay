// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DependencyGraph } from '../DependencyGraph';
import { sampleGraph } from './given/a_sample';

// The same business expectations as the C# sample holders; no executable model admission is required.
describe('when reading Commerce dependencies', () => {
    let graph: DependencyGraph;
    beforeEach(() => { graph = sampleGraph('Commerce'); });
    it('should count the dependencies in both directions between the modules', () => {
        graph.implied('module', 'module').filter(edge => edge.source.address === 'Ordering' || edge.source.address === 'Fulfillment').map(edge => [edge.source.address, edge.target.address, edge.sliceEdges]).should.deep.equal([['Ordering', 'Fulfillment', 4], ['Fulfillment', 'Ordering', 1]]);
    });
    it('should find the module and feature cycles', () => {
        graph.cycles('module').map(group => group.members.map(node => node.address)).should.deep.equal([['Ordering', 'Fulfillment']]);
        graph.cycles('feature').map(group => group.members.map(node => node.address)).should.deep.equal([['Fulfillment.Shipping', 'Fulfillment.Tracking']]);
    });
    it('should suggest Payments before Orders and Support', () => {
        graph.suggestedOrder().containers.find(item => item.container.address === 'Ordering')!.children.map(node => node.address).should.deep.equal(['Ordering.Payments', 'Ordering.Orders', 'Ordering.Support']);
    });
});

describe('when reading TimeTracking dependencies', () => {
    let graph: DependencyGraph;
    beforeEach(() => { graph = sampleGraph('TimeTracking'); });
    it('should infer the module edges without introducing an ordering cycle', () => {
        graph.implied('module', 'module').map(edge => `${edge.source.address}|${edge.target.address}`).should.deep.equal(['Engagements|Timesheets', 'Payroll|Timesheets', 'Timesheets|Engagements']);
        graph.cycles('module').should.have.lengthOf(0);
    });
    it('should suggest Timesheets before Payroll', () => {
        graph.suggestedOrder().containers[0].children.map(node => node.address).should.deep.equal(['Engagements', 'Timesheets', 'Payroll']);
    });
});

describe('when reading Library dependencies', () => {
    let graph: DependencyGraph;
    beforeEach(() => { graph = sampleGraph('Library'); });
    it('should find the Catalog and Loans cycle without suggesting a reorder', () => {
        graph.cycles('feature').map(group => group.members.map(node => node.address)).should.deep.equal([['Lending.Catalog', 'Lending.Loans']]);
        graph.suggestedOrder().containers.some(item => item.changed).should.be.false;
    });
});

describe('when reading Invoicing dependencies', () => {
    let graph: DependencyGraph;
    beforeEach(() => { graph = sampleGraph('Invoicing'); });
    it('should resolve every consumed context and mark every import used', () => {
        [...new Set(graph.implied('slice', 'context').map(edge => edge.target.address))].sort().should.deep.equal(['context:Customers', 'context:Payments', 'context:Shipping']);
        graph.unusedImports.should.have.lengthOf(0);
    });
    it('should not imply a parent feature dependency on its own subfeature', () => {
        graph.implied('feature', 'feature').some(edge => edge.source.address === 'Invoicing.InvoiceManagement' && edge.target.address === 'Invoicing.InvoiceManagement.Adjustments').should.be.false;
    });
});

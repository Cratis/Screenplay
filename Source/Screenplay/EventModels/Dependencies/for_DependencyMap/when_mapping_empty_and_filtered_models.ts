// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DependencyGraph, parse } from '@cratis/screenplay-compiler';
import { dependencyMapFor } from '../dependencyMapFor';
import { boardIdOf } from '../boardIdOf';
import { layoutDependencyMap } from '../layoutDependencyMap';
import { sampleApplication } from './given/a_sample';

describe('when mapping an empty model', () => {
    const map = dependencyMapFor(parse('').value);
    it('should keep an empty map usable', () => map.nodes.should.deep.equal([]));
    it('should reserve a minimum drawing area', () => layoutDependencyMap(map).should.deep.equal({ width: 320, height: 240, nodes: [], edges: [] }));
    it('should give an application no board scope id', () => (boardIdOf({ kind: 'application', address: '', key: 'application:', scope: [], rank: -1 }) === undefined).should.be.true);
});

describe('when filtering a dependency map', () => {
    const application = sampleApplication('TimeTracking');
    const map = dependencyMapFor(application, ['usesFactsFrom', 'reactsTo', 'decidesFrom']);
    it('should omit non-ordering edges without changing suggested module order', () => map.edges.some(edge => edge.source === 'module:Engagements' && edge.target === 'module:Timesheets').should.be.false);
    it('should preserve every module', () => map.modules.should.deep.equal(['module:Engagements', 'module:Timesheets', 'module:Payroll']));
    it('should not assign board identities to contexts', () => DependencyGraph.for(sampleApplication('Invoicing')).nodes.filter(node => node.kind === 'context').every(node => boardIdOf(node) === undefined).should.be.true);
});

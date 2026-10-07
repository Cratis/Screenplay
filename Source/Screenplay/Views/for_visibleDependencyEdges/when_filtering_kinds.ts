// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { dependencyKinds, parse } from '@cratis/screenplay-compiler';
import { dependencyMapFor } from '@cratis/screenplay-event-models';
import { DependencyMapLevel } from '../DependencyMapLevel';
import { visibleDependencyEdges } from '../visibleDependencyEdges';

const map = dependencyMapFor(parse(`import Customers.CustomerRegistered
module Facts
  feature Record
    slice StateChange RecordFact
      event FactRecorded
      query AllFacts
        returns Facts
module Views
  feature Display
    slice StateView ShowFacts
      readmodel Facts
      projection Facts
        from FactRecorded
        from CustomerRegistered
      screen FactsScreen
        uses AllFacts
      specification WithFacts
        given FactRecorded
        when query AllFacts
        then result
`).value);

describe('when filtering dependency kinds', () => {
    const defaultEdges = visibleDependencyEdges(map, DependencyMapLevel.Module, ['usesFactsFrom', 'reactsTo', 'decidesFrom']);
    it('should start with only story-ordering kinds', () => defaultEdges.every(edge => edge.evidence.every(id => map.evidence[id].kind === 'usesFactsFrom')).should.be.true);
    it('should show imported contexts only when enabled', () => visibleDependencyEdges(map, DependencyMapLevel.Module, ['outsideTheModel']).some(edge => edge.target === 'context:context:Customers').should.be.true);
    it('should hide verified-with evidence unless explicitly enabled', () => defaultEdges.flatMap(edge => edge.evidence).some(id => map.evidence[id].testOnly).should.be.false);
    it('should allow asks and verification to be enabled', () => visibleDependencyEdges(map, DependencyMapLevel.Module, dependencyKinds).flatMap(edge => edge.evidence).some(id => map.evidence[id].kind === 'verifiedWith').should.be.true);
    it('should recount references after filtering', () => defaultEdges.every(edge => edge.references === edge.evidence.length && Object.values(edge.byKind).reduce((sum, count) => sum + count, 0) === edge.references).should.be.true);
    it('should group multiple references into one slice pair', () => visibleDependencyEdges(map, DependencyMapLevel.Module, dependencyKinds).find(edge => edge.target === 'module:Facts')!.sliceEdges.should.equal(1));
    it('should select feature-level edges separately', () => visibleDependencyEdges(map, DependencyMapLevel.Feature, dependencyKinds).every(edge => edge.source.startsWith('feature:')).should.be.true);
    it('should omit every edge when no kind is enabled', () => visibleDependencyEdges(map, DependencyMapLevel.Module, []).should.deep.equal([]));
});

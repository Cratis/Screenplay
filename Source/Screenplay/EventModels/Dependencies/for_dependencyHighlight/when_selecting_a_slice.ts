// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DependencyGraph, parse } from '@cratis/screenplay-compiler';
import { dependencyHighlight } from '../dependencyHighlight';

const graph = DependencyGraph.for(parse(`module Producers
  feature Facts
    slice StateChange First
      event FirstFact
module Consumers
  feature Views
    slice StateView Second
      readmodel SecondView
      projection SecondView
        from FirstFact
    slice StateView Third
      command Decide
        reads SecondView
`).value);

describe('when selecting a slice', () => {
    const highlight = dependencyHighlight(graph, 'Consumers.Views.Second');
    it('should find its dependencies', () => highlight.dependencies.should.deep.equal(['slice:Producers.Facts.First']));
    it('should find its dependants', () => highlight.dependants.should.deep.equal(['slice:Consumers.Views.Third']));
    it('should distinguish crossing edges', () => highlight.edges.map(edge => edge.crossing).should.deep.equal([true, false]));
});

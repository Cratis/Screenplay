// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { dependencyMapFor } from '../dependencyMapFor';
import { layoutDependencyMap } from '../layoutDependencyMap';
import { sampleApplication } from '../for_DependencyMap/given/a_sample';

const reciprocal = dependencyMapFor(parse(`module Work
  feature First
    slice StateView FirstView
      event FirstRecorded
      readmodel First
      projection First
        from SecondRecorded
  feature Second
    slice StateView SecondView
      event SecondRecorded
      readmodel Second
      projection Second
        from FirstRecorded
`).value);

describe('when routing reciprocal feature dependencies', () => {
    const layout = layoutDependencyMap(reciprocal);
    it('should give each directed edge a separate path', () => new Set(layout.edges.map(edge => edge.path)).size.should.equal(2));
    it('should give each directed edge a separate label position', () => new Set(layout.edges.map(edge => `${edge.labelX},${edge.labelY}`)).size.should.equal(2));
    it('should route deterministically regardless of edge enumeration', () => layoutDependencyMap({ ...reciprocal, edges: [...reciprocal.edges].reverse() }).edges.should.deep.equal(layout.edges));
});

describe('when routing TimeTracking feature dependencies', () => {
    const map = dependencyMapFor(sampleApplication('TimeTracking'));
    const layout = layoutDependencyMap({ ...map, edges: map.edges.filter(edge => edge.source.startsWith('feature:')) });
    it('should keep every routed label within the drawing', () => layout.edges.every(edge => edge.labelX - edge.labelWidth / 2 >= 0 && edge.labelX + edge.labelWidth / 2 <= layout.width && edge.labelY >= 0 && edge.labelY <= layout.height).should.be.true);
});

describe('when routing many disjoint column gaps', () => {
    const source = Array.from({ length: 24 }, (_, index) => `module Module${index}
  feature Feature
    slice StateView View
      event Fact${index}
      readmodel Facts
      projection Facts
        from Fact${index % 2 === 0 ? index + 1 : index - 1}
`).join('');
    const map = dependencyMapFor(parse(source).value);
    const featureEdges = map.edges.filter(edge => edge.source.startsWith('feature:'));
    const layout = layoutDependencyMap({ ...map, edges: featureEdges });
    const singlePair = layoutDependencyMap({ ...map, edges: featureEdges.slice(0, 2) });
    it('should reuse lanes across disjoint column ranges', () => layout.nodes[0].y.should.equal(singlePair.nodes[0].y));
    it('should reserve rows for the maximum overlap rather than the edge count', () => layout.nodes[0].y.should.equal(96 + 2 * 32));
});

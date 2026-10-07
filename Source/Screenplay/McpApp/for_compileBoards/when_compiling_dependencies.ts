// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { compileBoards } from '../compileBoards';

const source = `module Facts
  feature Record
    slice StateChange RecordFact
      event FactRecorded
module Views
  feature Display
    slice StateView ShowFacts
      readmodel Facts
      projection Facts
        from FactRecorded
`;

describe('when compiling dependencies for current and proposed boards', () => {
    const boards = compileBoards({ application: 'Facts', documents: [{ path: 'facts.play', source }], changes: [{ path: 'facts.play', source: source.replaceAll('Views', 'ProposedViews') }] });
    it('should include the current dependency map', () => boards.current.dependencies.edges.some(edge => edge.source === 'module:Views' && edge.target === 'module:Facts').should.be.true);
    it('should follow the proposed model in its map', () => boards.proposed!.dependencies.edges.some(edge => edge.source === 'module:ProposedViews' && edge.target === 'module:Facts').should.be.true);
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { boardFor } from '../compileBoard';
import { narrowTo } from '../boardScope';

const compilation = parse(`module Facts
  feature Record
    slice StateChange RecordFact
      event FactRecorded
module Views
  feature Display
    slice StateView ShowFacts
      readmodel Facts
      projection Facts
        from FactRecorded
`);

describe('when including dependencies beside a narrowed board', () => {
    const narrowed = narrowTo(compilation.value, { files: new Set(), containers: [['Views']] })!;
    const board = boardFor({ ...compilation, value: narrowed }, 'Views', compilation.value);
    it('should keep cross-module dependencies from the whole application', () => board.dependencies.edges.some(edge => edge.source === 'module:Views' && edge.target === 'module:Facts').should.be.true);
    it('should keep the board narrowed', () => (board.document as { collections: { modules: { name: string }[] }[] }).collections[0].modules.map(module => module.name).should.deep.equal(['Views']));
});

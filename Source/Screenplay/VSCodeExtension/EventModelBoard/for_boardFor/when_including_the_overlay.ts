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

describe('when including the overlay beside a narrowed board', () => {
    const narrowed = narrowTo(compilation.value, { files: new Set(), containers: [['Views']] })!;
    const board = boardFor({ ...compilation, value: narrowed }, 'Views', compilation.value);
    const views = board.overlay!.scopes.find(scope => scope.kind === 'module' && scope.dependencies.length > 0)!;
    it('should keep the dependencies from the whole application', () => (views !== undefined).should.be.true);
    it('should keep a crossing link to a module that is not drawn', () => views.links.some(link => link.crossing).should.be.true);
    it('should survive the message channel', () => JSON.parse(JSON.stringify(board.overlay)).should.deep.equal(board.overlay));
});

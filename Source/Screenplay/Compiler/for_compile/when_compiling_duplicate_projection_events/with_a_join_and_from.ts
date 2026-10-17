// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseProjectionSource, CompilationResult } from '../../ScreenplayCompiler';
import { ProjectionSyntax } from '../../Syntax/Projections';

describe('when compiling duplicate projection events across a join and from', () => {
    let result: CompilationResult<readonly ProjectionSyntax[]>;
    beforeEach(() => { result = parseProjectionSource('projection Links\n  from LinkRevised\n  join link on linkId\n    with LinkRevised'); });

    it('should keep separate event and join sets', () => { result.diagnostics.should.be.empty; });
    it('should succeed', () => { result.success.should.be.true; });
});

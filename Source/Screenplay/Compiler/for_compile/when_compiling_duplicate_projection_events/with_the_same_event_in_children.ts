// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseProjectionSource, CompilationResult } from '../../ScreenplayCompiler';
import { ProjectionSyntax } from '../../Syntax/Projections';

describe('when compiling duplicate projection events across children blocks', () => {
    let result: CompilationResult<readonly ProjectionSyntax[]>;
    beforeEach(() => { result = parseProjectionSource('projection Links\n  from LinkRevised\n  children first identified by id\n    from LinkRevised\n  children second identified by id\n    from LinkRevised'); });

    it('should start fresh sets per children block', () => { result.diagnostics.should.be.empty; });
    it('should succeed', () => { result.success.should.be.true; });
});

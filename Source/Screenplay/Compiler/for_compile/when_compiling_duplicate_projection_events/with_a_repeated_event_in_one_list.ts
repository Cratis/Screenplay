// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseProjectionSource, CompilationResult } from '../../ScreenplayCompiler';
import { ProjectionSyntax } from '../../Syntax/Projections';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';

describe('when compiling duplicate projection events in one list', () => {
    let result: CompilationResult<readonly ProjectionSyntax[]>;
    beforeEach(() => { result = parseProjectionSource('projection Links\n  from LinkRevised, LinkRevised, LinkRevised'); });

    it('should report second and later references', () => {
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([DiagnosticCodes.DuplicateEventInProjectionScope, DiagnosticCodes.DuplicateEventInProjectionScope]);
    });
    it('should locate the list', () => {
        result.diagnostics.every(diagnostic => diagnostic.location.line === 2 && diagnostic.location.column === 3).should.be.true;
    });
});

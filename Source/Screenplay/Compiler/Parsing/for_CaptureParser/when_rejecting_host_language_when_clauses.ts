// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parseCaptureSource } from '../../ScreenplayCompiler';

const warnings = (when: string) => parseCaptureSource(`capture C\n  map\n    a = \$.a\n  append Added\n    when ${when}\n      a = \$.a\n`).diagnostics.filter(_ => _.code === DiagnosticCodes.HostLanguageExpression);

describe('when rejecting host language when clauses', () => {
    it('should warn once for a property clause', () => {
        warnings('a && b').map(_ => _.severity).should.deep.equal(['warning']);
    });

    it.each(['`a && b`', '`f(a) || b`', 'a or b', 'a'])('should not warn for %s', when => {
        warnings(when).should.deep.equal([]);
    });
});

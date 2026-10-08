// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import vectors from '../../../Compiler/Conformance/diagnostics.json';
import { validateLines } from '../validation';

const guardedCode = (code: string | undefined) => /^PLAY034[5-8]$/.test(code ?? '');
const cases = vectors.cases.filter(vector => vector.diagnostics.some(diagnostic => guardedCode(diagnostic.split('@')[0])));

describe('when forwarding guarded action compiler diagnostics to Monaco markers', () => {
    it('should exercise all four codes', () => {
        new Set(cases.flatMap(vector => vector.diagnostics.map(diagnostic => diagnostic.split('@')[0])).filter(guardedCode)).size.should.equal(4);
    });

    it.each(cases)('should preserve code severity message and location for $name', vector => {
        const diagnostics = parse(vector.source.join('\n')).diagnostics.filter(diagnostic => guardedCode(diagnostic.code));
        const expected = diagnostics.map(diagnostic => [diagnostic.code, diagnostic.severity, diagnostic.message, diagnostic.location.line - 1, diagnostic.location.column]);
        for (const context of [undefined, { compilerDiagnostics: diagnostics }]) {
            validateLines(vector.source, context).filter(marker => guardedCode(marker.code))
                .map(marker => [marker.code, marker.severity, marker.message, marker.line, marker.startColumn]).should.deep.equal(expected);
        }
    });
});

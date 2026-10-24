// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes, parse } from '@cratis/screenplay-compiler';
import { validateLines } from '../validation';

const invalid = ['module M', '  feature F', '    slice Translate Receive', '      capture Feed', '        source webhook', '          path /feed', '        key id is event source'];
const code = DiagnosticCodes.CaptureEventSourceKeyOutsideEvents;

describe('when forwarding event source key diagnostics', () => {
    it('should forward the real invalid webhook capture diagnostic', () => {
        const diagnostic = parse(invalid.join('\n')).diagnostics.find(diagnostic => diagnostic.code === code)!;
        validateLines(invalid).filter(issue => issue.code === code).should.deep.equal([{
            code,
            severity: diagnostic.severity,
            message: diagnostic.message,
            line: diagnostic.location.line - 1,
            startColumn: diagnostic.location.column,
            endColumn: invalid[diagnostic.location.line - 1].length + 1,
        }]);
    });
    it('should preserve a diagnostic supplied by the compiler host', () => {
        const supplied = { code, severity: 'error' as const, message: 'Only a source events capture with a single property key can declare is event source', location: { line: 4, column: 7 } };
        validateLines(invalid, { compilerDiagnostics: [supplied] }).filter(issue => issue.code === code).should.deep.equal([{
            code, severity: supplied.severity, message: supplied.message, line: 3, startColumn: 7, endColumn: invalid[3].length + 1,
        }]);
    });
    it('should not diagnose an unmarked webhook capture', () => {
        validateLines(invalid.map(line => line.replace(' is event source', ''))).filter(issue => issue.code === code).should.deep.equal([]);
    });
    it('should not diagnose a marked events capture', () => {
        const valid = ['module M', '  feature F', '    slice Translate Receive', '      direction inbound', '      event Arrived from "carrier"', '        id String', '      capture Feed', '        source events', '          from Arrived', '        key id is event source'];
        validateLines(valid).filter(issue => issue.code === code).should.deep.equal([]);
    });
});

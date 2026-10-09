// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const scalar = 'eventsource A\n  stream S\n    streamId Personal';
const composite = 'eventsource A\n  stream S\n    streamId\n      owner Personal\n      period String';
const route = 'concept Public : String\ntype Input\n  personal Personal\neventsource A\n  stream S\n    streamId Public\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        input Input\n        stream A.S\n          streamId = input.personal';
const compositeRoute = 'concept Public : String\ntype Input\n  personal Personal\neventsource A\n  stream S\n    streamId\n      owner Public\n      period String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        input Input\n        stream A.S\n          streamId\n            owner = input.personal\n            period = "public"';
const protectedDiagnostics = (source: string) => parse(source).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0515');

describe('when checking protected stream ids', () => {
    for (const attribute of ['pii', 'secret']) {
        it.each([
            [scalar, 4, 14, 'a stream id'],
            [composite, 5, 13, "a stream id part 'owner'"],
            [route, 14, 22, 'a stream id route mapping'],
            [compositeRoute, 17, 21, 'a stream id route mapping'],
        ] as const)(`should reject ${attribute} at %s`, (source, line, column, position) => {
            const diagnostics = protectedDiagnostics(`concept Personal : String ${attribute}\n${source}`);
            expect(diagnostics).toHaveLength(1);
            expect(diagnostics[0]).toMatchObject({ severity: 'error', location: { line, column }, message: `Concept 'Personal' is ${attribute} and cannot be ${position} - use a surrogate Uuid identifier and keep the ${attribute} value as a property` });
            expect(diagnostics[0].message).not.toContain('input.personal');
        });
    }
    it.each([false, true])('should not repeat the declaration error for its own concept %s', composite => {
        const source = (composite ? compositeRoute : route).replace('owner Public', 'owner Personal').replace('streamId Public', 'streamId Personal');
        const result = parse('concept Personal : String pii\n' + source);
        expect(result.diagnostics).toHaveLength(1);
        expect(result.diagnostics[0]).toMatchObject({ code: 'PLAY0515', location: { line: composite ? 8 : 7 } });
    });
    it.each([false, true])('should still report a different protected source concept %s', composite => {
        const source = (composite ? compositeRoute : route).replace('owner Public', 'owner Other').replace('streamId Public', 'streamId Other');
        const diagnostics = protectedDiagnostics('concept Personal : String pii\nconcept Other : String secret\n' + source);
        expect(diagnostics).toHaveLength(2);
        expect(diagnostics[0].message).toContain('secret');
        expect(diagnostics[1].message).toContain('pii');
        expect(diagnostics[1].message).toContain('route mapping');
    });
    it('should preserve the existing precedence for both classifications', () => {
        const diagnostics = protectedDiagnostics('concept Personal : String secret pii\n' + scalar);
        expect(diagnostics).toHaveLength(1);
        expect(diagnostics[0].message).toContain('pii');
    });
    it('should leave ordinary protected properties and literal routes valid', () => {
        expect(parse('concept Personal : String pii\n' + route.replace('input.personal', '"public"')).diagnostics).toEqual([]);
    });
});

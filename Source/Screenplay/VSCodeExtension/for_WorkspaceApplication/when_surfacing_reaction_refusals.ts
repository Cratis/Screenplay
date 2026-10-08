// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import vectors from '../../Compiler/Conformance/diagnostics.json';
import { WorkspaceApplication } from '../WorkspaceApplication';

const refusalCode = (code: string) => /^PLAY053[89]$|^PLAY054[0-5]$/.test(code);
const cases = vectors.cases.filter(vector => vector.diagnostics.some(diagnostic => refusalCode(diagnostic.split('@')[0])));

describe('when surfacing reaction refusal diagnostics in workspace files', () => {
    it('should cover every refusal and redelivery code', () => {
        expect(new Set(cases.flatMap(vector => vector.diagnostics.map(diagnostic => diagnostic.split('@')[0])).filter(refusalCode)).size).toBe(8);
    });

    it.each(cases)('should preserve code severity message and location for $name', vector => {
        const source = vector.source.join('\n');
        const workspace = new WorkspaceApplication();
        workspace.set('application.play', source);
        const actual = workspace.diagnosticsFor('application.play').filter(diagnostic => refusalCode(diagnostic.code));
        expect(actual).toEqual(parse(source, 'application.play').diagnostics.filter(diagnostic => refusalCode(diagnostic.code)));
        expect(actual.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`)).toEqual(vector.diagnostics.filter(diagnostic => refusalCode(diagnostic.split('@')[0])));
    });

    it('should resolve branch declarations across imported workspace files', () => {
        const workspace = new WorkspaceApplication();
        workspace.set('application.play', 'module M\n  feature F\n    import "claims.play"\n    import "recovery.play"');
        workspace.set('claims.play', 'slice StateChange Claims\n  event Approved\n  event Claimed\n  command Claim\n    produces Claimed\n  constraint OneClaim\n    unique event Claimed');
        workspace.set('recovery.play', 'slice Automation Recovery\n  reaction Claimer\n    when Approved\n      invokes Claim\n        on refused by constraint Claims.OneClaim\n          acknowledge\n        on refused by constraint Claims.OneClaim\n          acknowledge');
        expect(workspace.diagnosticsFor('recovery.play').filter(diagnostic => refusalCode(diagnostic.code))).toMatchObject([
            { code: 'PLAY0540', severity: 'warning', location: { path: 'recovery.play', line: 7, column: 9 } },
        ]);
        expect(workspace.diagnosticsFor('claims.play').filter(diagnostic => refusalCode(diagnostic.code))).toEqual([]);
    });
});

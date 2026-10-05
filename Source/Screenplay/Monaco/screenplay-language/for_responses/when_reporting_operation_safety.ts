// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { responseAnalysis } from '../response-analysis';
import { mergeSymbols, scanDocument } from '../symbols';
import { validateLines } from '../validation';

const prefix = 'system Mailer\nmodule M\n  feature F\n    slice Automation S\n      event Recorded\n      operation Send\n        uses Mailer\n      reaction R\n        every 1 day\n';

describe('when reporting operation safety', () => {
    it.each(['          produces Send\n', '          produces operation InlineSend\n            uses Mailer\n'])('should report command-only operation intent without event repair advice for %s', production => {
        const lines = (prefix + production).trimEnd().split('\n');
        const analysis = responseAnalysis(lines);
        expect(analysis.operationProductionLines?.has(lines.findIndex(line => line.trim().startsWith('produces')))).toBe(true);
        const issues = validateLines(lines);
        expect(issues.map(issue => issue.code)).toContain('PLAY0499');
        expect(issues.filter(issue => ['PLAY0166', 'PLAY0470', 'PLAY0478'].includes(issue.code ?? ''))).toEqual([]);
    });
    it('should resolve imported reaction targets and suppress wrong event advice', () => {
        const lines = ['slice Automation Here', '  reaction R', '    every 1 day', '      produces Other.Send'];
        const other = { path: 'operation.play', source: 'system Mailer\nslice StateChange Other\n  operation Send\n    uses Mailer', placement: ['M', 'F'] };
        const application = mergeSymbols(scanDocument(other.source.split('\n')));
        const issues = validateLines(lines, { path: 'reaction.play', placement: ['M', 'F'], application: { ...application, authoringDocuments: [other] } });
        expect(issues.map(issue => issue.code)).toContain('PLAY0499');
        expect(issues.filter(issue => issue.code === 'PLAY0166')).toEqual([]);
    });
    it('should retain normal unknown event advice for reaction event productions', () => {
        const issues = validateLines((prefix + '          produces Missing\n').trimEnd().split('\n'));
        expect(issues.map(issue => issue.code)).toContain('PLAY0166');
        expect(issues.map(issue => issue.code)).not.toContain('PLAY0499');
    });
});

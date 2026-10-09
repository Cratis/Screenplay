// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { exampleCompletions } from '../example-authoring';
import { hoverContent } from '../hover-content';
import { responseAnalysis } from '../response-analysis';

const lines = ['policy Member', '  require role "A" or role "B"', 'persona Person', '  policy Member', 'specification X', '  given caller as Person'];
const hover = (source: string[]) => hoverContent(source, 5, 'Person', 19, 25);

describe('when authoring persona callers', () => {
    it('shows the selected minimal witness and contributions', () => {
        expect(hover(lines)).toContain('authenticated');
        expect(hover(lines)).toContain('Roles: "A"');
        expect(hover(lines)).not.toContain('Roles: "A", "B"');
        expect(hover(lines)).toContain('Member: role "A"');
    });
    it('keeps Unicode persona names in hover and completion', () => {
        const source = lines.map(line => line.replace('Person', 'Pärson'));
        expect(hoverContent(source, 5, 'Pärson', 19, 25)).toContain('Roles: "A"');
        expect(exampleCompletions(source, 5, '  given caller as Pär')?.map(entry => entry.label)).toEqual(['Pärson']);
    });
    it('explains compile-time synthesis refusals', () => {
        expect(hover(lines.map(line => line.replace('role "A" or role "B"', 'not role "A"')))).toContain('negation in policy Member');
        expect(hover(lines.map(line => line.replace('role "A" or role "B"', 'claim "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" matches "A"')))).toContain('roleClaim');
    });
    it('completes top-level personas in the caller slot', () => {
        const current = lines.map((line, index) => index === 5 ? '  given caller as ' : line);
        expect(exampleCompletions(current, 5, current[5])?.map(entry => entry.label)).toEqual(['Person']);
    });
    it('does not offer persona callers inside comments', () => expect(exampleCompletions(lines, 5, '  // given caller as ')).toBeNull());
    it('reports unknown persona references with the compiler code', () => expect(responseAnalysis(lines.map(line => line.replace('as Person', 'as Other'))).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0565'));
});

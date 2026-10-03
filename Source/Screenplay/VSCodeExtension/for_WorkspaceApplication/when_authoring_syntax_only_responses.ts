// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { responseTokens, validateLines } from '@cratis/screenplay-language';
import { WorkspaceApplication } from '../WorkspaceApplication';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const patterns = grammar.repository.keywords.patterns as { match: string; comment?: string }[];

describe('when authoring syntax-only responses across files', () => {
    it('should surface compiler diagnostics from imported and unsaved declarations without duplicate rules', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', 'import "types.play"\nmodule M\n  feature F\n    import "slice.play"');
        application.set('types.play', 'concept Id : Uuid');
        const source = 'slice StateChange S\n  command C\n    id Id generated\n    returns @id';
        application.set('slice.play', source);
        expect(application.diagnosticsFor('slice.play')).toEqual([]);
        application.set('types.play', 'concept Id : String');
        const diagnostics = application.diagnosticsFor('slice.play');
        expect(diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0483');
        const issues = validateLines(source.split('\n'), {
            application: application.symbolsExcept('slice.play'),
            placement: application.placementOf('slice.play'),
            compilerDiagnostics: diagnostics,
        });
        expect(issues.filter(issue => issue.code === 'PLAY0483')).toHaveLength(1);
        expect(issues.some(issue => issue.message.includes('execution unavailable'))).toBe(true);
    });
    it('should keep TextMate contextual words and use typed overlays for scalar ambiguity', () => {
        const generated = patterns.find(pattern => pattern.comment?.startsWith('Generated is'))!;
        const response = patterns.find(pattern => pattern.comment?.startsWith('Unambiguous response'))!;
        expect(new RegExp(generated.match).test('  id Id generated identifier')).toBe(true);
        expect(new RegExp(generated.match).test('  generated String')).toBe(false);
        expect(new RegExp(response.match).test('  returns @id')).toBe(true);
        expect(new RegExp(response.match).test('  returns String')).toBe(false);
        expect(responseTokens(['command C', '  returns lower', '  lower String']).some(token => token.line === 1 && token.type === 0)).toBe(true);
    });
});

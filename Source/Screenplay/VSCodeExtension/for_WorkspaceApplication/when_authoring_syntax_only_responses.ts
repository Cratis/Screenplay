// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { hoverContent, mergeSymbols, responseAnalysis, responseCompletions, responseTokens, scanDocument, validateLines } from '@cratis/screenplay-language';
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
    it.each([0, 1])('should complete only current unsaved sources despite foreign command offset %s', offset => {
        const application = new WorkspaceApplication();
        application.set('current.play', 'module Old');
        application.set('other.play', '\n'.repeat(offset) + 'command Other\n  foreign String');
        const lines = ['command Current', '  mine String', '  returns '];
        const symbols = mergeSymbols(scanDocument(lines), application.symbolsExcept('current.play'));
        expect(responseCompletions(lines, 2, lines[2], symbols)?.map(entry => entry.insertText)).toEqual(['@mine']);
    });
    it('should preserve imported fragment placement for authoring analysis and information markers', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', 'import "types.play"\nmodule M\n  feature F\n    import "slice.play"');
        application.set('types.play', 'concept Id : Uuid');
        application.set('slice.play', 'slice StateChange S\n  command C\n    id Id generated identifier\n    returns @missing');
        application.set('other.play', 'module Other\n  feature F\n    slice StateChange Other\n      command C\n        foreign String\n        returns @foreign');
        const lines = ['slice StateChange S', '  command C', '    id Id generated identifier', '    returns @missing'];
        const context = { application: application.symbolsExcept('slice.play'), placement: application.placementOf('slice.play'), path: 'slice.play' };
        const analysis = responseAnalysis(lines, context.application.authoringDocuments, context.placement, context.path);
        expect([...analysis.commands.values()].map(command => command.name)).toEqual(['C']);
        const issues = validateLines(lines, context);
        expect(issues.filter(issue => issue.code === 'PLAY0487')).toHaveLength(1);
        expect(issues.filter(issue => issue.code === 'PLAY0483')).toHaveLength(0);
        expect(issues.filter(issue => issue.code === 'PLAY0268')).toHaveLength(2);
        expect(validateLines(lines.map(line => line.replace('@missing', '@id')), context).filter(issue => issue.severity !== 'information')).toEqual([]);
    });
    it('should share precise hover tokens and full explicit type rendering', () => {
        const lines = ['command C', '  values String[] optional', '  returns', '    result String[] optional = values // result'];
        expect(hoverContent(lines, 3, 'result', 5, 11)).toContain('String[] optional (explicit)');
        const comment = lines[3].lastIndexOf('result') + 1;
        expect(hoverContent(lines, 3, 'result', comment, comment + 6)).toBeNull();
    });
    it('should surface one invalid generated identifier fixture diagnostic', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', 'module M\n  feature F\n    import "slice.play"');
        application.set('types.play', 'concept Id : Uuid');
        const source = 'slice StateChange S\n  command C\n    id Id generated identifier\n  specification Invalid\n    when C\n      for 42';
        application.set('slice.play', source);
        const diagnostics = application.diagnosticsFor('slice.play');
        expect(diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0490')).toHaveLength(1);
        expect(validateLines(source.split('\n'), { application: application.symbolsExcept('slice.play'), placement: application.placementOf('slice.play'), path: 'slice.play', compilerDiagnostics: diagnostics }).filter(issue => issue.code === 'PLAY0490')).toHaveLength(1);
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

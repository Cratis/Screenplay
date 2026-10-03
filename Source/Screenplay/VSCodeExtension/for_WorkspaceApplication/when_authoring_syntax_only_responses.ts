// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { hoverContent, mergeSymbols, planCompletions, responseAnalysis, responseCompletions, responseTokens, scanDocument, validateLines } from '@cratis/screenplay-language';
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
    it.each(['s', 'return', 'turn', 'returns', 'name', '@s', '@return', '@turn', '@returns', '@name'])('should share precise scalar operand hover and semantic tokens for %s', operand => {
        const property = operand.replace('@', '');
        for (const separator of [' ', ' \u2003\t']) {
            const lines = ['command C', `  ${operand} String`, `  returns${separator}${operand}`];
            const column = 9 + separator.length + (operand.startsWith('@') ? 1 : 0);
            expect(responseTokens(lines).filter(token => token.line === 2)).toEqual([
                { line: 2, column: 2, length: 7, type: 0 },
                { line: 2, column, length: property.length, type: 1 },
            ]);
            expect(hoverContent(lines, 2, 'returns', 3, 10)).toContain('String');
            expect(hoverContent(lines, 2, property, column + 1, column + 1 + property.length)).toContain('String');
        }
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
    it.each([
        ['"01234567-89ab-cdef-0123-456789abcdef"', []],
        ['42', ['PLAY0490']],
    ])('should validate isolated unsaved specifications against split commands for %s', (value, codes) => {
        const application = new WorkspaceApplication();
        application.set('types.play', 'concept Id : Uuid');
        application.set('command.play', 'command C\n  id Id generated identifier');
        application.set('other.play', 'command Other\n  value String');
        application.set('spec.play', 'specification Old');
        const lines = ['specification S', '  when C', `    for ${value}`];
        const context = { application: application.symbolsExcept('spec.play'), placement: application.placementOf('spec.play'), path: 'spec.play' };
        const analysis = responseAnalysis(lines, context.application.authoringDocuments, context.placement, context.path);
        expect(analysis.diagnostics.map(diagnostic => diagnostic.code)).toEqual(codes);
        expect(analysis.specifications.get(0)?.location).toEqual({ path: 'spec.play', line: 1, column: 1 });
        expect(validateLines(lines, context).filter(issue => issue.severity !== 'information').map(issue => issue.code)).toEqual(codes);
    });
    it('should resolve generated fixtures and return assertions across isolated unsaved documents', () => {
        const application = new WorkspaceApplication();
        application.set('types.play', 'concept Id : Uuid');
        application.set('command.play', 'command C\n  receipt Id generated\n  returns\n    result = receipt');
        const lines = ['specification S', '  when C', '    generated receipt = 42', '  then returns', '    result = 42'];
        const context = { application: application.symbolsExcept('spec.play'), path: 'spec.play' };
        expect(validateLines(lines, context).filter(issue => issue.severity !== 'information').map(issue => issue.code)).toEqual(['PLAY0490', 'PLAY0491']);
        expect(validateLines(lines.map(line => line.replace('42', '"01234567-89ab-cdef-0123-456789abcdef"')), context).filter(issue => issue.severity !== 'information')).toEqual([]);
    });
    it('should honor imported placements when commands share a name across features', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', 'import "types.play"\nmodule M\n  feature F1\n    import "generated.play"\n  feature F2\n    import "local.play"\n    import "spec.play"');
        application.set('types.play', 'concept Id : Uuid');
        application.set('generated.play', 'slice StateChange Generated\n  command C\n    id Id generated identifier\n    returns @id');
        application.set('local.play', 'slice StateChange Local\n  command C\n    id Int identifier\n    returns @id');
        application.set('spec.play', 'slice StateChange S');
        const lines = ['slice StateChange S', '  specification Check', '    when C', '      for 42', '    then returns 42'];
        const context = { application: application.symbolsExcept('spec.play'), placement: application.placementOf('spec.play'), path: 'spec.play' };
        expect(context.placement).toEqual(['M', 'F2']);
        expect(validateLines(lines, context).filter(issue => issue.severity !== 'information')).toEqual([]);
        application.set('application.play', 'import "types.play"\nmodule M\n  feature F1\n    import "generated.play"\n    import "spec.play"\n  feature F2\n    import "local.play"');
        const moved = { application: application.symbolsExcept('spec.play'), placement: application.placementOf('spec.play'), path: 'spec.play' };
        expect(moved.placement).toEqual(['M', 'F1']);
        expect(validateLines(lines, moved).filter(issue => issue.severity !== 'information').map(issue => issue.code)).toEqual(['PLAY0490', 'PLAY0491']);
    });
    it('should preserve real duplicate errors rather than renaming supplied slices', () => {
        const application = new WorkspaceApplication();
        application.set('other.play', 'slice StateChange S\n  command Other');
        const lines = ['slice StateChange S', '  command C', '    value String'];
        expect(validateLines(lines, { application: application.symbolsExcept('current.play'), path: 'current.play' }).map(issue => issue.code)).toContain('PLAY0173');
    });
    it.each(['command', '@command'])('should complete and hover response sources from the typed owner for %s', name => {
        const application = new WorkspaceApplication();
        application.set('other.play', 'command Other\n  foreign String');
        const lines = ['command C', `  ${name} String`, '  returns @command'];
        const symbols = mergeSymbols(scanDocument(lines), application.symbolsExcept('current.play'));
        expect(symbols.commands.map(command => command.name)).toEqual(['C', 'Other']);
        expect(responseCompletions(lines, 2, '  returns @', symbols)?.map(entry => entry.insertText)).toEqual(['command']);
        expect(hoverContent(lines, 2, 'returns', 3, 10)).toContain('String');
        expect(hoverContent(lines, 2, 'command', 12, 19)).toContain('String');
        const record = ['command C', `  ${name} String`, '  returns', '    @result = @command'];
        expect(responseCompletions(record, 3, '    result = ', mergeSymbols(scanDocument(record), symbols))?.map(entry => entry.label)).toEqual(['command']);
        expect(hoverContent(record, 3, 'result', 6, 12)).toContain('String (inferred)');
    });
    it.each(['String optional', 'String[] optional'])('should share full explicit and inferred response type wording for %s', type => {
        for (const explicit of [false, true]) {
            const lines = ['command C', `  value ${type}`, '  returns', `    result ${explicit ? `${type} ` : ''}= value`, 'specification S', '  when C', '  then returns', '    '];
            expect(responseCompletions(lines, 7, lines[7], scanDocument(lines))?.[0].documentation).toContain(`${type}. Syntax-only`);
            expect(hoverContent(lines, 3, 'result', 5, 11)).toContain(`${type} (${explicit ? 'explicit' : 'inferred'})`);
        }
    });
    it('should preserve ordinary optional suggestions alongside eligible generation modifiers', () => {
        const completions = (before: string) => {
            const plan = planCompletions(['concept Id : Uuid', 'command C', before], 2, before);
            return plan.kind === 'entries' ? plan.entries.map(entry => entry.label) : [];
        };
        expect(completions('  id Id ')).toEqual(['optional', 'generated', 'generated identifier', 'identifier']);
        expect(completions('  id Id o')).toEqual(['optional']);
        expect(completions('  id Id generated ')).toEqual(['identifier']);
        expect(completions('  id Id optional ')).not.toContain('generated');
        expect(completions('  id Id generated identifier ')).not.toContain('optional');
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

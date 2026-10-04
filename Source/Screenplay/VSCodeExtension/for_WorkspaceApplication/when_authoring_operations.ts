// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { destinationHints, hoverContent, operationCompletions, responseTokens, scanDocument, validateLines } from '@cratis/screenplay-language';
import { WorkspaceApplication } from '../WorkspaceApplication';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));

describe('when authoring operations across workspace files', () => {
    it('should use placed typed declarations for unsaved sources and suppress operation destination hints', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', 'module M\n  feature F\n    import "*.slice.play"');
        application.set('intent.slice.play', 'system Mailer\nslice StateChange Shared\n  operation Send\n    uses Mailer\n    recipient String optional\n  event Recorded');
        const source = 'slice StateChange Here\n  command C\n    mine String\n    produces Shared.Send\n      recipient = mi\n    produces Recorded';
        application.set('current.slice.play', source);
        const context = application.symbolsExcept('current.slice.play');
        expect(context.authoringPlacement).toEqual(['M', 'F']);
        const symbols = { ...scanDocument(source.split('\n')), ...context };
        expect(operationCompletions(source.split('\n'), 4, '      recipient = mi', symbols)?.map(entry => entry.label)).toEqual(['mine']);
        expect(destinationHints(source.split('\n'), context).map(hint => hint.label)).toEqual(['for <new event source>']);
        expect(application.operationDeclarations('current.slice.play').declarations[0].location.path).toBe('intent.slice.play');
        expect(hoverContent(source.split('\n'), 3, 'Send', 21, 25, context)).toContain('M.F.Shared.Send');
        application.set('intent.slice.play', 'system Mailer\nslice StateChange Shared\n  operation Deliver\n    uses Mailer');
        expect(application.diagnosticsFor('current.slice.play').map(diagnostic => diagnostic.code)).toContain('PLAY0497');
        expect(destinationHints(source.split('\n'), application.symbolsExcept('current.slice.play'))).toEqual([]);
    });
    it('should surface command-only diagnostics for placed and isolated reactions without event repair advice', () => {
        const application = new WorkspaceApplication();
        application.set('intent.play', 'system Mailer\nmodule M\n  feature F\n    slice StateChange Shared\n      operation Send\n        uses Mailer');
        const lines = ['reaction R', '  every 1 day', '    produces Send'];
        const context = { application: application.symbolsExcept('reaction.play'), path: 'reaction.play' };
        const issues = validateLines(lines, context);
        expect(issues.map(issue => issue.code)).toContain('PLAY0499');
        expect(issues.some(issue => issue.code === 'PLAY0166')).toBe(false);
    });
    it('should not apply event metadata checks to inline operation input names', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', 'module M\n  feature F\n    import "slice.play"');
        const source = 'system Mailer\nslice StateChange S\n  command C\n    count Int\n    produces operation Send\n      uses Mailer\n      sequence Int = count\n      occurred Int = count';
        application.set('slice.play', source);
        const issues = validateLines(source.split('\n'), { application: application.symbolsExcept('slice.play'), path: 'slice.play', placement: application.placementOf('slice.play'), compilerDiagnostics: application.diagnosticsFor('slice.play') });
        expect(issues.filter(issue => issue.severity !== 'information')).toEqual([]);
    });
    it('should keep keyword input highlighting contextual and phase headers bare', () => {
        const property = grammar.repository['keyword-input'];
        for (const name of ['system', 'operation', 'command', 'execute', 'compensate', '@uses']) {
            expect(new RegExp(property.match).exec(`      ${name} String = value`)?.[1]).toBe(name);
        }
        const phase = grammar.repository['operation-phase'];
        expect(new RegExp(phase.begin).test('      execute String = value')).toBe(false);
        expect(new RegExp(phase.begin).test('      execute // phase')).toBe(true);
        expect(phase.patterns.some((pattern: { include: string }) => pattern.include === '#fenced-csharp')).toBe(true);
        const lines = ['system Mailer', 'operation Send', '  uses Mailer', '  command String', '  execute', '    implementation', '      hint "Guide"'];
        expect(responseTokens(lines).find(token => token.line === 3 && token.column === 2)?.type).toBe(1);
        expect(responseTokens(lines).find(token => token.line === 4 && token.column === 2)?.type).toBe(0);
    });
});

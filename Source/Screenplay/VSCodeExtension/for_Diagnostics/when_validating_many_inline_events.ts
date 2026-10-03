// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as vscode from 'vscode';
import { ApplicationIndex } from '../ApplicationIndex';
import { registerDiagnostics } from '../Diagnostics';
import { WorkspaceApplication } from '../WorkspaceApplication';

const editor = vi.hoisted(() => ({ documents: [] as vscode.TextDocument[], diagnostics: [] as vscode.Diagnostic[], codeReads: 0 }));

vi.mock('vscode', async importOriginal => {
    const original = await importOriginal<typeof import('../vscode.stub')>();
    return {
        ...original,
        Range: class extends original.Range {
            constructor(startLine: number, startCharacter: number, endLine: number, endCharacter: number) {
                if ([startLine, startCharacter, endLine, endCharacter].some(value => value < 0)) throw new Error('Invalid negative diagnostic position');
                super(startLine, startCharacter, endLine, endCharacter);
            }
        },
        DiagnosticSeverity: { Error: 0, Warning: 1, Information: 2 },
        DiagnosticTag: { Deprecated: 2 },
        Diagnostic: class {
            source?: string;
            #code: string | number | undefined;
            constructor(readonly range: vscode.Range, readonly message: string, readonly severity: number) {}
            get code() { editor.codeReads++; return this.#code; }
            set code(value: string | number | undefined) { this.#code = value; }
        },
        languages: {
            createDiagnosticCollection: () => ({
                set: (_uri: vscode.Uri, diagnostics: vscode.Diagnostic[]) => { editor.diagnostics = diagnostics; },
                dispose: () => {},
            }),
        },
        workspace: {
            get textDocuments() { return editor.documents; },
            onDidOpenTextDocument: () => ({ dispose: () => {} }),
            onDidChangeTextDocument: () => ({ dispose: () => {} }),
            onDidCloseTextDocument: () => ({ dispose: () => {} }),
        },
    };
});

function document(count: number): string {
    return [...Array.from({ length: count }, (_, index) => `import Other.Imported${index}`),
        'module Projects', '  feature Naming', ...Array.from({ length: count }, (_, index) => [
            `    slice StateChange Rename${index}`, `      command Rename${index}`,
            '        projectId Uuid identifier', `        produces event Renamed${index}`,
            '          projectId Uuid = projectId',
        ]).flat()].join('\n');
}

function refresh(application: WorkspaceApplication, source: string): void {
    const lines = source.split('\n');
    editor.documents = [{
        languageId: 'screenplay', isClosed: false, uri: vscode.Uri.file('/model.play'),
        getText: () => source, lineCount: lines.length, lineAt: (index: number) => ({ text: lines[index] }),
    } as vscode.TextDocument];
    const index = {
        fileOf: () => ({ application, path: 'model.play' }),
        onDidChange: () => ({ dispose: () => {} }),
    } as unknown as ApplicationIndex;
    registerDiagnostics({ subscriptions: [] } as unknown as vscode.ExtensionContext, index);
}

describe('when validating many inline events in the workspace', () => {
    beforeEach(() => { editor.codeReads = 0; editor.diagnostics = []; editor.documents = []; });

    it('should keep compilation and diagnostic merging linear on every edit', () => {
        const application = new WorkspaceApplication();
        const measure = (count: number) => {
            const source = document(count);
            // Include the real application recompile after an edit, not a cached diagnostics lookup.
            const filters = vi.spyOn(Array.prototype, 'filter');
            const searches = vi.spyOn(Array.prototype, 'some');
            editor.codeReads = 0;
            let collectionWork: number;
            let codeReads: number;
            try {
                application.set('model.play', source);
                refresh(application, source);
                codeReads = editor.codeReads;
                collectionWork = [...filters.mock.contexts, ...searches.mock.contexts]
                    .reduce<number>((sum, receiver) => sum + (receiver as unknown[]).length, 0);
            } finally {
                filters.mockRestore();
                searches.mockRestore();
            }
            expect(editor.diagnostics).toHaveLength(count);
            expect(editor.diagnostics.every(diagnostic => diagnostic.code === 'PLAY0469' && diagnostic.severity === vscode.DiagnosticSeverity.Warning)).toBe(true);
            expect(new Set(editor.diagnostics.map(diagnostic => diagnostic.range.start.line)).size).toBe(count);
            return { collectionWork, codeReads };
        };
        const small = measure(100);
        const large = measure(200);
        expect(small.collectionWork).toBeGreaterThan(0);
        expect(small.codeReads).toBeGreaterThan(0);
        expect(large.collectionWork).toBeLessThan(small.collectionWork * 2.2);
        expect(large.codeReads).toBeLessThan(small.codeReads * 2.2);
    });

    it('should mark legacy optionality deprecated at its complete type span without duplicate compiler advice', () => {
        const source = 'type T\n  note String? // keep?';
        const application = new WorkspaceApplication();
        application.set('model.play', source);
        refresh(application, source);
        expect(editor.diagnostics).toHaveLength(1);
        expect(editor.diagnostics[0].code).toBe('PLAY0479');
        expect(editor.diagnostics[0].tags).toEqual([vscode.DiagnosticTag.Deprecated]);
        expect(editor.diagnostics[0].severity).toBe(vscode.DiagnosticSeverity.Information);
        expect(editor.diagnostics[0].range.start).toEqual(new vscode.Position(1, 7));
        expect(editor.diagnostics[0].range.end).toEqual(new vscode.Position(1, 14));
    });

    it.each([
        ['command C', '  value Missing?', 'Missing?'],
        ['command C', '  value Missing   optional', 'Missing   optional'],
        ['command C', '\tvalue\tMissing\toptional', 'Missing\toptional'],
        ['command C', '  value Inconnué?', 'Inconnué?'],
        ['command C', '  value Missing', 'Missing'],
        ['command C\n  produces event Recorded', '    value Missing? = input', 'Missing?'],
        ['command C\n  produces event Recorded', '    value Missing   optional = input', 'Missing   optional'],
    ])('should refresh exact source ranges for %s / %s without negative positions', (header, line, spelling) => {
        const source = header + '\n' + line;
        const application = new WorkspaceApplication();
        application.set('model.play', source);
        expect(() => refresh(application, source)).not.toThrow();
        const diagnostic = editor.diagnostics.find(item => item.code === 'PLAY0165');
        expect(diagnostic).toBeDefined();
        expect(diagnostic?.range.start).toEqual(new vscode.Position(header.split('\n').length, line.indexOf(spelling)));
        expect(diagnostic?.range.end).toEqual(new vscode.Position(header.split('\n').length, line.indexOf(spelling) + spelling.length));
        for (const item of editor.diagnostics) {
            expect(item.range.start.character).toBeGreaterThanOrEqual(0);
            expect(item.range.end.character).toBeGreaterThan(item.range.start.character);
        }
    });

    it('should preserve editor precedence and distinct codes or lines when merging diagnostics', () => {
        const source = document(2);
        const application = new WorkspaceApplication();
        application.set('model.play', source);
        const compiled = application.diagnosticsFor('model.play');
        expect(compiled).toHaveLength(2);
        vi.spyOn(application, 'diagnosticsFor').mockReturnValue([
            ...compiled,
            { ...compiled[0], code: 'PLAY0473', severity: 'error', message: 'A distinct code on the same line' },
            { ...compiled[0], location: { ...compiled[0].location, line: 5 }, message: 'The same code on another line' },
        ]);
        refresh(application, source);
        expect(editor.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.range.start.line}`)).toEqual([
            'PLAY0469@8', 'PLAY0469@13', 'PLAY0473@8', 'PLAY0469@4',
        ]);
        expect(editor.diagnostics[0].message).toContain('The payload copies');
        expect(editor.diagnostics[2].severity).toBe(vscode.DiagnosticSeverity.Error);
    });
});

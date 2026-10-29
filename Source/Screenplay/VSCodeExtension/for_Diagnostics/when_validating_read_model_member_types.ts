// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it, vi } from 'vitest';
import * as vscode from 'vscode';
import { ApplicationIndex } from '../ApplicationIndex';
import { registerDiagnostics } from '../Diagnostics';
import { WorkspaceApplication } from '../WorkspaceApplication';

const editor = vi.hoisted(() => ({ documents: [] as vscode.TextDocument[], diagnostics: [] as vscode.Diagnostic[] }));

vi.mock('vscode', async importOriginal => ({
    ...await importOriginal<typeof import('../vscode.stub')>(),
    DiagnosticSeverity: { Error: 0, Warning: 1, Information: 2 },
    DiagnosticTag: { Deprecated: 2 },
    Diagnostic: class {
        code?: string | number;
        constructor(readonly range: vscode.Range, readonly message: string, readonly severity: number) {}
    },
    languages: {
        createDiagnosticCollection: () => ({ set: (_uri: vscode.Uri, diagnostics: vscode.Diagnostic[]) => { editor.diagnostics = diagnostics; }, dispose: () => {} }),
    },
    workspace: {
        get textDocuments() { return editor.documents; },
        onDidOpenTextDocument: () => ({ dispose: () => {} }),
        onDidChangeTextDocument: () => ({ dispose: () => {} }),
        onDidCloseTextDocument: () => ({ dispose: () => {} }),
    },
}));

const source = 'module M\n  feature F\n    slice StateView View\n      readmodel Parent\n        child Child[]\n      readmodel Child\n        value String';
const message = "Read model 'Parent' member 'child' references unsupported type 'Child'. Use a primitive, concept or top-level 'type'; read-model shapes and slice-local 'type' declarations are not member types.";

function refresh(text: string, application?: WorkspaceApplication): void {
    const lines = text.split('\n');
    editor.documents = [{
        languageId: 'screenplay', isClosed: false, uri: vscode.Uri.file('/model.play'),
        getText: () => text, lineCount: lines.length, lineAt: (index: number) => ({ text: lines[index] }),
    } as vscode.TextDocument];
    const index = {
        fileOf: () => application ? { application, path: 'model.play' } : undefined,
        onDidChange: () => ({ dispose: () => {} }),
    } as unknown as ApplicationIndex;
    registerDiagnostics({ subscriptions: [] } as unknown as vscode.ExtensionContext, index);
}

describe('when validating read-model member types in VS Code', () => {
    beforeEach(() => { editor.documents = []; editor.diagnostics = []; });

    it('should expose the native local error at the member', () => {
        refresh(source);
        const diagnostics = editor.diagnostics.filter(diagnostic => diagnostic.range.start.line === 4);
        diagnostics.map(diagnostic => [diagnostic.code, diagnostic.severity, diagnostic.message, diagnostic.range.start.character]).should.deep.equal([
            ['PLAY0273', vscode.DiagnosticSeverity.Error, message, 14],
        ]);
    });
    it('should deduplicate the same assembled compiler and language diagnostic', () => {
        const application = new WorkspaceApplication();
        application.set('model.play', source);
        refresh(source, application);
        editor.diagnostics.filter(diagnostic => diagnostic.range.start.line === 4).map(diagnostic => [diagnostic.code, diagnostic.message]).should.deep.equal([['PLAY0273', message]]);
    });
    it('should resolve reusable types from the assembled workspace', () => {
        const application = new WorkspaceApplication();
        application.set('values.play', 'type Child\n  value String');
        application.set('model.play', source);
        refresh(source, application);
        editor.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0273' || diagnostic.code === 'PLAY0165').should.be.empty;
    });
});

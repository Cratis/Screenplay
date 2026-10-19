// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as vscode from 'vscode';

export interface FakeDiagnostic { range: unknown; message: string; severity: number; code?: unknown; source?: string }
export interface FakeDocument { uri: vscode.Uri; languageId: string; isDirty: boolean; lineCount: number; lineAt(line: number): { text: string } }

// The editor as the verdict specs need it: collections that remember what was set, commands, messages and events.
export const editor = {
    collections: new Map<string, Map<string, FakeDiagnostic[]>>(),
    disposedCollections: [] as string[],
    commands: new Map<string, () => unknown>(),
    warnings: [] as string[],
    informations: [] as string[],
    configuration: { onSave: false },
    documents: [] as FakeDocument[],
    saved: undefined as ((document: vscode.TextDocument) => void) | undefined,
    changed: undefined as ((event: vscode.TextDocumentChangeEvent) => void) | undefined,
    reset(): void {
        this.collections.clear(); this.disposedCollections = []; this.commands.clear(); this.warnings = []; this.informations = [];
        this.configuration = { onSave: false }; this.documents = []; this.saved = undefined; this.changed = undefined;
    },
    diagnosticsOf(collection: string, uri: vscode.Uri): FakeDiagnostic[] { return this.collections.get(collection)?.get(uri.toString()) ?? []; },
    count(collection: string): number { return this.collections.get(collection)?.size ?? 0; },
};

export function createVscode(original: typeof import('../../vscode.stub')): unknown {
    return {
        ...original,
        DiagnosticSeverity: { Error: 0, Warning: 1, Information: 2 },
        Diagnostic: class { code?: unknown; source?: string; constructor(readonly range: unknown, readonly message: string, readonly severity: number) {} },
        languages: {
            createDiagnosticCollection: (name: string) => {
                const entries = new Map<string, FakeDiagnostic[]>();
                editor.collections.set(name, entries);
                return {
                    set: (uri: vscode.Uri, markers: FakeDiagnostic[]) => entries.set(uri.toString(), markers),
                    delete: (uri: vscode.Uri) => entries.delete(uri.toString()),
                    clear: () => entries.clear(),
                    dispose: () => { editor.disposedCollections.push(name); entries.clear(); },
                };
            },
        },
        commands: { registerCommand: (name: string, callback: () => unknown) => { editor.commands.set(name, callback); return { dispose: () => {} }; } },
        window: {
            showWarningMessage: async (message: string) => { editor.warnings.push(message); },
            showInformationMessage: async (message: string) => { editor.informations.push(message); },
            createOutputChannel: () => ({ appendLine: () => {}, dispose: () => {} }),
        },
        workspace: {
            get textDocuments() { return editor.documents; },
            getConfiguration: () => ({ get: (_name: string, fallback: unknown) => editor.configuration.onSave ?? fallback }),
            onDidSaveTextDocument: (callback: typeof editor.saved) => { editor.saved = callback; return { dispose: () => {} }; },
            onDidChangeTextDocument: (callback: typeof editor.changed) => { editor.changed = callback; return { dispose: () => {} }; },
        },
    };
}

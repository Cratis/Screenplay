// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as vscode from 'vscode';

export interface FakeDocument { uri: vscode.Uri; languageId: string; isDirty: boolean; version: number; lineCount: number }
export interface FakeEdit { uri: vscode.Uri; text: string }

// The editor as the migration specs need it: an active document, an input box, edits and messages.
export const editor = {
    commands: new Map<string, () => unknown>(),
    active: undefined as FakeDocument | undefined,
    answer: '5' as string | undefined,
    accept: true,
    edits: [] as FakeEdit[],
    warnings: [] as string[],
    informations: [] as string[],
    logged: [] as string[],
    reset(): void {
        this.commands.clear(); this.active = undefined; this.answer = '5'; this.accept = true;
        this.edits = []; this.warnings = []; this.informations = []; this.logged = [];
    },
};

export function createVscode(original: typeof import('../../vscode.stub')): unknown {
    return {
        ...original,
        WorkspaceEdit: class {
            readonly replaced: FakeEdit[] = [];
            replace(uri: vscode.Uri, _range: unknown, text: string): void { this.replaced.push({ uri, text }); }
        },
        commands: { registerCommand: (name: string, callback: () => unknown) => { editor.commands.set(name, callback); return { dispose: () => {} }; } },
        window: {
            get activeTextEditor() { return editor.active ? { document: editor.active } : undefined; },
            showInputBox: async () => editor.answer,
            showWarningMessage: async (message: string) => { editor.warnings.push(message); },
            showInformationMessage: async (message: string) => { editor.informations.push(message); },
            createOutputChannel: () => ({ appendLine: (line: string) => { editor.logged.push(line); }, dispose: () => {} }),
        },
        workspace: {
            applyEdit: async (edit: { replaced: FakeEdit[] }) => {
                if (editor.accept) editor.edits.push(...edit.replaced);
                return editor.accept;
            },
        },
    };
}

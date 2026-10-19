// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { ServerDiagnostic } from './ServerDiagnostic';

const maximumScanned = 500;

/**
 * Where a finding about the whole application is shown: the model's root file, the first `.play` file found
 * breadth-first under the root with names in alphabetical order. Null when the root holds none.
 */
export function applicationRootFile(root: string): string | undefined {
    const queue = [''];
    let scanned = 0;
    while (queue.length && scanned < maximumScanned) {
        const folder = queue.shift()!;
        let entries: fs.Dirent[];
        try { entries = fs.readdirSync(path.join(root, folder), { withFileTypes: true }).sort((left, right) => left.name.localeCompare(right.name)); } catch { continue; }
        scanned += entries.length;
        const file = entries.find(entry => entry.isFile() && entry.name.endsWith('.play'));
        if (file) return path.join(folder, file.name).split(path.sep).join('/');
        queue.push(...entries.filter(entry => entry.isDirectory() && !entry.name.startsWith('.') && entry.name !== 'node_modules').map(entry => path.join(folder, entry.name)));
    }
    return undefined;
}

export const verdictSource = 'Screenplay C# check';

/** The C# tool's completeness verdicts, kept apart from the editor's own diagnostics and from repair findings. */
export class VerdictCollection implements vscode.Disposable {
    readonly #collection = vscode.languages.createDiagnosticCollection('screenplay-csharp-verdicts');

    /** Replaces everything. A document with unsaved edits is skipped: the verdict describes the saved file. */
    publish(root: string, verdicts: readonly ServerDiagnostic[]): void {
        this.#collection.clear();
        const grouped = new Map<string, { uri: vscode.Uri; markers: vscode.Diagnostic[] }>();
        for (const verdict of verdicts) {
            // A finding about the whole application (no path, position 0) lands on the root file; any other
            // unattributed finding is never guessed onto a file.
            const unattributed = verdict.location.path === null && verdict.location.line === 0 && verdict.location.column === 0;
            const file = verdict.location.path ?? (unattributed ? applicationRootFile(root) : undefined);
            if (!file) continue;
            const uri = vscode.Uri.file(path.join(root, file));
            const document = vscode.workspace.textDocuments.find(open => open.uri.toString() === uri.toString());
            if (document?.isDirty) continue;
            const line = Math.max(verdict.location.line - 1, 0), column = Math.max(verdict.location.column - 1, 0);
            const end = Math.max(column + 1, document && line < document.lineCount ? document.lineAt(line).text.length : column + 1);
            const severity = verdict.severity === 'Error' ? vscode.DiagnosticSeverity.Error : verdict.severity === 'Warning' ? vscode.DiagnosticSeverity.Warning : vscode.DiagnosticSeverity.Information;
            const marker = new vscode.Diagnostic(new vscode.Range(line, column, line, end), verdict.message, severity);
            marker.code = verdict.code;
            marker.source = verdictSource;
            const key = uri.toString();
            const entry = grouped.get(key) ?? { uri, markers: [] };
            entry.markers.push(marker);
            grouped.set(key, entry);
        }
        for (const { uri, markers } of grouped.values()) this.#collection.set(uri, markers);
    }

    /** A document changed: its verdicts describe text that no longer exists. */
    clearDocument(uri: vscode.Uri): void { this.#collection.delete(uri); }
    clear(): void { this.#collection.clear(); }
    dispose(): void { this.#collection.dispose(); }
}

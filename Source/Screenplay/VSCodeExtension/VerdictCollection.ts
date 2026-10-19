// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as path from 'node:path';
import { ServerDiagnostic } from './ServerDiagnostic';

export const verdictSource = 'Screenplay C# check';

/** The C# tool's completeness verdicts, kept apart from the editor's own diagnostics and from repair findings. */
export class VerdictCollection implements vscode.Disposable {
    readonly #collection = vscode.languages.createDiagnosticCollection('screenplay-csharp-verdicts');

    /** Replaces everything. A document with unsaved edits is skipped: the verdict describes the saved file. */
    publish(root: string, verdicts: readonly ServerDiagnostic[]): void {
        this.#collection.clear();
        const grouped = new Map<string, { uri: vscode.Uri; markers: vscode.Diagnostic[] }>();
        for (const verdict of verdicts) {
            if (!verdict.location.path) continue; // Never guess a file for an unattributed finding.
            const uri = vscode.Uri.file(path.join(root, verdict.location.path));
            const document = vscode.workspace.textDocuments.find(open => open.uri.toString() === uri.toString());
            if (document?.isDirty) continue;
            const line = verdict.location.line - 1, column = verdict.location.column - 1;
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

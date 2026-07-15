// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { languageId, validateLines, ValidationIssue } from '@cratis/screenplay-language';

const validationDelay = 300;

function toDiagnostic(issue: ValidationIssue): vscode.Diagnostic {
    const range = new vscode.Range(
        issue.line,
        issue.startColumn - 1,
        issue.line,
        issue.endColumn - 1,
    );
    const severity =
        issue.severity === 'error'
            ? vscode.DiagnosticSeverity.Error
            : vscode.DiagnosticSeverity.Warning;
    const diagnostic = new vscode.Diagnostic(range, issue.message, severity);
    diagnostic.source = languageId;
    return diagnostic;
}

export function registerDiagnostics(context: vscode.ExtensionContext): void {
    const collection = vscode.languages.createDiagnosticCollection(languageId);
    context.subscriptions.push(collection);

    const handles = new Map<string, ReturnType<typeof setTimeout>>();

    const refresh = (document: vscode.TextDocument) => {
        if (document.languageId !== languageId) return;
        const lines = document.getText().split(/\r?\n/);
        collection.set(document.uri, validateLines(lines).map(toDiagnostic));
    };
    const scheduleRefresh = (document: vscode.TextDocument) => {
        if (document.languageId !== languageId) return;
        const key = document.uri.toString();
        const pending = handles.get(key);
        if (pending !== undefined) clearTimeout(pending);
        handles.set(key, setTimeout(() => refresh(document), validationDelay));
    };

    context.subscriptions.push(
        vscode.workspace.onDidOpenTextDocument(refresh),
        vscode.workspace.onDidChangeTextDocument((event) => scheduleRefresh(event.document)),
        vscode.workspace.onDidCloseTextDocument((document) => {
            const pending = handles.get(document.uri.toString());
            if (pending !== undefined) clearTimeout(pending);
            handles.delete(document.uri.toString());
            collection.delete(document.uri);
        }),
    );
    vscode.workspace.textDocuments.forEach(refresh);
}

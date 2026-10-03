// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { Diagnostic as CompilerDiagnostic, DiagnosticCodes, legacyOptionalTypeLength } from '@cratis/screenplay-compiler';
import { languageId, validateLines, ValidationIssue } from '@cratis/screenplay-language';
import { ApplicationIndex } from './ApplicationIndex';

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
            : issue.severity === 'information'
                ? vscode.DiagnosticSeverity.Information
                : vscode.DiagnosticSeverity.Warning;
    const diagnostic = new vscode.Diagnostic(range, issue.message, severity);
    diagnostic.source = languageId;
    if (issue.code) {
        diagnostic.code = issue.code;
    }
    if (issue.code === DiagnosticCodes.LegacyOptionalSuffix) diagnostic.tags = [vscode.DiagnosticTag.Deprecated];

    return diagnostic;
}

// A compiler diagnostic carries where it starts; it covers the rest of that line.
function fromCompiler(document: vscode.TextDocument, compiled: CompilerDiagnostic): vscode.Diagnostic {
    const line = Math.min(compiled.location.line - 1, document.lineCount - 1);
    const length = legacyOptionalTypeLength(document.lineAt(line).text, compiled);
    const range = new vscode.Range(line, compiled.location.column - 1, line, length > 0 ? compiled.location.column - 1 + length : document.lineAt(line).text.length);
    const severity = compiled.severity === 'error' ? vscode.DiagnosticSeverity.Error : compiled.severity === 'information' ? vscode.DiagnosticSeverity.Information : vscode.DiagnosticSeverity.Warning;
    const diagnostic = new vscode.Diagnostic(range, compiled.message, severity);
    diagnostic.source = languageId;
    diagnostic.code = compiled.code;
    if (compiled.code === DiagnosticCodes.LegacyOptionalSuffix) diagnostic.tags = [vscode.DiagnosticTag.Deprecated];
    return diagnostic;
}

// Validates each open document as a file of the application its workspace folder holds: names the other files
// declare are known, and what compiling the application says about the file's imports and its placement is
// reported with it. A document outside every workspace folder is validated on its own.
export function registerDiagnostics(context: vscode.ExtensionContext, index: ApplicationIndex): void {
    const collection = vscode.languages.createDiagnosticCollection(languageId);
    context.subscriptions.push(collection);

    const handles = new Map<string, ReturnType<typeof setTimeout>>();

    const refresh = (document: vscode.TextDocument) => {
        if (document.languageId !== languageId || document.isClosed) return;
        const lines = document.getText().split(/\r?\n/);
        const file = index.fileOf(document.uri);
        const compilerDiagnostics = file?.application.diagnosticsFor(file.path);
        const issues = validateLines(lines, { application: file?.application.symbolsExcept(file.path), placement: file?.application.placementOf(file.path), path: file?.path, compilerDiagnostics }).map(toDiagnostic);
        const compiled = compilerDiagnostics?.map(diagnostic => fromCompiler(document, diagnostic)) ?? [];
        const reported = new Set(issues.map(issue => `${issue.code}:${issue.range.start.line}`));
        collection.set(document.uri, [...issues, ...compiled.filter(diagnostic => !reported.has(`${diagnostic.code}:${diagnostic.range.start.line}`))]);
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
        // A change to any file can resolve or break a name or an import in another.
        index.onDidChange(() => vscode.workspace.textDocuments.forEach(refresh)),
    );
    vscode.workspace.textDocuments.forEach(refresh);
}

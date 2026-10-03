// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { DiagnosticCodes, findQuickFixes, isQuickFixDiagnostic, prepareQuickFixes, QuickFix } from '@cratis/screenplay-compiler';
import { languageId } from '@cratis/screenplay-language';
import { ApplicationIndex } from './ApplicationIndex';

const applyCommand = 'screenplay.applyQuickFix';
const migrateOptional = vscode.CodeActionKind.Source.append('screenplay.migrateOptional');

interface PendingQuickFix {
    readonly uri: vscode.Uri;
    readonly version: number;
    readonly fix: QuickFix;
}

export function registerCodeActions(context: vscode.ExtensionContext, index: ApplicationIndex): void {
    const cache = new WeakMap<vscode.TextDocument, { version: number; placement: string; fixes: ReturnType<typeof prepareQuickFixes> }>();
    context.subscriptions.push(vscode.languages.registerCodeActionsProvider(languageId, {
        provideCodeActions(document, range, request, token) {
            const migrationRequested = request.only?.contains(migrateOptional) === true;
            const migrationOnly = migrationRequested && !request.only?.contains(vscode.CodeActionKind.QuickFix);
            if (request.only !== undefined && !migrationRequested && !request.only.contains(vscode.CodeActionKind.QuickFix)) return [];
            const diagnostics = request.diagnostics.filter(diagnostic => {
                const code = typeof diagnostic.code === 'object' ? diagnostic.code.value : diagnostic.code;
                return isQuickFixDiagnostic(code) && diagnostic.range.intersection(range) !== undefined;
            });
            if (token.isCancellationRequested || (!migrationRequested && diagnostics.length === 0)) return [];
            const file = index.fileOf(document.uri);
            const placement = file?.application.placementOf(file.path);
            const placementKey = JSON.stringify(placement);
            const version = document.version;
            let analysis = cache.get(document);
            if (analysis?.version !== version || analysis.placement !== placementKey) {
                analysis = { version, placement: placementKey, fixes: prepareQuickFixes(document.getText(), { placement }) };
                cache.set(document, analysis);
            }
            const occurrences = migrationOnly ? [] : analysis.fixes(diagnostics.map(diagnostic => ({
                line: diagnostic.range.start.line + 1,
                diagnosticCode: String(typeof diagnostic.code === 'object' ? diagnostic.code.value : diagnostic.code),
            }))).filter(fix => fix.scope === 'occurrence');
            const requested = [...occurrences, ...(request.only === undefined || migrationRequested ? analysis.fixes(undefined, DiagnosticCodes.LegacyOptionalSuffix) : [])];
            const fixes = requested.filter(fix => request.only === undefined || request.only.contains(fix.scope === 'document' ? migrateOptional : vscode.CodeActionKind.QuickFix));
            if (token.isCancellationRequested || document.version !== version) return [];
            return fixes.map(fix => {
                const action = new vscode.CodeAction(fix.title, fix.scope === 'document' ? migrateOptional : vscode.CodeActionKind.QuickFix);
                action.isPreferred = fix.scope === 'occurrence';
                action.command = { command: applyCommand, title: fix.title, arguments: [{ uri: document.uri, version, fix } satisfies PendingQuickFix] };
                return action;
            });
        },
    }, { providedCodeActionKinds: [vscode.CodeActionKind.QuickFix, migrateOptional] }));

    // An explicit action is the only write boundary. Never apply offsets from an older dirty buffer.
    context.subscriptions.push(vscode.commands.registerCommand(applyCommand, async (pending: PendingQuickFix) => {
        const document = vscode.workspace.textDocuments.find(document => document.uri.toString() === pending.uri.toString());
        if (document === undefined || document.version !== pending.version) {
            void vscode.window.showWarningMessage('The document changed. Request the quick fix again.');
            return false;
        }
        const file = index.fileOf(document.uri);
        const first = pending.fix.edits[0];
        if (first === undefined) return false;
        const verified = findQuickFixes(document.getText(), { line: document.positionAt(first.start).line + 1, diagnosticCode: pending.fix.diagnosticCode, placement: file?.application.placementOf(file.path) })
            .find(fix => fix.scope === pending.fix.scope && fix.diagnosticCode === pending.fix.diagnosticCode && JSON.stringify(fix.edits) === JSON.stringify(pending.fix.edits));
        if (verified === undefined) return false;
        const edit = new vscode.WorkspaceEdit();
        for (const change of verified.edits) {
            edit.replace(document.uri, new vscode.Range(document.positionAt(change.start), document.positionAt(change.start + change.length)), change.text);
        }
        return vscode.workspace.applyEdit(edit);
    }));
}

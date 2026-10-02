// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { findQuickFixes, QuickFix } from '@cratis/screenplay-compiler';
import { languageId } from '@cratis/screenplay-language';
import { ApplicationIndex } from './ApplicationIndex';

const applyCommand = 'screenplay.applyQuickFix';
const fixAll = vscode.CodeActionKind.SourceFixAll.append('screenplay');

interface PendingQuickFix {
    readonly uri: vscode.Uri;
    readonly version: number;
    readonly fix: QuickFix;
}

export function registerCodeActions(context: vscode.ExtensionContext, index: ApplicationIndex): void {
    context.subscriptions.push(vscode.languages.registerCodeActionsProvider(languageId, {
        provideCodeActions(document, range, _context, token) {
            const file = index.fileOf(document.uri);
            const version = document.version;
            const fixes = findQuickFixes(document.getText(), { line: range.start.line + 1, placement: file?.application.placementOf(file.path) });
            if (token.isCancellationRequested || document.version !== version) return [];
            return fixes.map(fix => {
                const action = new vscode.CodeAction(fix.title, fix.scope === 'document' ? fixAll : vscode.CodeActionKind.QuickFix);
                action.isPreferred = fix.scope === 'occurrence';
                action.command = { command: applyCommand, title: fix.title, arguments: [{ uri: document.uri, version, fix } satisfies PendingQuickFix] };
                return action;
            });
        },
    }, { providedCodeActionKinds: [vscode.CodeActionKind.QuickFix, fixAll] }));

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
        const verified = findQuickFixes(document.getText(), { line: document.positionAt(first.start).line + 1, placement: file?.application.placementOf(file.path) })
            .find(fix => fix.scope === pending.fix.scope && fix.diagnosticCode === pending.fix.diagnosticCode && JSON.stringify(fix.edits) === JSON.stringify(pending.fix.edits));
        if (verified === undefined) return false;
        const edit = new vscode.WorkspaceEdit();
        for (const change of verified.edits) {
            edit.replace(document.uri, new vscode.Range(document.positionAt(change.start), document.positionAt(change.start + change.length)), change.text);
        }
        return vscode.workspace.applyEdit(edit);
    }));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor, IRange, languages } from 'monaco-editor';
import { DiagnosticCodes, isQuickFixDiagnostic, prepareQuickFixes } from '@cratis/screenplay-compiler';

const migrateOptional = 'source.screenplay.migrateOptional';
const containsKind = (requested: string, kind: string) => requested === '' || requested === kind || kind.startsWith(`${requested}.`);

function intersects(left: IRange, right: IRange): boolean {
    return (left.startLineNumber < right.endLineNumber || (left.startLineNumber === right.endLineNumber && left.startColumn <= right.endColumn)) &&
        (right.startLineNumber < left.endLineNumber || (right.startLineNumber === left.endLineNumber && right.startColumn <= left.endColumn));
}

// The compiler owns recipes and verification. The adapter only translates verified offsets and pins
// edits to the analyzed buffer version, so future fixes need no editor-specific parsing or .NET bridge.
export function createCodeActionProvider(placement?: readonly string[]): languages.CodeActionProvider {
    const cache = new WeakMap<editor.ITextModel, { version: number; placement: string; fixes: ReturnType<typeof prepareQuickFixes> }>();
    return {
        provideCodeActions(model, range, context, token) {
            const migrationRequested = context.only !== undefined && containsKind(context.only, migrateOptional);
            const migrationOnly = context.only !== undefined && migrationRequested && !containsKind(context.only, 'quickfix');
            if (token.isCancellationRequested || (context.only !== undefined && !migrationRequested && !containsKind(context.only, 'quickfix'))) return { actions: [], dispose() {} };
            const diagnostics = context.markers.filter(marker => {
                const code = typeof marker.code === 'object' ? marker.code.value : marker.code;
                return isQuickFixDiagnostic(code) && intersects(marker, range);
            });
            if (!migrationRequested && diagnostics.length === 0) return { actions: [], dispose() {} };
            const version = model.getVersionId();
            const placementKey = JSON.stringify(placement) ?? '';
            let analysis = cache.get(model);
            if (analysis?.version !== version || analysis.placement !== placementKey) {
                analysis = { version, placement: placementKey, fixes: prepareQuickFixes(model.getValue(), { placement }) };
                cache.set(model, analysis);
            }
            const occurrences = migrationOnly ? [] : analysis.fixes(diagnostics.map(diagnostic => ({
                line: diagnostic.startLineNumber,
                diagnosticCode: String(typeof diagnostic.code === 'object' ? diagnostic.code.value : diagnostic.code),
            }))).filter(fix => fix.scope === 'occurrence');
            const requested = [...occurrences, ...(context.only === undefined || migrationRequested ? analysis.fixes(undefined, DiagnosticCodes.LegacyOptionalSuffix) : [])];
            const fixes = requested.filter(fix => context.only === undefined || containsKind(context.only, fix.scope === 'document' ? migrateOptional : 'quickfix'));
            if (token.isCancellationRequested || model.getVersionId() !== version) return { actions: [], dispose() {} };
            return {
                actions: fixes.map(fix => ({
                    title: fix.title,
                    kind: fix.scope === 'document' ? migrateOptional : 'quickfix',
                    isPreferred: fix.scope === 'occurrence',
                    edit: { edits: fix.edits.map(edit => {
                        const start = model.getPositionAt(edit.start);
                        const end = model.getPositionAt(edit.start + edit.length);
                        return { resource: model.uri, versionId: version, textEdit: {
                            range: { startLineNumber: start.lineNumber, startColumn: start.column, endLineNumber: end.lineNumber, endColumn: end.column },
                            text: edit.text,
                        } };
                    }) },
                })),
                dispose() {},
            };
        },
    };
}

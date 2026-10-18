// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor, IRange, languages } from 'monaco-editor';
import { DiagnosticCodes, isQuickFixDiagnostic, prepareQuickFixes } from '@cratis/screenplay-compiler';

const migrateOptional = 'source.screenplay.migrateOptional';
const migrateCompliance = 'source.screenplay.migrateCompliance';
const migrationKind = (code: string) => code === DiagnosticCodes.LegacyComplianceMarker ? migrateCompliance : migrateOptional;
const containsKind = (requested: string, kind: string) => requested === '' || requested === kind || kind.startsWith(`${requested}.`);

function intersects(left: IRange, right: IRange): boolean {
    return (left.startLineNumber < right.endLineNumber || (left.startLineNumber === right.endLineNumber && left.startColumn <= right.endColumn)) &&
        (right.startLineNumber < left.endLineNumber || (right.startLineNumber === left.endLineNumber && right.startColumn <= left.endColumn));
}

// The compiler owns recipes and verification. The adapter only translates verified offsets and pins
// edits to the analyzed buffer version, so future fixes need no editor-specific parsing or .NET bridge.
export function createCodeActionProvider(placement?: readonly string[], applicationModels?: () => ReadonlyMap<string, editor.ITextModel>): languages.CodeActionProvider {
    const cache = new WeakMap<editor.ITextModel, { version: number; placement: string; fixes: ReturnType<typeof prepareQuickFixes> }>();
    return {
        provideCodeActions(model, range, context, token) {
            const migrationRequested = context.only !== undefined && [migrateOptional, migrateCompliance].some(kind => containsKind(context.only!, kind));
            const migrationOnly = context.only !== undefined && migrationRequested && !containsKind(context.only, 'quickfix');
            if (token.isCancellationRequested || (context.only !== undefined && !migrationRequested && !containsKind(context.only, 'quickfix'))) return { actions: [], dispose() {} };
            const diagnostics = context.markers.filter(marker => {
                const code = typeof marker.code === 'object' ? marker.code.value : marker.code;
                return isQuickFixDiagnostic(code) && intersects(marker, range);
            });
            if (!migrationRequested && diagnostics.length === 0) return { actions: [], dispose() {} };
            const version = model.getVersionId();
            const models = applicationModels?.();
            const path = models === undefined ? undefined : [...models].find(([, document]) => document === model)?.[0];
            if (models !== undefined && path === undefined) return { actions: [], dispose() {} };
            const versions = new Map([...(models?.values() ?? [model])].map(document => [document, document.getVersionId()]));
            const placementKey = JSON.stringify([placement, models === undefined ? undefined : [...models].map(([path, document]) => [path, document.uri.toString(), versions.get(document)])]);
            let analysis = cache.get(model);
            if (analysis?.version !== version || analysis.placement !== placementKey) {
                analysis = { version, placement: placementKey, fixes: prepareQuickFixes(model.getValue(), { placement, path, documents: models === undefined ? undefined : new Map([...models].map(([path, document]) => [path, document.getValue()])) }) };
                cache.set(model, analysis);
            }
            const occurrences = migrationOnly ? [] : analysis.fixes(diagnostics.map(diagnostic => ({
                line: diagnostic.startLineNumber,
                diagnosticCode: String(typeof diagnostic.code === 'object' ? diagnostic.code.value : diagnostic.code),
            }))).filter(fix => fix.scope === 'occurrence');
            const requested = [...occurrences, ...(context.only === undefined || migrationRequested ? [DiagnosticCodes.LegacyOptionalSuffix, DiagnosticCodes.LegacyComplianceMarker].flatMap(code => analysis.fixes(undefined, code)) : [])];
            const fixes = requested.filter(fix => context.only === undefined || containsKind(context.only, fix.scope === 'document' ? migrationKind(fix.diagnosticCode) : 'quickfix'));
            if (token.isCancellationRequested || [...versions].some(([document, version]) => document.getVersionId() !== version)) return { actions: [], dispose() {} };
            const supported = fixes.filter(fix => fix.edits.every(edit => edit.path === undefined || models?.has(edit.path)));
            return {
                actions: supported.map(fix => ({
                    title: fix.title,
                    kind: fix.scope === 'document' ? migrationKind(fix.diagnosticCode) : 'quickfix',
                    isPreferred: fix.scope === 'occurrence',
                    edit: { edits: fix.edits.map(edit => {
                        const document = edit.path === undefined ? model : models!.get(edit.path)!;
                        const start = document.getPositionAt(edit.start);
                        const end = document.getPositionAt(edit.start + edit.length);
                        return { resource: document.uri, versionId: versions.get(document)!, textEdit: {
                            range: { startLineNumber: start.lineNumber, startColumn: start.column, endLineNumber: end.lineNumber, endColumn: end.column },
                            text: edit.text,
                        } };
                    }).concat(fix.diagnosticCode !== DiagnosticCodes.EventFromLaterSlice || models === undefined ? [] : [...models].filter(([path]) => !fix.edits.some(edit => edit.path === path)).map(([, document]) => ({
                        // A sibling change can invalidate the ordering proof. Empty edits pin every
                        // analyzed model, including those whose source is not changed by the repair.
                        resource: document.uri, versionId: versions.get(document)!, textEdit: { range: { startLineNumber: 1, startColumn: 1, endLineNumber: 1, endColumn: 1 }, text: '' },
                    }))) },
                })),
                dispose() {},
            };
        },
    };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { languages } from 'monaco-editor';
import { findQuickFixes } from '@cratis/screenplay-compiler';

// The compiler owns recipes and verification. The adapter only translates verified offsets and pins
// edits to the analyzed buffer version, so future fixes need no editor-specific parsing or .NET bridge.
export function createCodeActionProvider(): languages.CodeActionProvider {
    return {
        provideCodeActions(model, range, _context, token) {
            const version = model.getVersionId();
            const fixes = findQuickFixes(model.getValue(), { line: range.startLineNumber });
            if (token.isCancellationRequested || model.getVersionId() !== version) return { actions: [], dispose() {} };
            return {
                actions: fixes.map(fix => ({
                    title: fix.title,
                    kind: fix.scope === 'document' ? 'source.screenplay.migrateOptional' : 'quickfix',
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

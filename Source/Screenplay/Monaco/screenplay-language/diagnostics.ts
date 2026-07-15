// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor } from 'monaco-editor';
import { Monaco, languageId } from './language';
import { validateLines } from './validation';

const owner = 'screenplay';
const validationDelay = 300;

export function validate(monaco: Monaco, model: editor.ITextModel): editor.IMarkerData[] {
    return validateLines(model.getLinesContent()).map((issue) => ({
        severity:
            issue.severity === 'error'
                ? monaco.MarkerSeverity.Error
                : monaco.MarkerSeverity.Warning,
        message: issue.message,
        startLineNumber: issue.line + 1,
        startColumn: issue.startColumn,
        endLineNumber: issue.line + 1,
        endColumn: issue.endColumn,
    }));
}

function watch(monaco: Monaco, model: editor.ITextModel): void {
    let handle: ReturnType<typeof setTimeout> | undefined;

    const run = () => {
        if (model.isDisposed()) return;
        if (model.getLanguageId() !== languageId) {
            monaco.editor.setModelMarkers(model, owner, []);
            return;
        }
        monaco.editor.setModelMarkers(model, owner, validate(monaco, model));
    };
    const schedule = () => {
        if (handle !== undefined) clearTimeout(handle);
        handle = setTimeout(run, validationDelay);
    };

    model.onDidChangeContent(schedule);
    model.onDidChangeLanguage(run);
    model.onWillDispose(() => {
        if (handle !== undefined) clearTimeout(handle);
    });
    run();
}

export function attachDiagnostics(monaco: Monaco): void {
    monaco.editor.getModels().forEach((model) => watch(monaco, model));
    monaco.editor.onDidCreateModel((model) => watch(monaco, model));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor } from 'monaco-editor';
import { Monaco, languageId } from './language';
import { validateLines } from './validation';
import { CompletionOptions } from './completions';
import { symbolsForBuffer } from './symbols';

const owner = 'screenplay';
const validationDelay = 300;

export function validate(monaco: Monaco, model: editor.ITextModel, options: CompletionOptions = {}): editor.IMarkerData[] {
    const lines = model.getLinesContent();
    const application = symbolsForBuffer(lines, options.application?.(model));
    return validateLines(lines, { application, path: application.authoringPath, placement: application.authoringPlacement }).map((issue) => ({
        severity:
            issue.severity === 'error'
                ? monaco.MarkerSeverity.Error
                : issue.severity === 'information'
                    ? monaco.MarkerSeverity.Info
                    : monaco.MarkerSeverity.Warning,
        tags: issue.code === 'PLAY0479' ? [monaco.MarkerTag.Deprecated] : undefined,
        message: issue.message,
        code: issue.code,
        startLineNumber: issue.line + 1,
        startColumn: issue.startColumn,
        endLineNumber: issue.line + 1,
        endColumn: issue.endColumn,
    }));
}

function watch(monaco: Monaco, model: editor.ITextModel, options: CompletionOptions): void {
    let handle: ReturnType<typeof setTimeout> | undefined;

    const run = () => {
        if (model.isDisposed()) return;
        if (model.getLanguageId() !== languageId) {
            monaco.editor.setModelMarkers(model, owner, []);
            return;
        }
        monaco.editor.setModelMarkers(model, owner, validate(monaco, model, options));
    };
    const schedule = () => {
        if (handle !== undefined) clearTimeout(handle);
        handle = setTimeout(run, validationDelay);
    };

    const contextChange = options.onDidChangeApplication?.(run);
    model.onDidChangeContent(schedule);
    model.onDidChangeLanguage(run);
    model.onWillDispose(() => {
        contextChange?.dispose();
        if (handle !== undefined) clearTimeout(handle);
    });
    run();
}

export function attachDiagnostics(monaco: Monaco, options: CompletionOptions = {}): void {
    monaco.editor.getModels().forEach((model) => watch(monaco, model, options));
    monaco.editor.onDidCreateModel((model) => watch(monaco, model, options));
}

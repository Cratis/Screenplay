// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { IDisposable, editor } from 'monaco-editor';
import { Monaco, languageConfiguration, languageId, monarchLanguage } from './language';
import { CompletionProvider } from './CompletionProvider';
import { HoverProvider } from './HoverProvider';
import { Validator } from './Validator';

const markerOwner = 'cdl-validator';

// Keyed by Monaco instance rather than a single boolean — mirrors the composite
// language's own registration guard (../../index.ts) and lets a second register()
// call for the same instance hand back the existing disposer instead of doubling
// up the completion/hover providers.
const disposersByMonaco = new Map<Monaco, () => void>();

function toMarkers(monaco: Monaco, validator: Validator, model: editor.ITextModel): editor.IMarkerData[] {
    return validator.validate(model.getLinesContent()).map((issue) => ({
        severity: issue.severity === 'error' ? monaco.MarkerSeverity.Error : monaco.MarkerSeverity.Warning,
        message: issue.message,
        startLineNumber: issue.line + 1,
        startColumn: issue.startColumn,
        endLineNumber: issue.line + 1,
        endColumn: issue.endColumn,
    }));
}

export function registerCaptureLanguage(monaco: Monaco): { dispose(): void } {
    const existing = disposersByMonaco.get(monaco);
    if (existing) {
        return { dispose: existing };
    }

    monaco.languages.register({ id: languageId });
    monaco.languages.setLanguageConfiguration(languageId, languageConfiguration);
    monaco.languages.setMonarchTokensProvider(languageId, monarchLanguage);

    const validator = new Validator();
    const disposables: IDisposable[] = [];

    disposables.push(monaco.languages.registerCompletionItemProvider(languageId, new CompletionProvider()));
    disposables.push(monaco.languages.registerHoverProvider(languageId, new HoverProvider()));

    const validateModel = (model: editor.ITextModel): void => {
        if (model.isDisposed() || model.getLanguageId() !== languageId) {
            return;
        }
        monaco.editor.setModelMarkers(model, markerOwner, toMarkers(monaco, validator, model));
    };

    disposables.push(monaco.editor.onDidCreateModel((model) => {
        if (model.getLanguageId() !== languageId) {
            return;
        }
        validateModel(model);
        disposables.push(model.onDidChangeContent(() => validateModel(model)));
    }));

    monaco.editor.getModels().forEach((model) => validateModel(model));

    const dispose = (): void => {
        disposables.forEach((disposable) => disposable.dispose());
        disposables.length = 0;
        disposersByMonaco.delete(monaco);
    };

    disposersByMonaco.set(monaco, dispose);
    return { dispose };
}

export { languageId } from './language';

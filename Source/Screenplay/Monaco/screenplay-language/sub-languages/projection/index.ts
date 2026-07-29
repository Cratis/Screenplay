// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Monaco from 'monaco-editor';
import { configuration, languageId, monarchLanguage } from './language';
import { CompletionProvider } from './CompletionProvider';
import { Validator } from './Validator';
import { HoverProvider } from './HoverProvider';
import { CodeActionProvider } from './CodeActionProvider';

export interface JsonSchema {
    title?: string;
    name?: string;
    $id?: string;
    $ref?: string;
    type?: string;
    format?: string;
    description?: string;
    properties?: Record<string, JsonSchemaProperty>;
    items?: JsonSchema;
    required?: string[];
    definitions?: Record<string, JsonSchema>;
}

export interface JsonSchemaProperty {
    id?: string;
    name?: string;
    type?: string;
    format?: string;
    description?: string;
    items?: JsonSchema;
    properties?: Record<string, JsonSchemaProperty>;
    required?: boolean;
    $ref?: string;
}

export interface ReadModelInfo {
    identifier: string;
    displayName: string;
    schema: JsonSchema;
}

// Kept as its own type (rather than duplicated inline in the validator and code action
// provider, as the Chronicle original had it) since both consume the exact same shape.
export interface DraftReadModelInfo {
    identifier: string;
    displayName: string;
    containerName: string;
    schema: JsonSchema;
}

let validator: Validator | undefined;
let completionProvider: CompletionProvider | undefined;
let hoverProvider: HoverProvider | undefined;
let codeActionProvider: CodeActionProvider | undefined;
let disposables: Monaco.IDisposable[] = [];
let monacoInstance: typeof Monaco | null = null;
let isRegistered = false;
let pendingCreateReadModelCallback: ((readModelName: string) => void) | null = null;
let pendingEditReadModelCallback: ((readModelName: string, currentSchema: JsonSchema) => void) | null = null;

export function registerProjectionLanguage(monaco: typeof Monaco): { dispose(): void } {
    if (!isRegistered) {
        isRegistered = true;
        monacoInstance = monaco;

        monaco.languages.register({ id: languageId });
        monaco.languages.setLanguageConfiguration(languageId, configuration);
        monaco.languages.setMonarchTokensProvider(languageId, monarchLanguage);

        validator = new Validator();
        completionProvider = new CompletionProvider();
        hoverProvider = new HoverProvider();
        codeActionProvider = new CodeActionProvider();

        // Apply pending callbacks if they were set before initialization
        if (pendingCreateReadModelCallback) {
            codeActionProvider.setCreateReadModelCallback(pendingCreateReadModelCallback);
            pendingCreateReadModelCallback = null;
        }
        if (pendingEditReadModelCallback) {
            codeActionProvider.setEditReadModelCallback(pendingEditReadModelCallback);
            pendingEditReadModelCallback = null;
        }

        // Register completion provider with helpful trigger characters
        disposables.push(monaco.languages.registerCompletionItemProvider(languageId, {
            provideCompletionItems: completionProvider.provideCompletionItems.bind(completionProvider),
            triggerCharacters: ['.', ' ', '=', '\n', '$'],
        }));

        // Register hover provider
        disposables.push(monaco.languages.registerHoverProvider(languageId, {
            provideHover: hoverProvider.provideHover.bind(hoverProvider),
        }));

        // Register code action provider
        disposables.push(monaco.languages.registerCodeActionProvider(languageId, {
            provideCodeActions: codeActionProvider.provideCodeActions.bind(codeActionProvider),
        }, {
            providedCodeActionKinds: ['quickfix']
        }));

        // Register command for creating read models
        disposables.push(monaco.editor.registerCommand('pdl.createReadModel', (_accessor: unknown, readModelName: string) => {
            codeActionProvider?.invokeCreateReadModel(readModelName);
        }));

        // Register command for editing read models
        disposables.push(monaco.editor.registerCommand('pdl.editReadModel', (_accessor: unknown, readModelName: string, currentSchema: JsonSchema) => {
            codeActionProvider?.invokeEditReadModel(readModelName, currentSchema);
        }));

        // Register validation on model change
        disposables.push(monaco.editor.onDidCreateModel((model: Monaco.editor.ITextModel) => {
            if (model.getLanguageId() === languageId) {
                validateModel(monaco, model);
                disposables.push(model.onDidChangeContent(() => validateModel(monaco, model)));
            }
        }));

        // Validate existing models
        monaco.editor.getModels().forEach((model: Monaco.editor.ITextModel) => {
            if (model.getLanguageId() === languageId) {
                validateModel(monaco, model);
            }
        });
    }

    return { dispose };
}

function dispose(): void {
    disposables.forEach((disposable) => disposable.dispose());
    disposables = [];
    isRegistered = false;
    monacoInstance = null;
}

export function setReadModelSchema(schema: JsonSchema): void {
    // Backwards compatible single-schema setter
    validator?.setReadModelSchemas([schema]);
    completionProvider?.setReadModelSchemas([schema]);
}

export function setReadModelSchemas(readModels: ReadModelInfo[]): void {
    validator?.setReadModels(readModels);
    completionProvider?.setReadModels(readModels);
    hoverProvider?.setReadModels(readModels);
    codeActionProvider?.setReadModels(readModels);
    revalidateAllModels();
}

export function setCreateReadModelCallback(callback: (readModelName: string) => void): void {
    if (codeActionProvider) {
        codeActionProvider.setCreateReadModelCallback(callback);
    } else {
        // Store callback for later when the provider is initialized
        pendingCreateReadModelCallback = callback;
    }
}

export function setEditReadModelCallback(callback: (readModelName: string, currentSchema: JsonSchema) => void): void {
    if (codeActionProvider) {
        codeActionProvider.setEditReadModelCallback(callback);
    } else {
        // Store callback for later when the provider is initialized
        pendingEditReadModelCallback = callback;
    }
}

export function setDraftReadModel(draft: DraftReadModelInfo | null): void {
    codeActionProvider?.setDraftReadModel(draft);
    validator?.setDraftReadModel(draft);
    hoverProvider?.setDraftReadModel(draft);
    revalidateAllModels();
}

export function setEventSchemas(eventSchemas: JsonSchema[] | Record<string, JsonSchema>): void {
    // Normalize either an array of schemas or a keyed record into a record keyed by derived schema name
    const normalize = (input: JsonSchema[] | Record<string, JsonSchema>): Record<string, JsonSchema> => {
        if (!input) return {};
        if (Array.isArray(input)) {
            const out: Record<string, JsonSchema> = {};
            input.forEach((s, i) => {
                if (!s) return;
                const name = s.title || s.name || (typeof s.$id === 'string' ? s.$id.split('/').pop() : undefined) || `Event${i + 1}`;
                out[name] = s;
            });
            return out;
        }
        return input;
    };

    const normalized = normalize(eventSchemas);
    validator?.setEventSchemas(normalized);
    completionProvider?.setEventSchemas(normalized);
    hoverProvider?.setEventSchemas(normalized);
    revalidateAllModels();
}

export function setEventSequences(sequences: string[]): void {
    completionProvider?.setEventSequences(sequences);
}

function validateModel(monaco: typeof Monaco, model: Monaco.editor.ITextModel): void {
    if (validator && !model.isDisposed()) {
        const markers = validator.validate(model);
        monaco.editor.setModelMarkers(model, 'pdl-validator', markers);
    }
}

function revalidateAllModels(): void {
    if (monacoInstance) {
        monacoInstance.editor.getModels().forEach((model: Monaco.editor.ITextModel) => {
            if (model.getLanguageId() === languageId) {
                validateModel(monacoInstance!, model);
            }
        });
    }
}

export { languageId };

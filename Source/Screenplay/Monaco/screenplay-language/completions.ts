// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor, languages } from 'monaco-editor';
import { typeReferenceSymbol, typeReferenceText } from './TypeReferenceSymbol';
import { Monaco, primitiveTypes } from './language';
import { DocumentSymbols, knownEventNames, knownTriggerNames, symbolsForBuffer } from './symbols';
import { responseCompletions } from './response-completions';
import { operationCompletions } from './operation-authoring';
import { eventSourceCompletions } from './event-source-authoring';
import { planCompletions } from './completion-planner';
import { contextVariableItems, producesItems, CompletionEntry } from './completion-items';

// What a host knows beyond the one model the editor holds.
export interface CompletionOptions {
    // The paths a file import written in the model can name, relative to the model's folder - typically
    // importablePaths over the host's .play files. Without it, an import path is not completed.
    playFiles?: (model: editor.ITextModel) => readonly string[];
    application?: (model: editor.ITextModel) => DocumentSymbols;
    // Notify after changing application documents or placement, even when this buffer is unchanged.
    onDidChangeApplication?: (listener: () => void) => { dispose(): void };
}

export function createCompletionProvider(monaco: Monaco, options: CompletionOptions = {}): languages.CompletionItemProvider {
    return {
        triggerCharacters: [' ', '$', '@', '.', '"', '/'],

        provideCompletionItems(model, position) {
            const lines = model.getLinesContent();
            const lineIndex = position.lineNumber - 1;
            const currentLine = lines[lineIndex] ?? '';
            const textBefore = currentLine.substring(0, position.column - 1);
            const application = options.application?.(model);
            const symbols = symbolsForBuffer(lines, application);
            const responseEntries = eventSourceCompletions(lines, lineIndex, textBefore, symbols) ?? operationCompletions(lines, lineIndex, textBefore, symbols) ?? responseCompletions(lines, lineIndex, textBefore, symbols);
            const plan = responseEntries === null ? planCompletions(lines, lineIndex, textBefore, symbols) : { kind: 'entries' as const, entries: responseEntries };
            if (plan.kind === 'none') return { suggestions: [] };

            const word = model.getWordUntilPosition(position);
            const range = new monaco.Range(
                position.lineNumber,
                word.startColumn,
                position.lineNumber,
                word.endColumn,
            );
            const kinds = monaco.languages.CompletionItemKind;
            const asSnippet = monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet;

            const snippet = (entry: CompletionEntry): languages.CompletionItem => ({
                label: entry.label,
                kind: entry.insertText.includes('$') ? kinds.Snippet : kinds.Keyword,
                insertText: entry.insertText,
                insertTextRules: asSnippet,
                documentation: entry.documentation,
                range,
            });
            const symbolItem = (
                name: string,
                kind: languages.CompletionItemKind,
                detail: string,
            ): languages.CompletionItem => ({
                label: name,
                kind,
                insertText: name,
                detail,
                range,
            });
            const eventNames = () => {
                const inlineNames = new Set(symbols.events.filter(event => event.inline).map(event => event.name));
                return [...new Set(knownEventNames(symbols))].map((name) => symbolItem(name, kinds.Event, inlineNames.has(name) ? 'inline event' : 'event'));
            };

            switch (plan.kind) {
                case 'playFiles': {
                    const pathRange = new monaco.Range(
                        position.lineNumber,
                        position.column - plan.replaceLength,
                        position.lineNumber,
                        position.column,
                    );
                    return {
                        suggestions: (options.playFiles?.(model) ?? []).map((path) => ({
                            label: path,
                            kind: path.includes('*') ? kinds.Folder : kinds.File,
                            insertText: path,
                            range: pathRange,
                        })),
                    };
                }
                case 'contextVariables': {
                    const variableRange = new monaco.Range(
                        position.lineNumber,
                        position.column - plan.replaceLength,
                        position.lineNumber,
                        word.endColumn,
                    );
                    return {
                        suggestions: contextVariableItems.map((entry) => ({
                            ...snippet(entry),
                            range: variableRange,
                        })),
                    };
                }
                case 'policies':
                    return {
                        suggestions: symbols.policies.map((policy) =>
                            symbolItem(policy.name, kinds.Reference, 'policy'),
                        ),
                    };
                case 'events':
                    return { suggestions: eventNames() };
                case 'triggers':
                    return {
                        suggestions: [...new Set(knownTriggerNames(symbols))].map((name) =>
                            symbolItem(name, kinds.Event, 'trigger'),
                        ),
                    };
                case 'commands':
                    return {
                        suggestions: symbols.commands.map((command) =>
                            symbolItem(command.name, kinds.Method, 'command'),
                        ),
                    };
                case 'producesTargets':
                    return { suggestions: [...producesItems.map(snippet), ...eventNames()] };
                case 'screens':
                    return {
                        suggestions: symbols.screens.map((screen) =>
                            symbolItem(screen.name, kinds.Interface, 'screen'),
                        ),
                    };
                case 'queries':
                    return {
                        suggestions: symbols.queries.map((query) =>
                            symbolItem(query.name, kinds.Function, `query => ${typeReferenceText(query.returnTypeReference ?? typeReferenceSymbol(query.returnType))}`),
                        ),
                    };
                case 'types':
                    return {
                        suggestions: [
                            ...symbols.concepts.map((concept) =>
                                symbolItem(
                                    concept.name,
                                    kinds.Class,
                                    `concept : ${concept.primitive}${concept.attributes.length ? ' ' + concept.attributes.join(' ') : ''}`,
                                ),
                            ),
                            ...symbols.types.map((type) =>
                                symbolItem(
                                    type.name,
                                    kinds.Struct,
                                    `type (${type.properties.length} properties)`,
                                ),
                            ),
                            ...primitiveTypes.map((primitive) =>
                                symbolItem(primitive, kinds.Struct, 'primitive'),
                            ),
                        ],
                    };
                case 'entries':
                    return { suggestions: plan.entries.map(snippet) };
            }
        },
    };
}

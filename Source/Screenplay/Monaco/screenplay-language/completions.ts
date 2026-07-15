// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { languages } from 'monaco-editor';
import { Monaco, primitiveTypes } from './language';
import { knownEventNames, scanDocument } from './symbols';
import { planCompletions } from './completion-planner';
import { contextVariableItems, producesItems, CompletionEntry } from './completion-items';

export function createCompletionProvider(monaco: Monaco): languages.CompletionItemProvider {
    return {
        triggerCharacters: [' ', '$', '@', '.'],

        provideCompletionItems(model, position) {
            const lines = model.getLinesContent();
            const lineIndex = position.lineNumber - 1;
            const currentLine = lines[lineIndex] ?? '';
            const textBefore = currentLine.substring(0, position.column - 1);
            const plan = planCompletions(lines, lineIndex, textBefore);
            if (plan.kind === 'none') return { suggestions: [] };

            const symbols = scanDocument(lines);
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
            const eventNames = () =>
                knownEventNames(symbols).map((name) => symbolItem(name, kinds.Event, 'event'));

            switch (plan.kind) {
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
                            symbolItem(query.name, kinds.Function, `query => ${query.returnType}`),
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

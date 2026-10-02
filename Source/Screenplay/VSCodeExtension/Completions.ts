// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ApplicationIndex } from './ApplicationIndex';
import {
    CompletionEntry,
    contextVariableItems,
    knownEventNames,
    knownTriggerNames,
    languageId,
    mergeSymbols,
    planCompletions,
    primitiveTypes,
    producesItems,
    scanDocument,
    typeReferenceSymbol,
    typeReferenceText,
} from '@cratis/screenplay-language';

function snippetItem(entry: CompletionEntry): vscode.CompletionItem {
    const kind = entry.insertText.includes('$')
        ? vscode.CompletionItemKind.Snippet
        : vscode.CompletionItemKind.Keyword;
    const item = new vscode.CompletionItem(entry.label, kind);
    item.insertText = new vscode.SnippetString(entry.insertText);
    item.documentation = entry.documentation;
    return item;
}

function symbolItem(
    name: string,
    kind: vscode.CompletionItemKind,
    detail: string,
): vscode.CompletionItem {
    const item = new vscode.CompletionItem(name, kind);
    item.detail = detail;
    return item;
}

// Completes from what the whole application declares - the document and the other files of its workspace
// folder - and completes an import path from the files an import in the document can name.
const providerFor = (index: ApplicationIndex): vscode.CompletionItemProvider => ({
    provideCompletionItems(document, position) {
        const lines = document.getText().split(/\r?\n/);
        const currentLine = lines[position.line] ?? '';
        const textBefore = currentLine.substring(0, position.character);
        const plan = planCompletions(lines, position.line, textBefore);
        if (plan.kind === 'none') return [];

        const file = index.fileOf(document.uri);
        const symbols = file === undefined ? scanDocument(lines) : mergeSymbols(scanDocument(lines), file.application.symbolsExcept(file.path));
        const eventNames = () => {
            const inlineNames = new Set(symbols.events.filter(event => event.inline).map(event => event.name));
            return [...new Set(knownEventNames(symbols))].map((name) =>
                symbolItem(name, vscode.CompletionItemKind.Event, inlineNames.has(name) ? 'inline event' : 'event'),
            );
        };

        switch (plan.kind) {
            case 'playFiles': {
                const range = new vscode.Range(position.line, position.character - plan.replaceLength, position.line, position.character);
                return (file?.application.importablePathsFor(file.path) ?? []).map((path) => {
                    const item = new vscode.CompletionItem(path, path.includes('*') ? vscode.CompletionItemKind.Folder : vscode.CompletionItemKind.File);
                    item.range = range;
                    return item;
                });
            }
            case 'contextVariables':
                return contextVariableItems.map((entry) => {
                    const item = snippetItem(entry);
                    item.range = new vscode.Range(
                        position.line,
                        position.character - plan.replaceLength,
                        position.line,
                        position.character,
                    );
                    return item;
                });
            case 'policies':
                return symbols.policies.map((policy) =>
                    symbolItem(policy.name, vscode.CompletionItemKind.Reference, 'policy'),
                );
            case 'events':
                return eventNames();
            case 'triggers':
                return [...new Set(knownTriggerNames(symbols))].map((name) =>
                    symbolItem(name, vscode.CompletionItemKind.Event, 'trigger'),
                );
            case 'commands':
                return symbols.commands.map((command) =>
                    symbolItem(command.name, vscode.CompletionItemKind.Method, 'command'),
                );
            case 'producesTargets':
                return [...producesItems.map(snippetItem), ...eventNames()];
            case 'screens':
                return symbols.screens.map((screen) =>
                    symbolItem(screen.name, vscode.CompletionItemKind.Interface, 'screen'),
                );
            case 'queries':
                return symbols.queries.map((query) =>
                    symbolItem(
                        query.name,
                        vscode.CompletionItemKind.Function,
                        `query => ${typeReferenceText(query.returnTypeReference ?? typeReferenceSymbol(query.returnType))}`,
                    ),
                );
            case 'types':
                return [
                    ...symbols.concepts.map((concept) =>
                        symbolItem(
                            concept.name,
                            vscode.CompletionItemKind.Class,
                            `concept : ${concept.primitive}${concept.attributes.length ? ' ' + concept.attributes.join(' ') : ''}`,
                        ),
                    ),
                    ...symbols.types.map((type) =>
                        symbolItem(
                            type.name,
                            vscode.CompletionItemKind.Struct,
                            `type (${type.properties.length} properties)`,
                        ),
                    ),
                    ...primitiveTypes.map((primitive) =>
                        symbolItem(primitive, vscode.CompletionItemKind.Struct, 'primitive'),
                    ),
                ];
            case 'entries':
                return plan.entries.map(snippetItem);
        }
    },
});

export function registerCompletions(context: vscode.ExtensionContext, index: ApplicationIndex): void {
    context.subscriptions.push(
        vscode.languages.registerCompletionItemProvider(languageId, providerFor(index), ' ', '$', '@', '.', '"', '/'),
    );
}

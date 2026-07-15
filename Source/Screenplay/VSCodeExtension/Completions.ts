// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import {
    CompletionEntry,
    contextVariableItems,
    knownEventNames,
    languageId,
    planCompletions,
    primitiveTypes,
    producesItems,
    scanDocument,
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

const provider: vscode.CompletionItemProvider = {
    provideCompletionItems(document, position) {
        const lines = document.getText().split(/\r?\n/);
        const currentLine = lines[position.line] ?? '';
        const textBefore = currentLine.substring(0, position.character);
        const plan = planCompletions(lines, position.line, textBefore);
        if (plan.kind === 'none') return [];

        const symbols = scanDocument(lines);
        const eventNames = () =>
            knownEventNames(symbols).map((name) =>
                symbolItem(name, vscode.CompletionItemKind.Event, 'event'),
            );

        switch (plan.kind) {
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
                        `query => ${query.returnType}`,
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
                    ...primitiveTypes.map((primitive) =>
                        symbolItem(primitive, vscode.CompletionItemKind.Struct, 'primitive'),
                    ),
                ];
            case 'entries':
                return plan.entries.map(snippetItem);
        }
    },
};

export function registerCompletions(context: vscode.ExtensionContext): void {
    context.subscriptions.push(
        vscode.languages.registerCompletionItemProvider(languageId, provider, ' ', '$', '@', '.'),
    );
}

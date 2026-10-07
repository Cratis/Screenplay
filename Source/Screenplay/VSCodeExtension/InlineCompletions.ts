// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ApplicationIndex } from './ApplicationIndex';
import { languageId, structureCompletion, symbolsForBuffer } from '@cratis/screenplay-language';

// Ghost text for the structure a block obviously needs next, from the compiler's own model - the
// production of a command, the skeleton of a specification, the fields of a form.
export function registerInlineCompletions(context: vscode.ExtensionContext, index: ApplicationIndex): void {
    context.subscriptions.push(
        vscode.languages.registerInlineCompletionItemProvider(languageId, {
            provideInlineCompletionItems(document, position) {
                const lines = document.getText().split(/\r?\n/);
                const line = lines[position.line] ?? '';
                const file = index.fileOf(document.uri);
                const symbols = symbolsForBuffer(lines, file?.application.symbolsExcept(file.path));
                const suggestion = structureCompletion(lines, position.line, line.substring(0, position.character), line.substring(position.character), symbols);
                return suggestion ? [new vscode.InlineCompletionItem(suggestion.text, new vscode.Range(position, position))] : [];
            },
        }),
    );
}

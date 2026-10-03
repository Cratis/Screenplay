// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { languageId, responseTokens, responseTokenTypes } from '@cratis/screenplay-language';

export function registerResponseTokens(context: vscode.ExtensionContext): void {
    const legend = new vscode.SemanticTokensLegend([...responseTokenTypes]);
    context.subscriptions.push(vscode.languages.registerDocumentSemanticTokensProvider(languageId, {
        provideDocumentSemanticTokens(document) {
            const builder = new vscode.SemanticTokensBuilder(legend);
            for (const token of responseTokens(document.getText().split(/\r?\n/))) {
                builder.push(token.line, token.column, token.length, token.type, 0);
            }
            return builder.build();
        },
    }, legend));
}

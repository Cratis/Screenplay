// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ApplicationIndex } from './ApplicationIndex';
import { hoverContent, languageId } from '@cratis/screenplay-language';

export function registerHover(context: vscode.ExtensionContext, index?: ApplicationIndex): void {
    context.subscriptions.push(
        vscode.languages.registerHoverProvider(languageId, {
            provideHover(document, position) {
                const wordRange = document.getWordRangeAtPosition(position);
                if (!wordRange) return null;

                const file = index?.fileOf(document.uri);
                const content = hoverContent(
                    document.getText().split(/\r?\n/),
                    position.line,
                    document.getText(wordRange),
                    wordRange.start.character + 1,
                    wordRange.end.character + 1,
                    file?.application.symbolsExcept(file.path),
                );
                return content ? new vscode.Hover(new vscode.MarkdownString(content)) : null;
            },
        }),
    );
}

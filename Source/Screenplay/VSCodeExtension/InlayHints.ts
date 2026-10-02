// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { destinationHints, languageId } from '@cratis/screenplay-language';

export const inlayHintsProvider: vscode.InlayHintsProvider = {
    provideInlayHints(document, range) {
        return destinationHints(document.getText().split(/\r?\n/)).filter(hint => hint.line >= range.start.line && hint.line <= range.end.line)
            .map(hint => {
                const result = new vscode.InlayHint(new vscode.Position(hint.line, hint.column - 1), hint.label);
                result.paddingLeft = true;
                return result;
            });
    },
};

export function registerInlayHints(context: vscode.ExtensionContext): void {
    context.subscriptions.push(vscode.languages.registerInlayHintsProvider(languageId, inlayHintsProvider));
}

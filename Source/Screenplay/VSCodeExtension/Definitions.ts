// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { languageId, scanDocument } from '@cratis/screenplay-language';
import { ApplicationIndex } from './ApplicationIndex';

export function registerDefinitions(context: vscode.ExtensionContext, index: ApplicationIndex): void {
    context.subscriptions.push(vscode.languages.registerDefinitionProvider(languageId, {
        provideDefinition(document, position) {
            const range = document.getWordRangeAtPosition(position);
            if (range === undefined) return [];
            const name = document.getText(range);
            if (index.fileOf(document.uri) !== undefined) return index.eventDefinitions(document.uri, name);
            return scanDocument(document.getText().split(/\r?\n/)).events.filter(event => event.name === name)
                .map(event => new vscode.Location(document.uri, new vscode.Position(event.line, 0)));
        },
    }));
}

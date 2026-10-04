// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { analyzeOperations, languageId, scanDocument } from '@cratis/screenplay-language';
import { ApplicationIndex } from './ApplicationIndex';

export function registerDefinitions(context: vscode.ExtensionContext, index: ApplicationIndex): void {
    context.subscriptions.push(vscode.languages.registerDefinitionProvider(languageId, {
        provideDefinition(document, position) {
            const range = document.getWordRangeAtPosition(position);
            if (range === undefined) return [];
            const name = document.getText(range);
            const file = index.fileOf(document.uri);
            const lines = document.getText().split(/\r?\n/);
            const operations = analyzeOperations(lines, file?.application.symbolsExcept(file.path));
            const reference = operations.references.find(reference => reference.location.line === position.line + 1 &&
                reference.name.split('.').includes(name));
            const use = operations.declarations.find(operation => operation.usesLocation?.line === position.line + 1 && operation.uses === name);
            const target = reference?.declaration?.location ?? (use && operations.systems.filter(system => system.name === use.uses).length === 1 ? operations.systems.find(system => system.name === use.uses)?.location : undefined);
            if (reference || use) {
                if (!target) return [];
                const folder = vscode.workspace.getWorkspaceFolder(document.uri);
                const uri = target.path === 'current.play' || target.path === file?.path ? document.uri : folder && target.path ? vscode.Uri.joinPath(folder.uri, target.path) : undefined;
                return uri ? [new vscode.Location(uri, new vscode.Position(target.line - 1, target.column - 1))] : [];
            }
            if (file !== undefined) return index.eventDefinitions(document.uri, name);
            return scanDocument(document.getText().split(/\r?\n/)).events.filter(event => event.name === name)
                .map(event => new vscode.Location(document.uri, new vscode.Position(event.line, 0)));
        },
    }));
}

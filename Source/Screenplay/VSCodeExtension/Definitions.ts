// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { reactionReferenceAt, eventSourceIdentifier, eventSourceReferenceAt, analyzeOperations, fenceMap, languageId, operationReferenceAt, scanDocument, withoutComment } from '@cratis/screenplay-language';
import { ApplicationIndex } from './ApplicationIndex';

export function registerDefinitions(context: vscode.ExtensionContext, index: ApplicationIndex): void {
    context.subscriptions.push(vscode.languages.registerDefinitionProvider(languageId, {
        provideDefinition(document, position) {
            const range = document.getWordRangeAtPosition(position);
            if (range === undefined) return [];
            const name = document.getText(range);
            const file = index.fileOf(document.uri);
            const lines = document.getText().split(/\r?\n/);
            if (fenceMap(lines)[position.line] || range.end.character > withoutComment(lines[position.line]).length) return [];
            const symbols = file?.application.symbolsExcept(file.path);
            const reaction = reactionReferenceAt(lines, position.line, range.start.character + 1, range.end.character + 1, symbols);
            const sourceReference = eventSourceReferenceAt(lines, position.line, range.start.character + 1, range.end.character + 1, symbols);
            if (reaction || sourceReference) {
                const target = reaction ? reaction.target : sourceReference?.target;
                if (!target) return [];
                const path = target.location.path;
                const source = path === (file?.path ?? 'current.play') ? document.getText() : symbols?.authoringDocuments?.find(document => document.path === path)?.source;
                const location = reaction ? reaction.target?.location : source && eventSourceIdentifier(target.location, target.name, source);
                const folder = vscode.workspace.getWorkspaceFolder(document.uri);
                const uri = path === (file?.path ?? 'current.play') ? document.uri : folder && path ? vscode.Uri.joinPath(folder.uri, path) : undefined;
                return uri && location ? [new vscode.Location(uri, new vscode.Range(location.line - 1, location.column - 1, location.line - 1, location.column - 1 + target.name.length))] : [];
            }
            const operations = analyzeOperations(lines, symbols);
            const reference = operationReferenceAt(operations, lines, position.line, range.start.character + 1, range.end.character + 1);
            const use = operations.declarations.find(operation => operation.usesLocation?.path === (file?.path ?? 'current.play') &&
                operation.usesLocation.line === position.line + 1 && operation.uses === name &&
                operation.usesLocation.column === range.start.character + 1 && range.end.character - range.start.character === operation.uses.length);
            const target = reference?.declaration?.location ?? (use && operations.systems.filter(system => system.name === use.uses).length === 1 ? operations.systems.find(system => system.name === use.uses)?.location : undefined);
            if ((reference && reference.kind !== 'event') || use) {
                if (!target) return [];
                const folder = vscode.workspace.getWorkspaceFolder(document.uri);
                const uri = target.path === 'current.play' || target.path === file?.path ? document.uri : folder && target.path ? vscode.Uri.joinPath(folder.uri, target.path) : undefined;
                return uri ? [new vscode.Location(uri, new vscode.Position(target.line - 1, target.column - 1))] : [];
            }
            const eventName = reference?.name.split('.').at(-1) ?? name;
            if (file !== undefined) return index.eventDefinitions(document.uri, eventName);
            return scanDocument(lines).events.filter(event => event.name === eventName)
                .map(event => new vscode.Location(document.uri, new vscode.Position(event.line, 0)));
        },
    }));
}

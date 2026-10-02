// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'node:path';
import * as vscode from 'vscode';
import { ApplicationSyntax, CompilationResult, compileApplication, parse, parseFolder, PlayFileSource } from '@cratis/screenplay-compiler';
import { fileImports } from '@cratis/screenplay-language';
import { findApplicationRoot } from './ApplicationRoot';

// What the board compiles for a document: the application it belongs to, under the name the board shows.
export interface BoardCompilation {
    readonly name: string;
    readonly root?: vscode.Uri;
    readonly result: CompilationResult<ApplicationSyntax>;
}

// Compiles the application a document belongs to. Inside a folder application that is every .play file of
// the folder, merged - so the board shows the whole model, not one file's share of it. Text that is open
// and unsaved is what is compiled, for the document itself and for every other file of the folder. A document
// of its own that imports files is the root of the application its imports make up.
export async function compileForBoard(document: vscode.TextDocument): Promise<BoardCompilation> {
    const workspaceFolder = vscode.workspace.getWorkspaceFolder(document.uri)?.uri;
    const root = document.uri.scheme === 'file'
        ? await findApplicationRoot(document.uri.fsPath, workspaceFolder?.fsPath, exists)
        : undefined;
    if (root === undefined) {
        const name = nameOf(document.uri.fsPath);
        if (document.uri.scheme !== 'file' || fileImports(document.getText().split(/\r?\n/)).length === 0) {
            return { name, result: parse(document.getText()) };
        }
        // Imports are relative to the document's folder and may climb out of it, so every file of the workspace
        // folder is named relative to the document's folder.
        const folder = vscode.Uri.file(path.dirname(document.uri.fsPath));
        const documents = new Map((await sourcesBeneath(workspaceFolder ?? folder, folder)).map(source => [source.path, source.source]));
        documents.set(path.basename(document.uri.fsPath), document.getText());
        return { name, result: compileApplication(documents, [path.basename(document.uri.fsPath)]) };
    }
    const rootUri = vscode.Uri.file(root);
    return { name: path.basename(root), root: rootUri, result: parseFolder(await sourcesBeneath(rootUri, rootUri)) };
}

async function sourcesBeneath(folder: vscode.Uri, relativeTo: vscode.Uri): Promise<PlayFileSource[]> {
    const files = await vscode.workspace.findFiles(new vscode.RelativePattern(folder, '**/*.play'), '**/node_modules/**');
    return Promise.all(files.map(file => read(file, relativeTo)));
}

async function read(file: vscode.Uri, root: vscode.Uri): Promise<PlayFileSource> {
    const open = vscode.workspace.textDocuments.find(document => document.uri.toString() === file.toString());
    const source = open?.getText() ?? new TextDecoder().decode(await vscode.workspace.fs.readFile(file));
    return { path: path.relative(root.fsPath, file.fsPath).split(path.sep).join('/'), source };
}

async function exists(filePath: string): Promise<boolean> {
    try {
        await vscode.workspace.fs.stat(vscode.Uri.file(filePath));
        return true;
    } catch {
        return false;
    }
}

function nameOf(filePath: string): string {
    return path.basename(filePath, '.play');
}

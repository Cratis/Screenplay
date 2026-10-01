// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'node:path';
import * as vscode from 'vscode';
import { ApplicationSyntax, CompilationResult, parse, parseFolder, PlayFileSource } from '@cratis/screenplay-compiler';
import { findApplicationRoot } from './ApplicationRoot';

// What the board compiles for a document: the application it belongs to, under the name the board shows.
export interface BoardCompilation {
    readonly name: string;
    readonly root?: vscode.Uri;
    readonly result: CompilationResult<ApplicationSyntax>;
}

// Compiles the application a document belongs to. Inside a folder application that is every .play file of
// the folder, merged - so the board shows the whole model, not one file's share of it. Text that is open
// and unsaved is what is compiled, for the document itself and for every other file of the folder.
export async function compileForBoard(document: vscode.TextDocument): Promise<BoardCompilation> {
    const root = document.uri.scheme === 'file'
        ? await findApplicationRoot(document.uri.fsPath, vscode.workspace.getWorkspaceFolder(document.uri)?.uri.fsPath, exists)
        : undefined;
    if (root === undefined) {
        return { name: nameOf(document.uri.fsPath), result: parse(document.getText()) };
    }
    const rootUri = vscode.Uri.file(root);
    const files = await vscode.workspace.findFiles(new vscode.RelativePattern(rootUri, '**/*.play'), '**/node_modules/**');
    const sources = await Promise.all(files.map(file => read(file, rootUri)));
    return { name: path.basename(root), root: rootUri, result: parseFolder(sources) };
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

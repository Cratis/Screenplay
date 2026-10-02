// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'node:path';
import * as vscode from 'vscode';
import { ApplicationSyntax, CompilationResult, compileApplication, parse, parseFolder, PlayFileSource } from '@cratis/screenplay-compiler';
import { fileImports } from '@cratis/screenplay-language';
import { applicationFileName, findApplicationRoot } from './ApplicationRoot';
import { filesOf, narrowTo, scopeOf } from './boardScope';

// What the board compiles for a document: the application it belongs to, under the name the board shows.
export interface BoardCompilation {
    readonly name: string;
    readonly root?: vscode.Uri;
    readonly result: CompilationResult<ApplicationSyntax>;
}

// Compiles the application a document belongs to and draws the document's share of it. Inside a folder
// application that is every .play file of the folder, compiled together so every slice is drawn against what the
// whole application declares - but the board shows what the document stands for: its own slices, the slices of
// the files it imports, and for a module or feature file the slices placed in what it declares. The folder's
// application.play stands for the whole application, and so does a file with no slice to show, such as one that
// only declares concepts or types. Text that is open and unsaved is what is compiled, for the document itself
// and for every other file of the folder. A document of its own that imports files is the root of the
// application its imports make up.
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
    const sources = await sourcesBeneath(rootUri, rootUri);
    const application = parseFolder(sources);
    const whole = { name: path.basename(root), root: rootUri, result: application };
    const documentPath = path.relative(root, document.uri.fsPath).split(path.sep).join('/');
    if (documentPath === applicationFileName) {
        return whole;
    }
    const narrowed = narrowTo(application.value, scopeOf(documentPath, new Map(sources.map(source => [source.path, source.source])), application));
    if (narrowed === undefined) {
        return whole;
    }
    const files = filesOf(narrowed);
    files.add(documentPath);
    const diagnostics = application.diagnostics.filter(diagnostic => diagnostic.location.path === undefined || files.has(diagnostic.location.path));
    return {
        name: nameOf(document.uri.fsPath),
        root: rootUri,
        result: { value: narrowed, diagnostics, success: !diagnostics.some(diagnostic => diagnostic.severity === 'error') },
    };
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

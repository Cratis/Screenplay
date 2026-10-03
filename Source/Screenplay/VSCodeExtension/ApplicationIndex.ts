// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'node:path';
import * as vscode from 'vscode';
import { languageId } from '@cratis/screenplay-language';
import { WorkspaceApplication } from './WorkspaceApplication';

// How long edits settle before the files that may depend on them are validated again.
const settleDelay = 300;

const playFiles = '**/*.play';
const excluded = '**/node_modules/**';

// A file of an application: the application, and the file's portable path within its workspace folder.
export interface ApplicationFile {
    readonly application: WorkspaceApplication;
    readonly path: string;
}

// Keeps the .play files of every workspace folder as one application per folder - what is open and unsaved
// is what is held - and says when any of them changed, once the edits have settled.
export class ApplicationIndex implements vscode.Disposable {
    readonly #applications = new Map<string, WorkspaceApplication>();
    readonly #changed = new vscode.EventEmitter<void>();
    readonly #subscriptions: vscode.Disposable[] = [];
    #pending: ReturnType<typeof setTimeout> | undefined;

    readonly onDidChange = this.#changed.event;

    constructor() {
        const watcher = vscode.workspace.createFileSystemWatcher(playFiles);
        this.#subscriptions.push(
            this.#changed,
            watcher,
            watcher.onDidCreate(uri => this.#readFromDisk(uri)),
            watcher.onDidChange(uri => this.#readFromDisk(uri)),
            watcher.onDidDelete(uri => this.#remove(uri)),
            vscode.workspace.onDidChangeTextDocument(event => this.#hold(event.document)),
            vscode.workspace.onDidOpenTextDocument(document => this.#hold(document)),
            vscode.workspace.onDidCloseTextDocument(document => this.#readFromDisk(document.uri)),
            vscode.workspace.onDidChangeWorkspaceFolders(() => void this.load()),
        );
    }

    // Reads every .play file of every workspace folder.
    async load(): Promise<void> {
        this.#applications.clear();
        for (const folder of vscode.workspace.workspaceFolders ?? []) {
            const application = new WorkspaceApplication();
            this.#applications.set(folder.uri.fsPath, application);
            const files = await vscode.workspace.findFiles(new vscode.RelativePattern(folder, playFiles), excluded);
            const texts = await Promise.all(files.map(file => textOf(file)));
            files.forEach((file, index) => application.set(relativePath(folder.uri.fsPath, file.fsPath), texts[index]));
        }
        this.#changed.fire();
    }

    // The application a document belongs to and its path in it - undefined outside every workspace folder.
    fileOf(uri: vscode.Uri): ApplicationFile | undefined {
        const folder = uri.scheme === 'file' ? vscode.workspace.getWorkspaceFolder(uri) : undefined;
        const application = folder === undefined ? undefined : this.#applications.get(folder.uri.fsPath);
        return application === undefined || folder === undefined ? undefined : { application, path: relativePath(folder.uri.fsPath, uri.fsPath) };
    }

    eventDefinitions(uri: vscode.Uri, name: string): vscode.Location[] {
        const folder = vscode.workspace.getWorkspaceFolder(uri);
        const file = this.fileOf(uri);
        if (folder === undefined || file === undefined) return [];
        return file.application.eventDeclarations(name).map(declaration => new vscode.Location(
            vscode.Uri.joinPath(folder.uri, declaration.path), new vscode.Position(declaration.line, 0),
        ));
    }

    dispose(): void {
        if (this.#pending !== undefined) clearTimeout(this.#pending);
        this.#subscriptions.forEach(subscription => subscription.dispose());
    }

    #hold(document: vscode.TextDocument): void {
        if (document.languageId !== languageId) return;
        const file = this.fileOf(document.uri);
        if (file?.application.set(file.path, document.getText())) this.#settle();
    }

    async #readFromDisk(uri: vscode.Uri): Promise<void> {
        const file = this.fileOf(uri);
        if (file === undefined || !uri.fsPath.endsWith('.play') || isExcluded(uri)) return;
        try {
            if (file.application.set(file.path, await textOf(uri))) this.#settle();
        } catch {
            this.#remove(uri);
        }
    }

    #remove(uri: vscode.Uri): void {
        const file = this.fileOf(uri);
        if (file?.application.delete(file.path)) this.#settle();
    }

    #settle(): void {
        if (this.#pending !== undefined) clearTimeout(this.#pending);
        this.#pending = setTimeout(() => {
            this.#pending = undefined;
            this.#changed.fire();
        }, settleDelay);
    }
}

// What is open is what counts, saved or not.
async function textOf(uri: vscode.Uri): Promise<string> {
    const open = vscode.workspace.textDocuments.find(document => document.uri.toString() === uri.toString());
    return open?.getText() ?? new TextDecoder().decode(await vscode.workspace.fs.readFile(uri));
}

function relativePath(folder: string, file: string): string {
    return path.relative(folder, file).split(path.sep).join('/');
}

// The watcher sees every .play file; the ones the initial search leaves out stay out.
function isExcluded(uri: vscode.Uri): boolean {
    return uri.fsPath.split(/[\\/]/).includes('node_modules');
}

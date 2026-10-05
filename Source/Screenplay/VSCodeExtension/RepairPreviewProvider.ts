// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { createHash, randomUUID } from 'node:crypto';
import { RepairFailure } from './RepairClient';
import { RepairPreview } from './RepairSession';

export class RepairPreviewProvider implements vscode.FileSystemProvider, vscode.Disposable {
    readonly #changed = new vscode.EventEmitter<vscode.FileChangeEvent[]>();
    readonly onDidChangeFile = this.#changed.event;
    watch(): vscode.Disposable { return { dispose() {} }; }
    stat(uri: vscode.Uri): vscode.FileStat {
        const bytes = this.readFile(uri);
        return { type: vscode.FileType.File, ctime: 0, mtime: 0, size: bytes.length, permissions: vscode.FilePermission.Readonly };
    }
    readFile(uri: vscode.Uri): Uint8Array { return Buffer.from(this.provideTextDocumentContent(uri), 'utf8'); }
    readDirectory(): [string, vscode.FileType][] { return []; }
    createDirectory(): never { throw vscode.FileSystemError.NoPermissions('Repair previews are read-only.'); }
    writeFile(): never { throw vscode.FileSystemError.NoPermissions('Repair previews are read-only.'); }
    delete(): never { throw vscode.FileSystemError.NoPermissions('Repair previews are read-only.'); }
    rename(): never { throw vscode.FileSystemError.NoPermissions('Repair previews are read-only.'); }
    static readonly scheme = 'screenplay-repair';
    readonly #documents = new Map<string, string>();
    readonly #inspections = new Map<string, string>();
    #token?: string;
    #preview?: RepairPreview;
    #generation = 0;
    #disposed = false;
    constructor(readonly scheme = RepairPreviewProvider.scheme) {}
    get token(): string | undefined { return this.#token; }
    review(token: string): RepairPreview {
        if (this.#token !== token || !this.#preview) throw new RepairFailure('PreviewExpired', 'Review expired.');
        // VS Code can accept a programmatic WorkspaceEdit even on a read-only model.
        // Such an edit must never count as the exact byte review issued by the server.
        for (const [uri, content] of this.#documents) {
            const document = vscode.workspace.textDocuments.find(document => document.uri.toString() === uri);
            if (!document || document.isDirty || document.getText() !== content.replace(/^\uFEFF/, '')) {
                this.clear();
                throw new RepairFailure('PreviewModified', 'A preview buffer was changed or closed. Rediscover and review the exact bytes.');
            }
        }
        return this.#preview;
    }

    provideTextDocumentContent(uri: vscode.Uri): string {
        const content = this.#documents.get(uri.toString()) ?? this.#inspections.get(uri.toString());
        if (content === undefined) throw new RepairFailure('PreviewExpired', 'This repair preview has expired. Rediscover and review it.');
        return content;
    }
    clear(): void { ++this.#generation; this.#documents.clear(); this.#token = undefined; this.#preview = undefined; }
    dispose(): void { if (this.#disposed) return; this.#disposed = true; this.clear(); this.#inspections.clear(); this.#changed.dispose(); }
    closed(uri: vscode.Uri): boolean {
        if (this.#inspections.delete(uri.toString())) return false; // Inspection has no review authority.
        if (!this.#documents.has(uri.toString())) return false;
        this.clear();
        return true;
    }
    async showFailure(kind: string, details: unknown, relevant: () => boolean = () => true): Promise<void> {
        if (this.#disposed || !relevant()) return;
        this.clear();
        const generation = this.#generation;
        const content = JSON.stringify({ failureKind: kind, details, note: 'This is a refused or uncertain operation, not an accepted proposal or runtime confirmation. No Apply authority is issued. Inspect recovery separately if Apply was dispatched.' }, null, 2);
        if (Buffer.byteLength(content, 'utf8') > 16 * 1024 * 1024) throw new RepairFailure('PreviewTooLarge', 'Conflict details exceed the read-only review budget.');
        const uri = vscode.Uri.from({ scheme: this.scheme, path: `/${randomUUID()}/failure.json` });
        this.#documents.set(uri.toString(), content);
        const document = await vscode.workspace.openTextDocument(uri);
        if (!this.#disposed && relevant() && generation === this.#generation) await vscode.window.showTextDocument(document, { preview: false });
    }
    async showInspection(state: unknown, relevant: () => boolean = () => true): Promise<boolean> {
        if (this.#disposed || !relevant()) return false;
        const content = JSON.stringify(state, null, 2);
        if (Buffer.byteLength(content, 'utf8') > 16 * 1024 * 1024) throw new RepairFailure('PreviewTooLarge', 'Inspection exceeds the read-only review budget.');
        const uri = vscode.Uri.from({ scheme: this.scheme, path: `/${randomUUID()}/inspection.json` });
        this.#inspections.clear(); // Bound retained inspection bytes independently of active review.
        this.#inspections.set(uri.toString(), content);
        const document = await vscode.workspace.openTextDocument(uri);
        const current = () => !this.#disposed && relevant() && this.#inspections.has(uri.toString());
        if (!current()) return false;
        await vscode.window.showTextDocument(document, { preview: false });
        return current();
    }
    async show(preview: RepairPreview, authorize: () => void = () => {}): Promise<void> {
        if (this.#disposed) throw new RepairFailure('PreviewExpired', 'Repair previews were disposed.');
        authorize();
        this.clear();
        const generation = this.#generation;
        const check = () => { authorize(); if (this.#disposed || generation !== this.#generation) throw new RepairFailure('PreviewExpired', 'Workspace changed or preview closed during review.'); };
        const add = (name: string, content: string) => {
            check();
            const uri = vscode.Uri.from({ scheme: this.scheme, path: `/${preview.token}/${name}` });
            this.#documents.set(uri.toString(), content);
            return uri;
        };
        const details = (bytes: Buffer | null) => bytes === null ? 'absent' : `${bytes.length} bytes; SHA-256 ${createHash('sha256').update(bytes).digest('hex')}; BOM ${bytes.subarray(0, 3).equals(Buffer.from([239, 187, 191])) ? 'yes' : 'no'}; CRLF ${bytes.includes(Buffer.from('\r\n')) ? 'yes' : 'no'}`;
        for (let index = 0; index < preview.files.length; index++) {
            const file = preview.files[index];
            const before = add(`${index}/before/${file.path}`, file.before?.toString('utf8') ?? '');
            const after = add(`${index}/after/${file.path}`, file.after?.toString('utf8') ?? '');
            await vscode.commands.executeCommand('vscode.diff', before, after, `${preview.title}: ${file.path}`, { preview: false });
            check();
        }
        const summary = [
            `# ${preview.title}`, '',
            preview.code === 'PLAY0478' ? '**Routing change:** events will explicitly target the command identifier. This is not a cleanup.' : 'Declare the produced event only where the C# compiler has proved the repair eligible.', '',
            '**Whole-document formatting:** all touched sources are canonically reprinted. Inspect every source diff and the identity-state diff.',
            '**Apply writes outside the editor.** It is not normal editor Undo. Use an exclusive writer; journaled rollback does not provide crash-atomic visibility across files.',
            'No AI, build, implementation execution or runtime confirmation is performed.', '',
            `Executable readiness (compiler subset only): **${preview.executableReady ? 'ready' : 'not ready'}**. Authoring acceptance does not mean runtime confirmation.`, '',
            '## Root and retained proposal', '```json', JSON.stringify(preview.binding ?? {}, null, 2), '```', '',
            '## Exact byte review',
            ...preview.files.flatMap(file => [`### ${file.path}`, `Before: ${details(file.before)}`, `After: ${details(file.after)}`, '']),
            '## Authoring diagnostics', '```json', JSON.stringify(preview.authoring, null, 2), '```', '',
            '## Executable diagnostics', '```json', JSON.stringify(preview.executable, null, 2), '```', '',
            '## Dropped comments', '```json', JSON.stringify(preview.droppedComments, null, 2), '```', '',
            'Navigate and scroll every diff before choosing Screenplay: Apply Reviewed C# Repair in the command palette, editor title or status bar. Only that explicit command opens final Apply confirmation. Screenplay: Discard C# Repair releases this review. Dismissing the nonmodal notice does neither.',
            'Apply is offered only after all source and identity byte pages have been collected. Closing a preview or changing the workspace invalidates it.',
        ].join('\n');
        const document = await vscode.workspace.openTextDocument(add('review.md', summary));
        check();
        await vscode.window.showTextDocument(document, { preview: false });
        check();
        this.#preview = preview;
        this.#token = preview.token;
    }
}

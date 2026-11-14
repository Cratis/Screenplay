// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it, vi } from 'vitest';
import * as vscode from 'vscode';
import { compileForBoard, BoardCompilation } from '../boardSources';

vi.mock('vscode', async importOriginal => {
    const original = await importOriginal<typeof import('vscode')>();
    return {
        ...original,
        RelativePattern: class { constructor(readonly base: unknown, readonly pattern: string) {} },
        workspace: {
            ...original.workspace,
            getWorkspaceFolder: () => ({ uri: original.Uri.file('/work') }),
            textDocuments: [],
            findFiles: vi.fn(),
            fs: { ...original.workspace.fs, stat: vi.fn(), readFile: vi.fn() },
        },
    };
});

const texts = new Map([
    ['/work/sub/main.play', 'import "../shared/types.play"'],
    ['/work/shared/types.play', 'concept Name string'],
    ['/work/other.play', 'concept Unrelated string'],
]);

describe('when compiling an importing standalone document beside unrelated workspace files', () => {
    let compilation: BoardCompilation;
    beforeEach(async () => {
        vi.mocked(vscode.workspace.fs.stat).mockRejectedValue(new Error('not found'));
        vi.mocked(vscode.workspace.findFiles).mockResolvedValue([...texts.keys()].map(path => vscode.Uri.file(path)));
        vi.mocked(vscode.workspace.fs.readFile).mockImplementation(async uri => new TextEncoder().encode(texts.get(uri.fsPath)!));
        const document = { uri: Object.assign(vscode.Uri.file('/work/sub/main.play'), { scheme: 'file' }), getText: () => texts.get('/work/sub/main.play')! };
        compilation = await compileForBoard(document as unknown as vscode.TextDocument);
    });

    it('should allow the document itself', () => compilation.files.should.include('main.play'));
    it('should allow an imported file that climbs out of the folder', () => compilation.files.should.include('../shared/types.play'));
    it('should not allow a workspace file that is not imported', () => compilation.files.should.not.include('../other.play'));
});

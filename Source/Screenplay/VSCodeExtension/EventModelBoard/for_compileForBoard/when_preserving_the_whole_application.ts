// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it, vi } from 'vitest';
import * as vscode from 'vscode';
import { compileForBoard } from '../boardSources';
import { boardFor } from '../compileBoard';

vi.mock('vscode', async importOriginal => {
    const original = await importOriginal<typeof import('vscode')>();
    return {
        ...original,
        RelativePattern: class { constructor(readonly base: unknown, readonly pattern: string) {} },
        workspace: {
            ...original.workspace,
            textDocuments: [],
            findFiles: vi.fn(),
            fs: { ...original.workspace.fs, stat: vi.fn(), readFile: vi.fn() },
        },
    };
});

const texts = new Map([
    ['/model/application.play', 'import "*.play"'],
    ['/model/facts.play', 'module Facts\n  feature Record\n    slice StateChange RecordFact\n      event FactRecorded'],
    ['/model/views.play', 'module Views\n  feature Display\n    slice StateView ShowFacts\n      readmodel Facts\n      projection Facts\n        from FactRecorded'],
]);
const documentAt = (path: string) => ({ uri: Object.assign(vscode.Uri.file(path), { scheme: 'file' }), getText: () => texts.get(path)! }) as unknown as vscode.TextDocument;

describe('when compiling a narrowed file board with a whole-application map', () => {
    let board: ReturnType<typeof boardFor>;
    beforeEach(async () => {
        vi.mocked(vscode.workspace.findFiles).mockResolvedValue([...texts.keys()].map(path => vscode.Uri.file(path)));
        vi.mocked(vscode.workspace.fs.stat).mockImplementation(async uri => {
            if (!texts.has(uri.fsPath)) throw new Error('not found');
            return { type: vscode.FileType.File, ctime: 0, mtime: 0, size: 0 };
        });
        vi.mocked(vscode.workspace.fs.readFile).mockImplementation(async uri => new TextEncoder().encode(texts.get(uri.fsPath)!));
        const compilation = await compileForBoard(documentAt('/model/views.play'));
        board = boardFor(compilation.result, compilation.name, compilation.wholeApplication);
    });
    it('should keep the board focused on the open file', () => (board.document as { collections: { modules: { name: string }[] }[] }).collections[0].modules.map(module => module.name).should.deep.equal(['Views']));
    it('should retain the producer outside the open file in the map', () => board.dependencies.edges.some(edge => edge.source === 'module:Views' && edge.target === 'module:Facts').should.be.true);
});

describe('when compiling an importing standalone document', () => {
    it('should retain its folder for evidence source navigation', async () => {
        vi.mocked(vscode.workspace.fs.stat).mockRejectedValue(new Error('not found'));
        vi.mocked(vscode.workspace.findFiles).mockResolvedValue([vscode.Uri.file('/model/views.play')]);
        vi.mocked(vscode.workspace.fs.readFile).mockResolvedValue(new TextEncoder().encode(texts.get('/model/views.play')!));
        const compilation = await compileForBoard({ ...documentAt('/model/standalone.play'), getText: () => 'import "views.play"' });
        compilation.root!.fsPath.should.equal('/model');
    });
});

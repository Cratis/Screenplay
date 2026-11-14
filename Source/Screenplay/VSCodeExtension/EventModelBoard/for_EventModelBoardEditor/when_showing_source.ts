// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it, vi } from 'vitest';
import * as vscode from 'vscode';
import { EventModelBoardEditor } from '../EventModelBoardEditor';
import { a_configuration } from '../for_ViewOptionsStore/given/a_configuration';
import { a_memento } from '../for_ViewOptionsStore/given/a_memento';
import { a_board_panel } from './given/a_board_panel';

const host = vi.hoisted(() => ({
    provider: undefined as EventModelBoardEditor | undefined,
    shown: vi.fn(),
    editor: { selection: undefined as unknown, revealRange: vi.fn() },
}));

vi.mock('vscode', async importOriginal => ({
    ...await importOriginal<typeof vscode>(),
    ViewColumn: { Beside: -2 },
    TextEditorRevealType: { InCenter: 2 },
    Selection: class { constructor(readonly start: unknown, readonly end: unknown) {} },
    workspace: {
        getConfiguration: () => a_configuration(),
        onDidChangeConfiguration: () => ({ dispose: () => {} }),
    },
    window: {
        registerCustomEditorProvider: (_type: string, provider: EventModelBoardEditor) => {
            host.provider = provider;
            return { dispose: () => {} };
        },
        showTextDocument: host.shown,
        showWarningMessage: () => {},
    },
}));
vi.mock('../BoardRefresh', () => ({
    BoardRefresh: class {
        now() {}
        dispose() {}
        sourceFor(requested: unknown) {
            return requested === 'slices/a.play' ? { fsPath: '/work/app/slices/a.play' } : undefined;
        }
    },
}));
vi.mock('../boardHtml', () => ({ boardHtml: () => '<html></html>', createNonce: () => 'nonce' }));

const document = { uri: { fsPath: '/work/app/work.play' } } as vscode.TextDocument;
let panel: ReturnType<typeof a_board_panel>;

beforeEach(() => {
    host.shown.mockReset();
    host.shown.mockResolvedValue(host.editor);
    host.editor.selection = undefined;
    host.editor.revealRange.mockReset();
    EventModelBoardEditor.register({ extensionUri: vscode.Uri.file('/extension'), globalState: a_memento(), subscriptions: [] } as unknown as vscode.ExtensionContext);
    panel = a_board_panel();
    host.provider!.resolveCustomTextEditor(document, panel.panel);
});

describe('when showing source', () => {
    it('should open a compiled file beside the board at the requested line', async () => {
        await panel.receive({ type: 'showSource', path: 'slices/a.play', line: 7 });
        host.shown.mock.calls.length.should.equal(1);
        host.shown.mock.calls[0][0].fsPath.should.equal('/work/app/slices/a.play');
        host.shown.mock.calls[0][1].viewColumn.should.equal(vscode.ViewColumn.Beside);
        (host.editor.selection as vscode.Selection).start.line.should.equal(6);
    });

    for (const path of ['../../etc/passwd', '/etc/passwd', 'slices/other.play', '']) {
        it(`should never open ${JSON.stringify(path)}`, async () => {
            await panel.receive({ type: 'showSource', path, line: 1 });
            host.shown.mock.calls.length.should.equal(0);
        });
    }

    it('should open the document itself when no path is given', async () => {
        await panel.receive({ type: 'showSource' });
        host.shown.mock.calls.length.should.equal(1);
        host.shown.mock.calls[0][0].should.equal(document.uri);
    });

    for (const line of [1.5, 0, -3, Number.NaN, Number.POSITIVE_INFINITY, '4']) {
        it(`should open without moving the selection for line ${String(line)}`, async () => {
            await panel.receive({ type: 'showSource', path: 'slices/a.play', line: line as number });
            host.shown.mock.calls.length.should.equal(1);
            (host.editor.selection === undefined).should.be.true;
        });
    }
});

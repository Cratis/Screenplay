// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { BoardToExtensionMessage, ExtensionToBoardMessage } from '../Webview/BoardMessage';
import { BoardRefresh } from './BoardRefresh';
import { boardHtml, createNonce } from './boardHtml';
import { ViewOptionsStore } from './ViewOptionsStore';

export const eventModelBoardViewType = 'screenplay.eventModelBoard';

// Opens a .play document on the event model board. It is the default editor for .play files; the text
// stays the source of truth, and the board redraws whenever the application changes - see BoardRefresh.
// The person's view options are kept for them across boards: a change on one board is saved and shown on
// every other open board.
export class EventModelBoardEditor implements vscode.CustomTextEditorProvider {
    readonly #boards = new Set<vscode.Webview>();

    constructor(private readonly extensionUri: vscode.Uri, private readonly viewOptions: ViewOptionsStore) {}

    static register(context: vscode.ExtensionContext): void {
        context.subscriptions.push(vscode.window.registerCustomEditorProvider(
            eventModelBoardViewType,
            new EventModelBoardEditor(context.extensionUri, new ViewOptionsStore(context.globalState)),
            { webviewOptions: { retainContextWhenHidden: true } }));
    }

    resolveCustomTextEditor(document: vscode.TextDocument, panel: vscode.WebviewPanel): void {
        const out = vscode.Uri.joinPath(this.extensionUri, 'out');
        panel.webview.options = { enableScripts: true, localResourceRoots: [out] };
        panel.webview.html = boardHtml({
            scriptUri: panel.webview.asWebviewUri(vscode.Uri.joinPath(out, 'webview.js')).toString(),
            styleUri: panel.webview.asWebviewUri(vscode.Uri.joinPath(out, 'webview.css')).toString(),
            cspSource: panel.webview.cspSource,
            nonce: createNonce(),
        });

        const board = new BoardRefresh(document, panel.webview);
        this.#boards.add(panel.webview);
        const received = panel.webview.onDidReceiveMessage((message: BoardToExtensionMessage) => {
            if (message.type === 'ready') {
                void panel.webview.postMessage({ type: 'viewOptions', options: this.viewOptions.options } satisfies ExtensionToBoardMessage);
                board.now();
            } else if (message.type === 'viewOptionsChanged') {
                void this.viewOptions.save(message.options);
                for (const other of this.#boards) {
                    if (other !== panel.webview) {
                        void other.postMessage({ type: 'viewOptions', options: message.options } satisfies ExtensionToBoardMessage);
                    }
                }
            } else if (message.type === 'showSource') {
                const file = message.path !== undefined && board.root !== undefined ? vscode.Uri.joinPath(board.root, message.path) : document.uri;
                void showSource(file, message.line);
            }
        });
        panel.onDidDispose(() => {
            this.#boards.delete(panel.webview);
            board.dispose();
            received.dispose();
        });
    }
}

// Opens the document in the text editor beside the board, at a line when one is given.
export async function showSource(uri: vscode.Uri, line?: number): Promise<void> {
    const editor = await vscode.window.showTextDocument(uri, { viewColumn: vscode.ViewColumn.Beside });
    if (line !== undefined) {
        const position = new vscode.Position(Math.max(0, line - 1), 0);
        editor.selection = new vscode.Selection(position, position);
        editor.revealRange(new vscode.Range(position, position), vscode.TextEditorRevealType.InCenter);
    }
}

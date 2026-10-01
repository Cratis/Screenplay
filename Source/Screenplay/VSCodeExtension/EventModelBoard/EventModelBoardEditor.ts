// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { parse } from '@cratis/screenplay-compiler';
import { BoardToExtensionMessage } from '../Webview/BoardMessage';
import { boardFor } from './compileBoard';
import { boardHtml, createNonce } from './boardHtml';

export const eventModelBoardViewType = 'screenplay.eventModelBoard';

// How long the board waits after a keystroke before compiling again, so typing does not redraw it per key.
const recompileDelay = 300;

// Opens a .play document on the event model board. It is the default editor for .play files; the text
// stays the source of truth, and the board redraws whenever it changes - also when it is edited in a text
// editor beside the board.
export class EventModelBoardEditor implements vscode.CustomTextEditorProvider {
    constructor(private readonly extensionUri: vscode.Uri) {}

    static register(context: vscode.ExtensionContext): void {
        context.subscriptions.push(vscode.window.registerCustomEditorProvider(
            eventModelBoardViewType,
            new EventModelBoardEditor(context.extensionUri),
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

        const show = () => void panel.webview.postMessage(boardFor(parse(document.getText()), nameOf(document.uri)));
        let pending: ReturnType<typeof setTimeout> | undefined;
        const changed = vscode.workspace.onDidChangeTextDocument(event => {
            if (event.document.uri.toString() === document.uri.toString()) {
                clearTimeout(pending);
                pending = setTimeout(show, recompileDelay);
            }
        });
        const received = panel.webview.onDidReceiveMessage((message: BoardToExtensionMessage) => {
            if (message.type === 'ready') {
                show();
            } else if (message.type === 'showSource') {
                void showSource(document.uri, message.line);
            }
        });
        panel.onDidDispose(() => {
            clearTimeout(pending);
            changed.dispose();
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

function nameOf(uri: vscode.Uri): string {
    const file = uri.path.substring(uri.path.lastIndexOf('/') + 1);
    return file.endsWith('.play') ? file.substring(0, file.length - '.play'.length) : file;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { EventModelBoardEditor, eventModelBoardViewType, showSource } from './EventModelBoardEditor';

// The board as the default editor for .play files, and the commands that move between it and the text.
export function registerEventModelBoard(context: vscode.ExtensionContext): void {
    EventModelBoardEditor.register(context);
    context.subscriptions.push(
        vscode.commands.registerCommand('screenplay.showBoard', (uri?: vscode.Uri) => {
            const target = uri ?? vscode.window.activeTextEditor?.document.uri;
            return target === undefined ? undefined : vscode.commands.executeCommand('vscode.openWith', target, eventModelBoardViewType);
        }),
        vscode.commands.registerCommand('screenplay.showSource', (uri?: vscode.Uri) => {
            const target = uri ?? activeBoardUri();
            return target === undefined ? undefined : showSource(target);
        }));
}

// The document of the board in the active editor tab, if a board is active.
function activeBoardUri(): vscode.Uri | undefined {
    const input = vscode.window.tabGroups.activeTabGroup.activeTab?.input;
    return input instanceof vscode.TabInputCustom && input.viewType === eventModelBoardViewType ? input.uri : undefined;
}

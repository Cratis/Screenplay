// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ensureBuiltInSubLanguages } from '@cratis/screenplay-language';
import { registerCompletions } from './Completions';
import { registerHover } from './Hover';
import { registerDiagnostics } from './Diagnostics';
import { registerFileLinks } from './FileLinks';
import { registerEventModelBoard } from './EventModelBoard/registerEventModelBoard';
import { ApplicationIndex } from './ApplicationIndex';

export function activate(context: vscode.ExtensionContext): void {
    ensureBuiltInSubLanguages();
    // The .play files of a workspace folder are one application, so a name or an import in one file is
    // checked against all of them.
    const index = new ApplicationIndex();
    context.subscriptions.push(index);
    registerCompletions(context, index);
    registerHover(context);
    registerDiagnostics(context, index);
    registerFileLinks(context);
    registerEventModelBoard(context);
    void index.load();
}

export function deactivate(): void {}

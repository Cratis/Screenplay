// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ensureBuiltInSubLanguages } from '@cratis/screenplay-language';
import { registerCompletions } from './Completions';
import { registerResponseTokens } from './ResponseTokens';
import { registerHover } from './Hover';
import { registerInlayHints } from './InlayHints';
import { registerDefinitions } from './Definitions';
import { registerCodeActions } from './CodeActions';
import { registerDiagnostics } from './Diagnostics';
import { registerFileLinks } from './FileLinks';
import { registerEventModelBoard } from './EventModelBoard/registerEventModelBoard';
import { ApplicationIndex } from './ApplicationIndex';
import { registerRepairCodeActions } from './RepairCodeActions';

import { readRepairObservation } from './RepairObservation';

export function activate(context: vscode.ExtensionContext): { readonly repairObservation: { readonly read: typeof readRepairObservation } } {
    ensureBuiltInSubLanguages();
    // The .play files of a workspace folder are one application, so a name or an import in one file is
    // checked against all of them.
    const index = new ApplicationIndex();
    context.subscriptions.push(index);
    registerCompletions(context, index);
    registerHover(context, index);
    registerResponseTokens(context);
    registerInlayHints(context, index);
    registerDefinitions(context, index);
    registerDiagnostics(context, index);
    registerCodeActions(context, index);
    registerRepairCodeActions(context, index);
    registerFileLinks(context);
    registerEventModelBoard(context);
    void index.load();
    return Object.freeze({ repairObservation: Object.freeze({ read: readRepairObservation }) });
}

export function deactivate(): void {}

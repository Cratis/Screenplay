// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ensureBuiltInSubLanguages } from '@cratis/screenplay-language';
import { registerCompletions } from './Completions';
import { registerHover } from './Hover';
import { registerDiagnostics } from './Diagnostics';

export function activate(context: vscode.ExtensionContext): void {
    ensureBuiltInSubLanguages();
    registerCompletions(context);
    registerHover(context);
    registerDiagnostics(context);
}

export function deactivate(): void {}

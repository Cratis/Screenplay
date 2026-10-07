// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';

interface SpellCheckerApi {
    registerConfig?(configPath: string): void;
}

// Teaches the Code Spell Checker extension, when it is installed, the words of the language, so
// `readmodel` and the other keywords are not flagged as unknown in .play files.
export async function registerSpellCheckerWords(context: vscode.ExtensionContext): Promise<void> {
    const spellChecker = vscode.extensions.getExtension<SpellCheckerApi>('streetsidesoftware.code-spell-checker');
    if (!spellChecker) return;
    const api = await spellChecker.activate();
    api?.registerConfig?.(vscode.Uri.joinPath(context.extensionUri, 'cspell-screenplay.json').fsPath);
}

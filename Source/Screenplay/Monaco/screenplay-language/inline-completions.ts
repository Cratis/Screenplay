// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { languages } from 'monaco-editor';
import type { CompletionOptions } from './completions';
import { structureCompletion } from './structure-completions';
import { symbolsForBuffer } from './symbols';

// Ghost text for the structure a block obviously needs next, from the compiler's own model.
export function createInlineCompletionProvider(options: CompletionOptions): languages.InlineCompletionsProvider {
    return {
        provideInlineCompletions(model, position) {
            const lines = model.getLinesContent();
            const lineIndex = position.lineNumber - 1;
            const line = lines[lineIndex] ?? '';
            const symbols = symbolsForBuffer(lines, options.application?.(model));
            const suggestion = structureCompletion(lines, lineIndex, line.substring(0, position.column - 1), line.substring(position.column - 1), symbols);
            if (!suggestion) return { items: [] };
            // The suggestion replaces the indentation before the cursor, so it starts with indentation of its own -
            // the editors take Tab as 'indent' rather than 'accept' for a suggestion that does not.
            return { items: [{ insertText: `${line.substring(0, position.column - 1)}${suggestion.text}`, range: { startLineNumber: position.lineNumber, startColumn: 1, endLineNumber: position.lineNumber, endColumn: position.column } }] };
        },
        disposeInlineCompletions() {},
    };
}

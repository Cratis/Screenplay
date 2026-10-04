// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { languages } from 'monaco-editor';
import { destinationHints } from './production-destinations';
import { CompletionOptions } from './completions';

export function createInlayHintsProvider(options: CompletionOptions = {}): languages.InlayHintsProvider {
    return {
        provideInlayHints(model, range) {
            const hints = destinationHints(model.getLinesContent(), options.application?.(model)).filter(hint => hint.line + 1 >= range.startLineNumber && hint.line + 1 <= range.endLineNumber);
            return {
                hints: hints.map(hint => ({ position: { lineNumber: hint.line + 1, column: hint.column }, label: hint.label, paddingLeft: true })),
                dispose() {},
            };
        },
    };
}

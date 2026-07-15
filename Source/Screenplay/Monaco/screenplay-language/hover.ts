// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { languages } from 'monaco-editor';
import { hoverContent } from './hover-content';

export function createHoverProvider(): languages.HoverProvider {
    return {
        provideHover(model, position) {
            const word = model.getWordAtPosition(position);
            if (!word) return null;

            const content = hoverContent(
                model.getLinesContent(),
                position.lineNumber - 1,
                word.word,
                word.startColumn,
                word.endColumn,
            );
            return content ? { contents: [{ value: content }] } : null;
        },
    };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLine } from './SourceLine';

// Splits a document into lines the way the C# SourceLineSplitter does: a line's indent is its leading
// whitespace, and its content is what follows with any comment and trailing whitespace removed. A comment
// starts at '//' (or '#' in the sub-languages that use it) outside a string or a template literal.
export function splitLines(source: string, hashComments = false, path?: string): SourceLine[] {
    const result: SourceLine[] = [];
    let number = 0;
    let offset = 0;
    for (const raw of source.split('\n')) {
        number++;
        const line = raw.endsWith('\r') ? raw.replace(/\r+$/, '') : raw;
        const indent = line.length - line.trimStart().length;
        const content = stripComment(line.substring(indent), hashComments).trimEnd();
        result.push(path === undefined
            ? { number, raw: line, indent, content, startOffset: offset }
            : { number, raw: line, indent, content, path, startOffset: offset });
        offset += raw.length + 1;
    }
    return result;
}

// The index a comment starts at in the text, or -1 when it has none.
export function commentStart(text: string, hashComments = false): number {
    let inString = false;
    let inTemplate = false;
    for (let index = 0; index < text.length; index++) {
        const current = text[index];
        if (current === '\\' && inString && index + 1 < text.length) {
            index++;
        } else if (current === '"' && !inTemplate) {
            inString = !inString;
        } else if (current === '`' && !inString) {
            inTemplate = !inTemplate;
        } else if (!inString && !inTemplate) {
            if (current === '/' && index + 1 < text.length && text[index + 1] === '/') {
                return index;
            }
            if (hashComments && current === '#') {
                return index;
            }
        }
    }
    return -1;
}

function stripComment(text: string, hashComments: boolean): string {
    const comment = commentStart(text, hashComments);
    return comment < 0 ? text : text.substring(0, comment);
}

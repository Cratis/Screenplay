// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fenceMap, firstWord, indentOf } from './document-context';

// The keyword the directive opens with — the single word the language points at a file with.
export const fileReferenceKeyword = 'file';

// A `file <path>` directive, with the path exactly as written and the span it occupies on its line.
// The span is measured against the raw line, and its columns are one-based like every other position
// this package reports, so an editor places a range on it the same way it does a validation issue.
export interface FileReference {
    path: string;
    line: number;
    startColumn: number;
    endColumn: number;
}

// `file String` is a property named `file`; `file Invoices/Register.cs` is the directive. The two are
// told apart by shape, the way the compiler tells them apart: a type reference is a bare identifier,
// so anything carrying a separator or an extension is a path and nothing else. The property wins the
// tie, because a document that used the name before the directive existed keeps meaning what it meant.
const typeReferencePattern = /^[A-Za-z_]\w*(?:\[\])?\??$/;

// A path naming a place on one machine rather than a place in the repository. The compiler warns about
// these; nothing here rejects one, so a resolver can still probe it and come up empty on other machines.
const absolutePathPattern = /^(?:[/\\]|[A-Za-z]:[/\\]|~[/\\])/;

export function isAbsoluteFileReferencePath(path: string): boolean {
    return absolutePathPattern.test(path);
}

// Drops a trailing `//` comment without cutting into a string or a template, mirroring how the
// compiler strips one before it reads the line.
function withoutComment(text: string): string {
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
        } else if (!inString && !inTemplate && current === '/' && text[index + 1] === '/') {
            return text.slice(0, index);
        }
    }

    return text;
}

// The `file` directive on a single line, or undefined when the line is not one. Callers that scan a
// whole document go through fileReferences instead, so code fences are accounted for.
export function fileReferenceOn(line: string, lineIndex: number): FileReference | undefined {
    const indent = indentOf(line);
    const content = withoutComment(line.slice(indent)).trimEnd();
    if (firstWord(content) !== fileReferenceKeyword) return undefined;

    const afterKeyword = content.slice(fileReferenceKeyword.length);
    const path = afterKeyword.trim();

    // A bare `file` leaves the word available as a name to whatever the enclosing block reads a bare
    // word as, and a bare identifier after it is a property type rather than a path.
    if (path.length === 0 || typeReferencePattern.test(path)) return undefined;

    const separatorLength = afterKeyword.length - afterKeyword.trimStart().length;
    const startColumn = indent + fileReferenceKeyword.length + separatorLength + 1;
    return { path, line: lineIndex, startColumn, endColumn: startColumn + path.length };
}

// Every `file` directive in a document, in document order. Lines inside a code fence are skipped, so
// a `file` line in an inline C# or TypeScript block is never mistaken for one of ours.
export function fileReferences(lines: string[]): FileReference[] {
    const fences = fenceMap(lines);
    const references: FileReference[] = [];

    for (let index = 0; index < lines.length; index++) {
        if (fences[index]) continue;
        const reference = fileReferenceOn(lines[index], index);
        if (reference) references.push(reference);
    }

    return references;
}

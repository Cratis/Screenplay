// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fenceMap, indentOf } from './document-context';

// An `import "<path or glob>"` of other .play files, with the pattern exactly as written and the span it
// occupies between the quotes. Columns are one-based like every other position this package reports.
export interface FileImport {
    pattern: string;
    line: number;
    startColumn: number;
    endColumn: number;
}

// The compiler's own shape for a file import (FileImportParser): a quoted operand without quotes or escapes.
const fileImportPattern = /^import(\s+)"([^"\\]+)"\s*(?:\/\/.*)?$/;
const quotedImportPattern = /^import\s+"/;

// Whether a line imports files rather than a qualified name - its operand is quoted.
export function isFileImportLine(line: string): boolean {
    return quotedImportPattern.test(line.trim());
}

// The file import on a single line, or undefined when the line is not a well-formed one.
export function fileImportOn(line: string, lineIndex: number): FileImport | undefined {
    const indent = indentOf(line);
    const match = line.slice(indent).match(fileImportPattern);
    if (!match) return undefined;
    const startColumn = indent + 'import'.length + match[1].length + 2;
    return { pattern: match[2], line: lineIndex, startColumn, endColumn: startColumn + match[2].length };
}

// Every well-formed file import in a document, in document order, skipping code fences.
export function fileImports(lines: string[]): FileImport[] {
    const fences = fenceMap(lines);
    const found: FileImport[] = [];
    for (let index = 0; index < lines.length; index++) {
        if (fences[index]) continue;
        const fileImport = fileImportOn(lines[index], index);
        if (fileImport) found.push(fileImport);
    }
    return found;
}

const folderOf = (path: string): string[] => path.split('/').slice(0, -1);

function relativeTo(from: readonly string[], path: string): string {
    const segments = path.split('/');
    let common = 0;
    while (common < from.length && common < segments.length - 1 && from[common] === segments[common]) common++;
    return [...from.slice(common).map(() => '..'), ...segments.slice(common)].join('/');
}

// What an import written in a document can name: every other .play file, relative to the document's folder;
// a glob for the files directly in each folder holding one; and a glob for everything beneath each folder on
// the way to them. Paths are portable ('/' separated) and relative to one root, the way the compiler resolves them.
export function importablePaths(documentPath: string, files: readonly string[]): string[] {
    const from = folderOf(documentPath);
    const paths = new Set<string>();
    for (const file of files.filter((candidate) => candidate !== documentPath && candidate.toLowerCase().endsWith('.play'))) {
        const relative = relativeTo(from, file);
        const folders = folderOf(relative);
        paths.add(relative);
        paths.add([...folders, '*.play'].join('/'));
        for (let depth = 0; depth <= folders.length; depth++) {
            paths.add([...folders.slice(0, depth), '**', '*.play'].join('/'));
        }
    }
    return [...paths].sort();
}

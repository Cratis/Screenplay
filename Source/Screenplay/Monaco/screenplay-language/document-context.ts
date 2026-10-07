// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Matches SourceLineSplitter.CommentStart: // inside a double-quoted string or a
// backtick template is content; a backslash escapes the next character only in a string.
export function withoutComment(line: string): string {
    let inString = false;
    let inTemplate = false;
    for (let index = 0; index < line.length; index++) {
        const character = line[index];
        if (character === '\\' && inString && index + 1 < line.length) {
            index++;
        } else if (character === '"' && !inTemplate) {
            inString = !inString;
        } else if (character === '`' && !inString) {
            inTemplate = !inTemplate;
        } else if (!inString && !inTemplate && character === '/' && line[index + 1] === '/') {
            return line.slice(0, index);
        }
    }
    return line;
}

export function indentOf(line: string): number {
    return line.length - line.trimStart().length;
}

export function firstWord(line: string): string {
    return line.trim().split(/\s+/)[0] ?? '';
}

// Marks every line that is a code fence or lives inside one, so structural
// scanning never mistakes inline C#/TypeScript/HTML for Screenplay.
export function fenceMap(lines: string[]): boolean[] {
    const map: boolean[] = new Array(lines.length).fill(false);
    let open = false;
    for (let index = 0; index < lines.length; index++) {
        if (/^\s*```(?:[a-z]+)?\s*$/.test(lines[index]) && (!open || /^\s*```\s*$/.test(lines[index]))) {
            map[index] = true;
            open = !open;
            continue;
        }
        map[index] = open;
    }
    return map;
}

// Walks upward from a position and returns the chain of enclosing block openers,
// innermost first — e.g. ['command', 'slice', 'feature', 'module']. A block opener
// is any less-indented line above; its first word names the construct (for layout
// slots this is the slot name, which callers treat as an unknown construct).
export function enclosingChain(
    lines: string[],
    fences: boolean[],
    lineIndex: number,
    indent: number,
): string[] {
    const chain: string[] = [];
    let currentIndent = indent;
    for (let index = lineIndex - 1; index >= 0 && currentIndent > 0; index--) {
        if (fences[index]) continue;
        const line = withoutComment(lines[index]);
        if (line.trim().length === 0) continue;
        const lineIndent = indentOf(line);
        if (lineIndent < currentIndent) {
            chain.push(firstWord(line));
            currentIndent = lineIndent;
        }
    }
    return chain;
}

// Returns the full trimmed text of the nearest enclosing block opener above a
// position — the same line enclosingChain's first entry is derived from, but kept
// whole rather than reduced to its first word. Used where the construct is not
// identified by a fixed leading keyword, such as a validation rule line, whose
// first word is the property it applies to rather than "rule".
export function nearestEnclosingLine(
    lines: string[],
    fences: boolean[],
    lineIndex: number,
    indent: number,
): string | undefined {
    for (let index = lineIndex - 1; index >= 0 && indent > 0; index--) {
        if (fences[index]) continue;
        const line = withoutComment(lines[index]);
        if (line.trim().length === 0) continue;
        if (indentOf(line) < indent) {
            return line.trim();
        }
    }
    return undefined;
}

// Like enclosingChain, but keeps each opener whole - 'slice StateView Name' rather than 'slice' - innermost first.
export function enclosingHeaders(
    lines: string[],
    fences: boolean[],
    lineIndex: number,
    indent: number,
): string[] {
    const headers: string[] = [];
    let currentIndent = indent;
    for (let index = lineIndex - 1; index >= 0 && currentIndent > 0; index--) {
        if (fences[index]) continue;
        const line = withoutComment(lines[index]);
        if (line.trim().length === 0) continue;
        const lineIndent = indentOf(line);
        if (lineIndent < currentIndent) {
            headers.push(line.trim());
            currentIndent = lineIndent;
        }
    }
    return headers;
}

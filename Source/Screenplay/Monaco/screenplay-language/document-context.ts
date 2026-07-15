// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        if (/^\s*```\s*$/.test(lines[index])) {
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
        const line = lines[index];
        if (line.trim().length === 0) continue;
        const lineIndent = indentOf(line);
        if (lineIndent < currentIndent) {
            chain.push(firstWord(line));
            currentIndent = lineIndent;
        }
    }
    return chain;
}

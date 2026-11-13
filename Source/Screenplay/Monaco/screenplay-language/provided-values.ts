// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fenceMap, indentOf, withoutComment } from './document-context';

export interface ProvidedValueDeclaration {
    name: string;
    type: string;
    // How the value is supplied: a query, the host, a value expression, or code the model cannot evaluate.
    source: 'query' | 'host' | 'value' | 'code';
    sourceText: string;
    // 0-based line of the declaration and the level it belongs to.
    line: number;
    level: 'application' | 'module' | 'feature' | 'slice';
    // Host and code sources are opaque: the model never evaluates them.
    isOpaque: boolean;
}

const declaration = /^(\s*)provide\s+([a-z_]\w*)\s+(.+?)\s*$/;
const levelKeywords: Record<string, ProvidedValueDeclaration['level']> = { module: 'module', feature: 'feature', slice: 'slice' };

function levelOf(lines: string[], fences: boolean[], lineIndex: number, indent: number): ProvidedValueDeclaration['level'] {
    if (indent === 0) return 'application';
    for (let index = lineIndex - 1; index >= 0; index--) {
        if (fences[index]) continue;
        const text = withoutComment(lines[index]);
        if (text.trim() === '' || indentOf(text) >= indent) continue;
        const keyword = /^\s*(module|feature|slice)\b/.exec(text)?.[1];
        if (keyword) return levelKeywords[keyword];
        indent = indentOf(text);
        if (indent === 0) return 'application';
    }
    return 'application';
}

function describe(rest: string, lines: string[], lineIndex: number): Pick<ProvidedValueDeclaration, 'type' | 'source' | 'sourceText' | 'isOpaque'> {
    const split = /^(.*?)\s+from\s+(.*)$/.exec(rest);
    if (!split) {
        const next = lines[lineIndex + 1]?.trim() ?? '';
        return { type: rest, source: 'code', sourceText: next.startsWith('file') ? next : 'an inline code block', isOpaque: true };
    }
    const sourceText = split[2];
    if (sourceText === 'host') return { type: split[1], source: 'host', sourceText, isOpaque: true };
    if (/^query\s/.test(sourceText)) return { type: split[1], source: 'query', sourceText, isOpaque: false };
    return { type: split[1], source: 'value', sourceText, isOpaque: false };
}

/**
 * Finds the `provide` declaration that wins for a value name at a line: the nearest enclosing level
 * (slice over feature over module over application) that declares it. Text based, so it works while the
 * document is still being typed.
 */
export function providedValueAt(lines: string[], lineIndex: number, name: string): ProvidedValueDeclaration | undefined {
    const fences = fenceMap(lines);
    let winner: ProvidedValueDeclaration | undefined;
    let winnerIndent = -1;
    for (let index = 0; index < lines.length; index++) {
        if (fences[index]) continue;
        const match = declaration.exec(withoutComment(lines[index]));
        if (!match || match[2] !== name) continue;
        const indent = match[1].length;
        if (indent <= winnerIndent || !inScope(lines, fences, index, indent, lineIndex)) continue;
        winnerIndent = indent;
        winner = { name, line: index, level: levelOf(lines, fences, index, indent), ...describe(match[3], lines, index) };
    }
    return winner;
}

/**
 * Every provided value in scope at a line, one winning declaration per name.
 */
export function providedValuesAt(lines: string[], lineIndex: number): ProvidedValueDeclaration[] {
    const fences = fenceMap(lines);
    const names = new Set<string>();
    for (let index = 0; index < lines.length; index++) {
        const name = fences[index] ? undefined : declaration.exec(withoutComment(lines[index]))?.[2];
        if (name) names.add(name);
    }
    return [...names].map(name => providedValueAt(lines, lineIndex, name)).filter((value): value is ProvidedValueDeclaration => value !== undefined);
}

function inScope(lines: string[], fences: boolean[], declarationLine: number, indent: number, lineIndex: number): boolean {
    if (indent === 0) return true;
    // The declaration's container ends at the first non-blank line indented less than the declaration.
    const start = Math.min(declarationLine, lineIndex);
    const end = Math.max(declarationLine, lineIndex);
    for (let index = start + 1; index <= end; index++) {
        if (fences[index]) continue;
        const text = withoutComment(lines[index]);
        if (text.trim() !== '' && indentOf(text) < indent) return false;
    }
    return true;
}

export function providedValueHover(lines: string[], lineIndex: number, variable: string): string | null {
    const name = /^\$context\.([a-z_]\w*)/.exec(variable)?.[1];
    if (!name) return null;
    const provided = providedValueAt(lines, lineIndex, name);
    if (!provided) return null;
    const opaque = provided.isOpaque ? ' Opaque: the model never evaluates it, so checks treat it as unknown, never allowed or denied.' : '';
    return `**$context.${name}** — ${provided.type}. Provided at the ${provided.level} level by \`provide ${name} ${provided.type}${provided.source === 'code' ? '' : ` from ${provided.sourceText}`}\` (line ${provided.line + 1}); the nearest declaration wins.${opaque}`;
}

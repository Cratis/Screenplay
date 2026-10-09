// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { responseAnalysis } from './response-analysis';
import { enclosingHeaders, fenceMap, indentOf } from './document-context';

function enclosing(lines: string[], line: number) {
    const headers = enclosingHeaders(lines, fenceMap(lines), line, indentOf(lines[line]));
    if (!headers.some(header => header.startsWith('specification ')) || headers.some(header => /^(?:case|given caller|when redelivered)\b/.test(header))) return undefined;
    return [...responseAnalysis(lines).specifications].filter(([start]) => start <= line).sort(([left], [right]) => right - left)[0]?.[1];
}

export function caseCompletions(lines: string[], line: number, before: string) {
    if (!/(?:=\s*|for\s+|then\s+error\s+)case\.[\w]*$/.test(before) || /when\s+redelivered|given\s+caller/.test(before) || /^\s*case\b/.test(before)) return null;
    const table = enclosing(lines, line);
    if ((table?.cases?.length ?? 0) === 0) return null;
    return table!.parameters?.map(parameter => ({ label: `case.${parameter.name}`, insertText: `case.${parameter.name}`, documentation: `Case value of ${parameter.type.name}${parameter.type.isCollection ? '[]' : ''}${parameter.type.isOptional ? ' optional' : ''}.` })) ?? [];
}

export function caseHover(lines: string[], line: number, start: number, end: number): string | null {
    const text = lines[line];
    const reference = [...text.matchAll(/\bcase\.([a-z_]\w*)\b/g)].find(match => match.index! < end && match.index! + match[0].length >= start);
    if (reference === undefined) return null;
    const table = enclosing(lines, line);
    const parameter = table?.parameters?.find(parameter => parameter.name === reference[1]);
    if (parameter === undefined) return null;
    const values = table!.cases?.map(row => {
        const source = row.values.find(value => value.property === parameter.name)?.source;
        const value = source !== null && typeof source === 'object' && 'value' in source ? source.value : source;
        return `- ${row.name}: \`${JSON.stringify(value)}\``;
    }).join('\n');
    return `**case.${parameter.name}** — ${parameter.type.name}\n\nCase origin in **${table!.name}**; substitutes after example overrides.\n\n${values ?? ''}`;
}

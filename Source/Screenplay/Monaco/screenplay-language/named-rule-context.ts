// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fenceMap, indentOf, withoutComment } from './document-context';

// The command property form, including its existing optional severity/message suffixes.
export const commandNamedRulePattern = /^[\p{L}\p{Mn}\p{Nd}\p{Pc}.]+\s+rule\s+[A-Za-z_][\p{L}\p{Mn}\p{Nd}\p{Pc}]*(?:\s+severity\s+(?:information|warning|error))?(?:\s+message\s+.*)?$/u;

export function namedRuleContext(lines: string[], lineIndex: number, indent: number): 'rule' | 'implementation' | null {
    const fences = fenceMap(lines);
    const owners: string[] = [];
    let boundary = indent;
    for (let index = lineIndex - 1; index >= 0 && boundary > 0; index--) {
        if (fences[index]) continue;
        const line = withoutComment(lines[index]).trimEnd();
        if (line.trim().length === 0 || indentOf(line) >= boundary) continue;
        owners.push(line.trim());
        boundary = indentOf(line);
    }
    const wrapped = owners[0] === 'implementation';
    const offset = wrapped ? 1 : 0;
    return commandNamedRulePattern.test(owners[offset] ?? '') && owners[offset + 1] === 'validate' && /^command\s/.test(owners[offset + 2] ?? '')
        ? wrapped ? 'implementation' : 'rule' : null;
}

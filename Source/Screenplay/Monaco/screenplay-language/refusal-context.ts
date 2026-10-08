// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fenceMap, indentOf, withoutComment } from './document-context';

// Return only an enclosing invocation branch, never a sibling branch or quoted/code content.
export function refusalContext(lines: string[], lineIndex: number, indent: number): string | undefined {
    const fences = fenceMap(lines);
    const headers: string[] = [];
    for (let index = lineIndex - 1; index >= 0 && indent > 0; index--) {
        if (fences[index]) continue;
        const line = withoutComment(lines[index]);
        if (line.trim().length === 0 || indentOf(line) >= indent) continue;
        headers.push(line.trim());
        indent = indentOf(line);
    }
    const branch = headers.findIndex(header => /^on\s+refused(?:\s|$)/.test(header));
    return branch >= 0 && /^invokes\s+/.test(headers[branch + 1] ?? '') && headers.some(header => /^reaction\s+/.test(header)) ? headers[branch] : undefined;
}

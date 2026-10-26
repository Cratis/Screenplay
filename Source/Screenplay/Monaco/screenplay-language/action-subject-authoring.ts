// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function actionSubjectHover(line: string, word: string, column: number): string | null {
    const match = /^\s*(?:action\s+(?:"(?:[^"\\]|\\.)*"|\$strings\.[\w.]+|[\w.]+)|item\s+\w+\s+action\s+[\w.]+)\s+(for)\s+(item|screen)\b/.exec(line);
    if (match === null || !['for', match[2]].includes(word)) return null;
    const start = match[0].lastIndexOf('for');
    if (column - 1 < start || column - 1 >= match[0].length) return null;
    return match[2] === 'item'
        ? '`for item` uses one nearest data record or selected collection item. Explicit inputs win, followed by compatible same-name fields and one declared nominal identifier/key match. Missing or equally near data is not guessed.'
        : '`for screen` scopes a plain or toolbar action to the screen, without row input filling. A guarded action can inspect a single record, but collection-valued data warns and does not choose a row.';
}

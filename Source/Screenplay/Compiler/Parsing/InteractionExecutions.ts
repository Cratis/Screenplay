// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InteractionArgumentSyntax } from '../Syntax/Screens';
import { dotNetWhitespace, pattern } from '../Text/patterns';
import { InteractionExecution } from './InteractionExecution';
import { firstWord } from './LineText';
import { locationOf, SourceLine } from './SourceLine';

const execute = pattern(`^execute${dotNetWhitespace}+([A-Za-z_]\\w*(?:\\.\\w+)*)$`);
const argument = pattern(`^with${dotNetWhitespace}+([A-Za-z_]\\w*)${dotNetWhitespace}+from${dotNetWhitespace}+([^\\n]+)$`);
const trim = new RegExp(`^${dotNetWhitespace}+|${dotNetWhitespace}+$`, 'gu');
const continuation = pattern(`^on${dotNetWhitespace}+(success|failure|result)$`);
const failable = new Set(['execute', 'refresh', 'confirm', 'open', 'raise']);

export function interactionBody(lines: readonly SourceLine[], header: SourceLine): readonly SourceLine[] {
    const start = lines.indexOf(header) + 1;
    let end = start;
    while (end < lines.length && lines[end].indent > header.indent) end++;
    return lines.slice(start, end);
}

// Preserve action, success, failure, result order even when continuations are authored out of order.
export function interactionExecutions(lines: readonly SourceLine[], depth = 1): readonly InteractionExecution[] {
    if (depth > 16 || lines.length === 0) return [];
    const result: InteractionExecution[] = [];
    for (const header of lines.filter(line => line.indent === lines[0].indent)) {
        const command = execute.exec(header.content);
        const keyword = firstWord(header.content);
        if (command === null && !failable.has(keyword)) continue;
        const body = interactionBody(lines, header);
        const children = body.filter(line => line.indent === body[0]?.indent);
        if (command !== null) {
            const arguments_: InteractionArgumentSyntax[] = children.flatMap(line => {
                const match = argument.exec(line.content);
                return match === null ? [] : [{ kind: 'InteractionArgumentSyntax', name: match[1], binding: match[2].replace(trim, ''), location: locationOf(line) }];
            });
            result.push({ command: command[1], arguments: arguments_ });
        }
        for (const outcome of ['success', 'failure', 'result']) {
            if (outcome === 'result' && !pattern(`^open${dotNetWhitespace}+dialog${dotNetWhitespace}+`).test(header.content)) continue;
            for (const child of children.filter(line => continuation.exec(line.content)?.[1] === outcome)) {
                result.push(...interactionExecutions(interactionBody(body, child), depth + 1));
            }
        }
    }
    return result;
}

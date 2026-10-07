// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConcurrencySyntax, ReadsSyntax, ReducerRuleSyntax, ReducerSyntax } from '../Syntax/DependencySources';
import { dotNetWhitespace, pattern } from '../Text/patterns';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

// Reference-only capture does not change the committed parser's diagnostics or opaque-block skipping.
// Fork before reading a body: code, malformed operands and unsupported realization stay opaque.
export function captureReads(line: SourceLine): ReadsSyntax | undefined {
    const match = pattern(`^reads${dotNetWhitespace}+([A-Z]\\w*)(?:${dotNetWhitespace}+as${dotNetWhitespace}+([a-z_]\\w*))?(?:${dotNetWhitespace}+by${dotNetWhitespace}+([a-z_]\\w*))?$`).exec(line.content);
    if (match === null || ['as', 'by', 'reads'].includes(match[2])) return undefined;
    return { kind: 'ReadsSyntax', readModel: match[1], alias: match[2] ?? null, by: match[3] ?? null, location: locationOf(line) };
}

function fork(context: ParserContext): ParserContext {
    return new ParserContext(context.reader.fork(), context.path, context.languages);
}

export function captureConcurrency(context: ParserContext, line: SourceLine): ConcurrencySyntax | undefined {
    if (line.content !== 'concurrency') return undefined;
    const body = fork(context);
    let eventSource = false;
    const dimensions: Record<string, string> = {};
    let eventTypes: string[] | undefined;
    for (let child = body.peekChild(line.indent); child !== undefined; child = body.peekChild(line.indent)) {
        body.reader.takeSignificant();
        // C# keeps walking every descendant of the header for recognized dimensions, even malformed
        // or duplicate ones. Only an unknown dimension skips its own descendants during recovery.
        switch (firstWord(child.content)) {
            case 'eventSource':
                if (child.content === 'eventSource') eventSource = true;
                break;
            case 'sourceType':
            case 'streamType':
            case 'streamId': {
                const dimension = pattern(`^(sourceType|streamType|streamId)${dotNetWhitespace}+([A-Za-z_]\\w*)$`).exec(child.content);
                if (dimension !== null) dimensions[dimension[1]] ??= dimension[2];
                break;
            }
            case 'events': {
                if (eventTypes !== undefined) break;
                const names = child.content.slice('events'.length).split(',').map(name => name.replace(new RegExp(`^${dotNetWhitespace}+|${dotNetWhitespace}+$`, 'gu'), '')).filter(Boolean);
                if (names.length > 0 && names.every(name => pattern('^[A-Z]\\w*$').test(name))) eventTypes = names;
                break;
            }
            default:
                body.skipBlock(child.indent);
                break;
        }
    }
    return { kind: 'ConcurrencySyntax', eventSource, eventSourceType: dimensions.sourceType ?? null, eventStreamType: dimensions.streamType ?? null, eventStreamId: dimensions.streamId ?? null, eventTypes: eventTypes ?? [], location: locationOf(line) };
}

export function captureReducer(context: ParserContext, line: SourceLine): ReducerSyntax {
    const match = pattern(`^reducer${dotNetWhitespace}+([A-Za-z_]\\w*)${dotNetWhitespace}*=>${dotNetWhitespace}*([A-Za-z_]\\w*)$`).exec(line.content);
    const body = fork(context);
    const rules: ReducerRuleSyntax[] = [];
    for (let child = body.peekChild(line.indent); child !== undefined; child = body.peekChild(line.indent)) {
        body.reader.takeSignificant();
        const rule = pattern(`^on${dotNetWhitespace}+([A-Z]\\w*)$`).exec(child.content);
        if (rule !== null) rules.push({ kind: 'ReducerRuleSyntax', event: rule[1], file: null, code: null, description: null, location: locationOf(child) });
        body.skipOpaqueBlock(child.indent);
    }
    return { kind: 'ReducerSyntax', name: match?.[1] ?? '', readModel: match?.[2] ?? '', rules, description: null, location: locationOf(line) };
}

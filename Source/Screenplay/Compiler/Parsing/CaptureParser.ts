// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CaptureAppendSyntax, CaptureChildrenSyntax, CaptureNestedSyntax, CaptureSourceSettingSyntax, CaptureSourceSyntax, CaptureSyntax } from '../Syntax/Captures';
import { pattern } from '../Text/patterns';
import { checkCaptureWhenTokens } from './ConditionTokens';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^capture\\s+([A-Za-z_]\\w*)$');
const appendPattern = pattern('^append\\s+([A-Z]\\w*)$');
const childrenPattern = pattern('^children\\s+([a-z_]\\w*)\\s+identified\\s+by\\s+([\\w.]+)$');
const nestedPattern = pattern('^nested\\s+([\\w.]+)$');

// The port of the C# CaptureParser, narrowed to what a capture reads from and the events it appends. How it
// maps what it reads, and when each event is appended, are not modeled. Guard token coverage is checked;
// the C# compiler remains the authority on their full grammar.
export function parseCapture(context: ParserContext, line: SourceLine): CaptureSyntax {
    const name = header.exec(line.content)?.[1] ?? '';
    let source: CaptureSourceSyntax | null = null;
    let key: string | null = null;
    const appends: CaptureAppendSyntax[] = [];
    const children: CaptureChildrenSyntax[] = [];
    const nested: CaptureNestedSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        switch (firstWord(child.content)) {
            case 'source':
                source = parseSource(context, child);
                break;
            case 'key':
                key = child.content.substring('key'.length).trim();
                break;
            case 'append':
                pushAppend(context, child, appends);
                break;
            case 'children': {
                const match = childrenPattern.exec(child.content);
                const blockAppends = appendsOf(context, child);
                if (match !== null) {
                    children.push({ kind: 'CaptureChildrenSyntax', property: match[1], identifiedBy: match[2], appends: blockAppends, location: locationOf(child) });
                }
                break;
            }
            case 'nested': {
                const match = nestedPattern.exec(child.content);
                const blockAppends = appendsOf(context, child);
                if (match !== null) {
                    nested.push({ kind: 'CaptureNestedSyntax', property: match[1], appends: blockAppends, location: locationOf(child) });
                }
                break;
            }
            default:
                context.skipOpaqueBlock(child.indent);
        }
    }
    return { kind: 'CaptureSyntax', name, source, key, appends, children, nested, location: locationOf(line) };
}

function parseSource(context: ParserContext, line: SourceLine): CaptureSourceSyntax {
    const settings: CaptureSourceSettingSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const settingName = firstWord(child.content);
        settings.push({ kind: 'CaptureSourceSettingSyntax', name: settingName, value: child.content.substring(settingName.length).trim(), location: locationOf(child) });
    }
    return { kind: 'CaptureSourceSyntax', syntaxKind: line.content.substring('source'.length).trim(), settings, location: locationOf(line) };
}

// The appends of a 'children' or 'nested' block; its 'map' is skipped.
function appendsOf(context: ParserContext, line: SourceLine): CaptureAppendSyntax[] {
    const appends: CaptureAppendSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'append') {
            pushAppend(context, child, appends);
        } else {
            context.skipOpaqueBlock(child.indent);
        }
    }
    return appends;
}

function pushAppend(context: ParserContext, line: SourceLine, appends: CaptureAppendSyntax[]): void {
    const match = appendPattern.exec(line.content);
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'when') checkCaptureWhenTokens(context, child);
        context.skipOpaqueBlock(child.indent);
    }
    if (match !== null) {
        appends.push({ kind: 'CaptureAppendSyntax', event: match[1], location: locationOf(line) });
    }
}

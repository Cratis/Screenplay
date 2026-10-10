// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { ReadsSyntax } from '../Syntax/ReadsSyntax';
import { pattern } from '../Text/patterns';
import { captureReads } from './DependencySourceParser';
import { parseMappingSource } from './ExpressionParser';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const partPattern = pattern('^([a-z_]\\w*)\\s*=\\s*(.+)$');

export function parseReads(context: ParserContext, line: SourceLine): ReadsSyntax | undefined {
    const read = captureReads(line);
    if (read === undefined) {
        context.skipOpaqueBlock(line.indent);
        return undefined;
    }
    const parts: PropertyMappingSyntax[] = [];
    let hasBlock = false;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (child.content !== 'by' || hasBlock || read.by !== null) {
            context.error(DiagnosticCodes.ReadsWithChildren, 'A reads line allows only one by child block, without a single by value.', locationOf(child));
            context.skipBlock(child.indent);
            continue;
        }
        hasBlock = true;
        for (let part = context.peekChild(child.indent); part !== undefined; part = context.peekChild(child.indent)) {
            context.reader.takeSignificant();
            const match = partPattern.exec(part.content);
            if (match === null) {
                context.error(DiagnosticCodes.InvalidReadModelKeyLookup, "A reads by part must be '<part> = <source>'.", locationOf(part));
                context.skipBlock(part.indent);
                continue;
            }
            const offset = part.content.indexOf('=') + 1;
            const sourceOffset = offset + part.content.substring(offset).length - part.content.substring(offset).trimStart().length;
            parts.push({ kind: 'PropertyMappingSyntax', property: match[1], source: parseMappingSource(match[2], { ...locationOf(part), column: part.indent + 1 + sourceOffset }, context.valueContext), location: locationOf(part) });
        }
    }
    if (hasBlock && (parts.length < 2 || new Set(parts.map(part => part.property)).size !== parts.length)) {
        context.error(DiagnosticCodes.InvalidReadModelKeyLookup, 'A reads by block requires at least two distinct named key parts.', locationOf(line));
    }
    return { ...read, ...(hasBlock ? { byParts: parts } : {}) };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ObserverFilterSyntax } from '../Syntax/ObserverFilterSyntax';
import { sourceStreamPattern } from '../Text/SourceStreamNames';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const header = sourceStreamPattern('^from\\s+([A-Za-z_]\\w*)(?:\\.([A-Za-z_]\\w*))?$');

export function parseObserverFilter(context: ParserContext, line: SourceLine, previous: ObserverFilterSyntax | null): ObserverFilterSyntax | null {
    const match = header.exec(line.content);
    if (match === null || previous !== null) {
        context.error(DiagnosticCodes.InvalidObserverFilter, "Declare at most one 'from <Source>[.<Stream>]' observer filter.", locationOf(line));
    } else {
        previous = { kind: 'ObserverFilterSyntax', eventSource: match[1], stream: match[2] ?? null, location: locationOf(line) };
    }
    const child = context.peekChild(line.indent);
    if (child !== undefined) {
        context.error(DiagnosticCodes.InvalidObserverFilter, 'An observer filter cannot have children or filter stream ids.', locationOf(child));
        context.skipBlock(line.indent);
    }
    return previous;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { SinkSyntax } from '../Syntax/Sinks';
import { pattern } from '../Text/patterns';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

export const sinkKeyword = 'sink';

const declarationPattern = pattern('^sink\\s+([A-Za-z_]\\w*)\\s+for\\s+(readmodels|graphs)$');
const directivePattern = pattern('^sink\\s+([A-Za-z_]\\w*)$');
const primitiveTypes = new Set(['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime']);

// 'sink <Name> for readmodels|graphs' at the application root - the port of the C# SinkParser.ParseDeclaration.
export function parseSinkDeclaration(context: ParserContext, line: SourceLine): SinkSyntax | undefined {
    const match = declarationPattern.exec(line.content);
    const child = context.peekChild(line.indent);
    if (child !== undefined) {
        context.error(DiagnosticCodes.InvalidSinkDeclaration, `A sink is declared on one line; '${child.content}' has no place under it`, locationOf(child));
        context.skipBlock(line.indent);
    }
    if (match === null) {
        context.error(DiagnosticCodes.InvalidSinkDeclaration, `Invalid sink declaration '${line.content}' - expected 'sink <Name> for readmodels' or 'sink <Name> for graphs'`, locationOf(line));
        return undefined;
    }
    return { kind: 'SinkSyntax', name: match[1], stores: match[2] === 'graphs' ? 'Graphs' : 'ReadModels', location: locationOf(line) };
}

// A read-model line is the directive only when it is 'sink' and one name that is not a primitive type; anything else
// stays a property named 'sink' ('sink String', 'sink Money[]', '@sink Money').
export function isReadModelSinkDirective(content: string): boolean {
    if (firstWord(content) !== sinkKeyword) return false;
    if (content === sinkKeyword) return true;
    const match = directivePattern.exec(content);
    return match !== null && !primitiveTypes.has(match[1]);
}

// The state of an owner's 'sink' lines while its body is read.
export interface SinkDirectiveState {
    sink?: string;
    location?: SourceLocation;
}

// 'sink <Name>' in a read model or graph body - the port of the C# SinkParser.ParseDirective.
export function parseSinkDirective(context: ParserContext, line: SourceLine, state: SinkDirectiveState, owner: string): void {
    const match = directivePattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidSinkDeclaration, `Invalid sink directive '${line.content}' - expected 'sink <Name>'`, locationOf(line));
        return;
    }
    const name = match[1];
    if (state.sink === undefined) {
        state.sink = name;
        state.location = locationOf(line);
        return;
    }
    if (state.sink === name) {
        context.warning(DiagnosticCodes.RepeatedSinkDirective, `${owner} repeats 'sink ${name}'; keep one directive.`, locationOf(line));
    } else {
        context.error(DiagnosticCodes.ConflictingSinkDirective, `${owner} names sink '${state.sink}' and sink '${name}'; it is stored in one sink.`, locationOf(line));
    }
}

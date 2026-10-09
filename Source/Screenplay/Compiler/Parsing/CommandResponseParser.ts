// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CommandResponseSyntax, ResponseFieldSyntax } from '../Syntax/Responses';
import { pattern } from '../Text/patterns';
import { unescapeIdentifier } from './LineText';
import { ParserContext } from './ParserContext';
import { parseTypeRef, reportLegacyOptionalSuffix, tryParseProperty } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

export const scalarResponsePattern = pattern('^returns\\s+(@?[A-Za-z_]\\w*)$');
const fieldPattern = pattern('^(@?[a-z_]\\w*)(?:\\s+([\\w.]+(?:\\[\\])?(?:\\?|\\s+optional)?))?\\s*=(?!=|>)\\s*(@?[a-z_]\\w*)$');

export function parseCommandResponse(context: ParserContext, line: SourceLine): CommandResponseSyntax | null {
    const location = locationOf(line);
    if (line.content !== 'returns') {
        const match = scalarResponsePattern.exec(line.content);
        if (match === null) {
            context.error(DiagnosticCodes.InvalidCommandResponse, "Expected 'returns <property>' or an unconditional 'returns' block.", location);
            context.skipBlock(line.indent);
            return null;
        }
        const child = context.peekChild(line.indent);
        if (child !== undefined) {
            context.error(DiagnosticCodes.InvalidCommandResponse, 'A scalar response cannot have child directives.', locationOf(child));
            context.skipBlock(line.indent);
        }
        // The operand capture ends the anchored match, so its offset cannot resolve inside 'returns'.
        const sourceOffset = match.index + match[0].length - match[1].length;
        return { kind: 'ScalarCommandResponseSyntax', source: { kind: 'PropertyResponseSourceSyntax', property: unescapeIdentifier(match[1]), location: { ...location, column: location.column + sourceOffset } }, location };
    }
    const fields: ResponseFieldSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const match = fieldPattern.exec(child.content);
        if (match === null) {
            const candidate = tryParseProperty({ ...child, content: child.content.split('=', 1)[0].trimEnd() });
            if (candidate?.isSubject) context.error(DiagnosticCodes.InvalidSubjectOwner, 'The subject modifier is only valid on event properties, not response fields (decision 0008: one data subject per event).', locationOf(child));
            else context.error(DiagnosticCodes.InvalidCommandResponse, "Expected '<field> [<Type>] = <property>' in a response block.", locationOf(child));
            context.skipBlock(child.indent);
            continue;
        }
        const fieldLocation = locationOf(child);
        const type = match[2] === undefined ? null : parseTypeRef(match[2], { ...fieldLocation, column: fieldLocation.column + child.content.indexOf(match[2], match[1].length) });
        if (type !== null) reportLegacyOptionalSuffix(context, type, child);
        fields.push({ kind: 'ResponseFieldSyntax', name: unescapeIdentifier(match[1]), type, source: { kind: 'PropertyResponseSourceSyntax', property: unescapeIdentifier(match[3]), location: { ...fieldLocation, column: fieldLocation.column + child.content.lastIndexOf(match[3]) } }, location: fieldLocation });
        const nested = context.peekChild(child.indent);
        if (nested !== undefined) {
            context.error(DiagnosticCodes.InvalidCommandResponse, 'A response field cannot have child directives.', locationOf(nested));
            context.skipBlock(child.indent);
        }
    }
    return { kind: 'RecordCommandResponseSyntax', fields, location };
}

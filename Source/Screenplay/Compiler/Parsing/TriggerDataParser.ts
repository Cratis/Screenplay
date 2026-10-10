// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PropertySyntax } from '../Syntax/Declarations';
import { pattern } from '../Text/patterns';
import { parseDescription } from './DescriptionParser';
import { isFileDirective } from './FileReferences';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseProperty } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

const typedValue = pattern('^@?[a-z_]\\w*\\s+[\\w.]+');

// Trigger values are kept as parser-owned diagnostic metadata, outside the syntax wire projection. Recognize their type spelling in
// exactly the C# data positions, without scanning descriptions, attachments, code or mapping expressions.
export function parseTriggerData(context: ParserContext, line: SourceLine): void {
    const property = parseProperty(context, line);
    if (property !== undefined) {
        if (property.isKey) context.error(DiagnosticCodes.InvalidReadModelKey, 'The key modifier is only valid on top-level read-model properties.', property.location);
        if (property.isGenerated) context.error(DiagnosticCodes.GeneratedPropertyOutsideCommand, 'Generated properties can only be declared on commands.', property.location);
        if (property.isSubject) context.error(DiagnosticCodes.InvalidSubjectOwner, 'The subject modifier is only valid on event properties, not trigger or reaction data properties (decision 0008: one data subject per event).', property.location);
        context.triggerData.push(property);
        return;
    }
    // Keep the existing opaque projection for other trigger values and implementation bodies.
    // Only optionality-shaped failures belong to this spelling change.
    if (typedValue.test(line.content) && /\?|\boptional\b/.test(line.content)) {
        context.error(DiagnosticCodes.InvalidTriggerData, `Invalid trigger value '${line.content}' - expected '<name>' or '<name> <Type>'`, locationOf(line));
    }
    context.skipOpaqueBlock(line.indent);
}

export function parseTriggerDeclaration(context: ParserContext, header: SourceLine): readonly PropertySyntax[] {
    const start = context.triggerData.length;
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'description') {
            parseDescription(context, line, null, `Trigger '${header.content}'`);
        } else if (!isFileDirective(line)) {
            parseTriggerData(context, line);
        }
    }
    return context.triggerData.slice(start);
}

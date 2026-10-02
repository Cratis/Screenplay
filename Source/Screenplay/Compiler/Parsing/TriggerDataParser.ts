// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { pattern } from '../Text/patterns';
import { parseDescription } from './DescriptionParser';
import { isFileDirective } from './FileReferences';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseProperty } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

const typedValue = pattern('^@?[a-z_]\\w*\\s+[\\w.]+');

// Trigger values are not part of the TypeScript syntax projection. Recognize their type spelling in
// exactly the C# data positions, without scanning descriptions, attachments, code or mapping expressions.
export function parseTriggerData(context: ParserContext, line: SourceLine): void {
    const property = parseProperty(context, line);
    if (property !== undefined) {
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

export function parseTriggerDeclaration(context: ParserContext, header: SourceLine): void {
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'description') {
            parseDescription(context, line, null, `Trigger '${header.content}'`);
        } else if (!isFileDirective(line)) {
            parseTriggerData(context, line);
        }
    }
}

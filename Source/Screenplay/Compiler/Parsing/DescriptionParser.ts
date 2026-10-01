// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { pattern } from '../Text/patterns';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { parseFencedText } from './CodeBlockParser';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const descriptionPattern = pattern(`^description\\s+"(${stringBodyPattern})"$`);

// Reads 'description "<text>"' or a 'description' followed by a ```text fence. A construct keeps its first
// description; a second one is reported and ignored.
export function parseDescription(context: ParserContext, line: SourceLine, existing: string | null, owner: string): string | null {
    if (line.content === 'description') {
        const text = parseFencedText(context, 'description', line);
        if (text === null) {
            return existing;
        }
        if (text.trim().length === 0) {
            context.error(DiagnosticCodes.EmptyDescription, `${owner} declares an empty description - the fenced block must contain text`, locationOf(line));
            return existing;
        }
        return keep(context, line, existing, owner, text);
    }
    const match = descriptionPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidDescription, `Invalid description '${line.content}' - expected 'description "<text>"'`, locationOf(line));
        return existing;
    }
    return keep(context, line, existing, owner, unescapeString(match[1]));
}

function keep(context: ParserContext, line: SourceLine, existing: string | null, owner: string, description: string): string | null {
    if (existing !== null) {
        context.error(DiagnosticCodes.DuplicateDescription, `${owner} already declares a description - at most one is allowed`, locationOf(line));
        return existing;
    }
    return description;
}

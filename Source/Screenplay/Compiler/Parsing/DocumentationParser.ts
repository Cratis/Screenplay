// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { parseFencedText } from './CodeBlockParser';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

export function parseDocumentation(context: ParserContext, line: SourceLine, existing: string | null, owner: string, code: string = DiagnosticCodes.InvalidDocumentation): string | null {
    if (line.content !== 'documentation' || context.peekChild(line.indent)?.content !== '```markdown') {
        context.error(code, `${owner} documentation requires a fenced markdown block`, locationOf(line));
        context.skipOpaqueBlock(line.indent);
        return existing;
    }
    const text = parseFencedText(context, 'markdown', line);
    if (text === null || text.trim().length === 0 || existing !== null) {
        context.error(code, `${owner} accepts one nonempty documentation block`, locationOf(line));
        return existing;
    }
    return text;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

// Reads the body of a fenced block that follows a directive, the way the C# CodeBlockParser does: the
// opening fence is either on the directive's own line or the next deeper line, and the body runs to a line
// holding only the closing fence. Body lines lose up to the opening fence's indent.
export function parseFencedText(context: ParserContext, opener: string, tagLine: SourceLine): string | null {
    const onTagLine = tagLine.content.startsWith('```');
    const open = onTagLine ? tagLine : context.reader.peekSignificant();
    const expectedFence = opener === 'description' ? '```text' : `\`\`\`${opener}`;
    if (open === undefined || (open !== tagLine && open.indent <= tagLine.indent) ||
        (open.content !== expectedFence && !(opener === 'description' && open.content === '```markdown') && !(open.content === '```' && open !== tagLine))) {
        context.error(DiagnosticCodes.ExpectedCodeFence, `Expected an opening \`\`\`${opener === 'description' ? 'text' : opener} fence after '${opener}'`, locationOf(tagLine));
        return null;
    }
    if (open !== tagLine) {
        context.reader.takeSignificant();
    }
    if (opener === 'description' && open.content === '```') {
        context.warning(DiagnosticCodes.LegacyInlineCodeFence, 'A bare description fence is deprecated - use \'```text\' instead', locationOf(open));
    }
    const code: string[] = [];
    for (;;) {
        const line = context.reader.takeRaw();
        if (line === undefined) {
            context.error(DiagnosticCodes.UnclosedCodeBlock, 'Unclosed inline code block - expected a closing ``` line', locationOf(open));
            break;
        }
        if (line.raw.trim() === '```') {
            break;
        }
        let strip = 0;
        while (strip < open.indent && strip < line.raw.length && line.raw[strip] === ' ') {
            strip++;
        }
        code.push(line.raw.substring(strip));
    }
    return code.join('\n');
}

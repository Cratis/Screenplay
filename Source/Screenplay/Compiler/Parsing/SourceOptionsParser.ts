// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { exactSourceOptions, legacySourceOptions, NumericMode, SourceOptions } from '../Syntax/SourceOptions';
import { LineReader } from './LineReader';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';
import { commentStart } from './SourceLineSplitter';

// Establishes options before entering any value grammar. This mirrors the C# physical-file prepass;
// fences and property owners are not reinterpreted as numeric directives.
export function sourceContext(lines: readonly SourceLine[], path?: string, languages?: ReadonlySet<string>, hashComments = false): ParserContext {
    const prepared: SourceLine[] = [];
    const diagnostics: Diagnostic[] = [];
    let options: SourceOptions = legacySourceOptions;
    let seen = false;
    let contentSeen = false;
    let inFence = false;
    for (const original of lines) {
        let line = original;
        if (line.number === 1 && line.raw.startsWith('\uFEFF')) {
            const raw = line.raw.substring(1);
            const indent = raw.length - raw.trimStart().length;
            const comment = commentStart(raw.substring(indent), hashComments);
            line = { ...line, indent, contentOffset: 1, content: (comment < 0 ? raw.substring(indent) : raw.substring(indent, indent + comment)).trimEnd() };
        }
        if (line.content.startsWith('```')) inFence = !inFence;
        if (!inFence && line.indent === 0 && firstWord(line.content) === 'numbers') {
            const code = line.content !== 'numbers exact' ? DiagnosticCodes.InvalidNumericDirective : seen ? DiagnosticCodes.DuplicateNumericDirective : contentSeen ? DiagnosticCodes.LateNumericDirective : undefined;
            if (code !== undefined) {
                diagnostics.push({ severity: 'error', code, message: "Expected one 'numbers exact' preamble before domain, imports and declarations.", location: locationOf(line) });
                options = Object.freeze({ numericMode: 'invalid' as NumericMode });
            } else options = exactSourceOptions;
            seen = true;
            prepared.push({ ...line, content: '' });
            continue;
        }
        contentSeen ||= line.content.trim().length > 0;
        prepared.push(line);
    }
    const context = new ParserContext(new LineReader(seen ? prepared : lines), path, languages);
    context.sourceOptions = options;
    diagnostics.forEach(diagnostic => context.error(diagnostic.code, diagnostic.message, diagnostic.location));
    return context;
}

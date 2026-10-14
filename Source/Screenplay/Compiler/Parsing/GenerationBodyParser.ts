// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { GenerationSyntax } from '../Syntax/GenerationSyntax';
import { isFile, parseCode, parseFile } from './ImplementationParser';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

export function parseGenerationBody(context: ParserContext, line: SourceLine, generation: GenerationSyntax): GenerationSyntax {
    let hasBody = false;
    let child: SourceLine | undefined;
    while ((child = context.peekChild(line.indent)) !== undefined) {
        context.reader.takeSignificant();
        const file = isFile(child);
        const fence = child.content.startsWith('```');
        if (generation.strategy !== 'rule' || (!file && !fence)) {
            context.error(DiagnosticCodes.UnknownRuleImplementationDirective, "Generation rule bodies accept only one 'file <path>' or tagged inline fence.", locationOf(child));
            context.skipBlock(child.indent);
            continue;
        }
        if (hasBody) context.error(DiagnosticCodes.ConflictingImplementationSources, 'A generation rule has exactly one file or inline payload.', locationOf(child));
        const parsedFile = file ? parseFile(context, child) : null;
        const parsedCode = fence ? parseCode(context, child) : null;
        if (!hasBody) generation = { ...generation, file: parsedFile, code: parsedCode };
        hasBody = true;
        const nested = file ? context.peekChild(child.indent) : undefined;
        if (nested !== undefined) {
            context.error(DiagnosticCodes.UnknownRuleImplementationDirective, 'A generation file directive cannot have children.', locationOf(nested));
            context.skipBlock(child.indent);
        }
    }
    return generation;
}

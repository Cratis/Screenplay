// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { FileImportSyntax } from '../Syntax/Structure';
import { pattern } from '../Text/patterns';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const fileImportPattern = pattern('^import\\s+"([^"\\\\]+)"$');
const quotedPattern = pattern('^import\\s+"');

// Whether an 'import' line names files rather than a qualified name - its operand is quoted.
export const isFileImport = (content: string): boolean => quotedPattern.test(content);

// An 'import "<pattern>"' line, or undefined when the line does not import files - the port of the C#
// FileImportParser.TryParse.
export function tryParseFileImport(line: SourceLine): FileImportSyntax | undefined {
    const match = fileImportPattern.exec(line.content);
    return match === null ? undefined : { kind: 'FileImportSyntax', pattern: match[1], location: locationOf(line) };
}

// Parses a consumed 'import' line inside a module or feature, where only files can be imported.
export function parseFileImport(context: ParserContext, line: SourceLine, imports: FileImportSyntax[]): void {
    const fileImport = tryParseFileImport(line);
    if (fileImport !== undefined) {
        imports.push(fileImport);
        return;
    }
    context.error(DiagnosticCodes.InvalidFileImport,
        `Invalid import '${line.content}' - inside a module or feature, import names files: 'import "<path or glob>"'`, locationOf(line));
    context.skipBlock(line.indent);
}

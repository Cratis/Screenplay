// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ScreenProvisionMappingSyntax, ScreenProvisionSyntax, UsesScreenSyntax } from '../Syntax/Shapes';
import { pattern } from '../Text/patterns';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const usesScreenWords = pattern('^uses\\s+screen(\\s|$)');
const adoptionHeader = pattern('^uses\\s+screen\\s+([A-Za-z_]\\w*)$');
const provision = pattern('^data\\s+([A-Za-z_]\\w*)\\s+via\\s+query\\s+(\\w+(?:\\.\\w+)*)(?:\\s+by\\s+(\\w+))?$');
const mapping = pattern('^([A-Za-z_]\\w*)\\s+from\\s+([A-Za-z_]\\w*(?:\\.[A-Za-z_]\\w*)*)$');

// Whether a slice line starts an adoption: 'uses screen ...'.
export const isScreenAdoption = (content: string): boolean => usesScreenWords.test(content);

// Parses 'uses screen <Name>' and the data the slice provides to the screen's needs, from the already consumed
// header line. The port of the C# ScreenAdoptionParser.
export function parseScreenAdoption(context: ParserContext, header: SourceLine): UsesScreenSyntax {
    const match = adoptionHeader.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidScreenAdoption, `Invalid screen adoption '${header.content}' - expected 'uses screen <Name>'`, locationOf(header));
    }
    const provisions: ScreenProvisionSyntax[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const parsed = provision.exec(line.content);
        if (parsed === null) {
            context.error(DiagnosticCodes.InvalidScreenAdoption, `Invalid provision '${line.content}' - expected 'data <Need> via query <Query> [by <param>]'`, locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        provisions.push({ kind: 'ScreenProvisionSyntax', need: parsed[1], query: parsed[2], by: parsed[3] ?? null, mappings: parseMappings(context, line), location: locationOf(line) });
    }
    return { kind: 'UsesScreenSyntax', screen: match?.[1] ?? '', provisions, location: locationOf(header) };
}

function parseMappings(context: ParserContext, provisionLine: SourceLine): ScreenProvisionMappingSyntax[] {
    const mappings: ScreenProvisionMappingSyntax[] = [];
    for (let line = context.peekChild(provisionLine.indent); line !== undefined; line = context.peekChild(provisionLine.indent)) {
        context.reader.takeSignificant();
        const parsed = mapping.exec(line.content);
        if (parsed === null) {
            context.error(DiagnosticCodes.InvalidScreenAdoption, `Invalid mapping '${line.content}' - expected '<field> from <path>'`, locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        mappings.push({ kind: 'ScreenProvisionMappingSyntax', field: parsed[1], path: parsed[2], location: locationOf(line) });
    }
    return mappings;
}

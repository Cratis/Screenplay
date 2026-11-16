// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PropertySyntax } from '../Syntax/Declarations';
import { ShapeSyntax } from '../Syntax/Shapes';
import { pattern } from '../Text/patterns';
import { parseDescription } from './DescriptionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseProperty, tryParseProperty } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

const shapeHeader = pattern('^shape\\s+([A-Za-z_]\\w*)$');

// Parses a top-level 'shape' declaration from its already consumed header line: a name and typed fields, like a
// read model without a builder. The port of the C# ShapeParser.
export function parseShape(context: ParserContext, header: SourceLine): ShapeSyntax {
    const match = shapeHeader.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidShapeDeclaration, `Invalid shape declaration '${header.content}' - expected 'shape <Name>'`, locationOf(header));
    }
    const name = match?.[1] ?? '';
    const properties: PropertySyntax[] = [];
    let description: string | null = null;
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'description' && tryParseProperty(line) === undefined) {
            description = parseDescription(context, line, description, `Shape '${name}'`);
            continue;
        }
        const property = parseProperty(context, line);
        if (property === undefined) {
            context.error(DiagnosticCodes.InvalidShapeDeclaration, `Invalid shape field '${line.content}' - expected '<name> <Type>'`, locationOf(line));
            continue;
        }
        if (property.isIdentifier || property.isKey === true || property.isGenerated === true || property.isSubject === true) {
            context.error(DiagnosticCodes.InvalidShapeDeclaration, `Shape field '${property.name}' cannot carry a key, identifier, subject or generated modifier - a shape is a contract on data, not a stored model`, locationOf(line));
            properties.push({ ...property, isIdentifier: false, isKey: false, isGenerated: false, isSubject: false });
            continue;
        }
        properties.push(property);
    }
    return { kind: 'ShapeSyntax', name, properties, description, location: locationOf(header) };
}

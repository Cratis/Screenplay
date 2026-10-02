// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { PropertySyntax, TypeRefSyntax } from '../Syntax/Declarations';
import { pattern } from '../Text/patterns';
import { ParserContext } from './ParserContext';
import { unescapeIdentifier } from './LineText';
import { locationOf, SourceLine } from './SourceLine';

const propertyPattern = pattern('^(@?[a-z_]\\w*)\\s+([\\w.]+(?:\\[\\])?(?:\\?|\\s+optional)?)(?:\\s+(identifier))?$');
const reversedModifiers = pattern('^@?[a-z_]\\w*\\s+[\\w.]+(?:\\[\\])?\\s+identifier\\s+optional(?:\\s*=.*)?$');

// A '<name> <Type>[[]][?] [identifier]' line, or undefined when the line does not have that shape.
export function tryParseProperty(line: SourceLine): PropertySyntax | undefined {
    const match = propertyPattern.exec(line.content);
    if (match === null) {
        return undefined;
    }
    const location = locationOf(line);
    return {
        kind: 'PropertySyntax',
        name: unescapeIdentifier(match[1]),
        type: parseTypeRef(match[2], { ...location, column: location.column + line.content.indexOf(match[2], match[1].length) }),
        isIdentifier: match[3] !== undefined,
        location,
    };
}

export function parseTypeRef(text: string, location: SourceLocation): TypeRefSyntax {
    const legacy = text.endsWith('?');
    const canonical = /\soptional$/.test(text);
    const isOptional = legacy || canonical;
    if (isOptional) {
        text = text.substring(0, text.length - (legacy ? 1 : 'optional'.length)).trimEnd();
    }
    const isCollection = text.endsWith('[]');
    if (isCollection) {
        text = text.substring(0, text.length - 2);
    }
    return { kind: 'TypeRefSyntax', name: text, isCollection, isOptional, location };
}

// Unlike tryParseProperty, this is only called once the owner commits to a property position.
export function parseProperty(context: ParserContext, line: SourceLine): PropertySyntax | undefined {
    const property = tryParseProperty(line);
    if (property !== undefined) {
        reportLegacyOptionalSuffix(context, property.type, line);
    } else {
        reportInvalidModifierOrder(context, line);
    }
    return property;
}

export function reportLegacyOptionalSuffix(context: ParserContext, type: TypeRefSyntax, line: SourceLine): void {
    const length = type.name.length + (type.isCollection ? 2 : 0);
    const offset = type.location.column - line.indent - 1 + length;
    if (type.isOptional && line.content[offset] === '?') {
        const name = type.name + (type.isCollection ? '[]' : '');
        context.information(DiagnosticCodes.LegacyOptionalSuffix, `Write '${name} optional' instead of '${name}?'.`, type.location);
    }
}

export function reportInvalidModifierOrder(context: ParserContext, line: SourceLine): void {
    if (reversedModifiers.test(line.content)) {
        context.error(DiagnosticCodes.InvalidOptionalModifierOrder, "Write 'optional' before 'identifier': '<name> <Type> optional identifier'.", locationOf(line));
    }
}

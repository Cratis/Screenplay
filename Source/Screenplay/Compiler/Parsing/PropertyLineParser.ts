// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { PropertySyntax, TypeRefSyntax } from '../Syntax/Declarations';
import { pattern } from '../Text/patterns';
import { ParserContext } from './ParserContext';
import { unescapeIdentifier } from './LineText';
import { locationOf, SourceLine } from './SourceLine';

const propertyPattern = pattern('^(@?[a-z_]\\w*)\\s+([\\w.]+(?:\\[\\])?(?:\\?|\\s+optional)?)(?:\\s+(generated))?(?:\\s+(identifier))?(?:\\s+(subject))?(?:\\s+(key))?$');
const invalidGeneratedModifiers = pattern('^@?[a-z_]\\w*\\s+[\\w.]+(?:\\[\\])?\\??\\s+((?:optional|generated|identifier|subject|key)(?:\\s+(?:optional|generated|identifier|subject|key))*)$');
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
        isGenerated: match[3] !== undefined,
        nameWasEscaped: match[1].startsWith('@'),
        isIdentifier: match[4] !== undefined,
        ...(match[5] !== undefined ? { isSubject: true } : {}),
        ...(match[6] !== undefined ? { isKey: true } : {}),
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
        if (property.isKey && (property.type.isOptional || property.type.isCollection || property.isGenerated || property.isIdentifier || property.isSubject)) {
            context.error(DiagnosticCodes.InvalidReadModelKey, 'A read-model key part must be required, not a collection, and cannot combine key with generated, identifier or subject.', locationOf(line));
        }
    } else {
        reportInvalidModifierOrder(context, line);
    }
    return property;
}

export function reportLegacyOptionalSuffix(context: ParserContext, type: TypeRefSyntax, line: SourceLine): void {
    context.claimField(line);
    const length = type.name.length + (type.isCollection ? 2 : 0);
    const offset = type.location.column - line.indent - 1 + length;
    if (type.isOptional && line.content[offset] === '?') {
        const name = type.name + (type.isCollection ? '[]' : '');
        context.information(DiagnosticCodes.LegacyOptionalSuffix, `Write '${name} optional' instead of '${name}?'.`, type.location);
    }
}

export function reportInvalidModifierOrder(context: ParserContext, line: SourceLine): boolean {
    const keyModifiers = invalidGeneratedModifiers.exec(line.content);
    if (keyModifiers !== null && keyModifiers[1].split(/\s+/).includes('key')) {
        context.error(DiagnosticCodes.InvalidReadModelKey, "Write 'key' once, without other modifiers: '<name> <Type> key'.", locationOf(line));
        return true;
    }
    const subjectModifiers = invalidGeneratedModifiers.exec(line.content);
    if (subjectModifiers !== null && subjectModifiers[1].split(/\s+/).includes('subject')) {
        context.error(DiagnosticCodes.InvalidSubjectModifierOrder, "Write each modifier once in order: '<name> <Type> optional generated identifier subject'.", locationOf(line));
        return true;
    }
    if (reversedModifiers.test(line.content)) {
        context.error(DiagnosticCodes.InvalidOptionalModifierOrder, "Write 'optional' before 'identifier': '<name> <Type> optional identifier'.", locationOf(line));
        return true;
    } else {
        const modifiers = invalidGeneratedModifiers.exec(line.content);
        if (modifiers !== null && modifiers[1].split(/\s+/).some(modifier => modifier === 'generated' || modifier === 'identifier')) {
            context.error(DiagnosticCodes.InvalidGeneratedModifierOrder, "Write each modifier once in order: '<name> <Type> optional generated identifier'.", locationOf(line));
            return true;
        }
    }
    return false;
}

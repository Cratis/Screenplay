// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { PropertySyntax, TypeRefSyntax } from '../Syntax/Declarations';
import { pattern } from '../Text/patterns';
import { unescapeIdentifier } from './LineText';
import { locationOf, SourceLine } from './SourceLine';

const propertyPattern = pattern('^(@?[a-z_]\\w*)\\s+([\\w.]+(?:\\[\\])?\\??)(?:\\s+(identifier))?$');

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
        type: parseTypeRef(match[2], location),
        isIdentifier: match[3] !== undefined,
        location,
    };
}

export function parseTypeRef(text: string, location: SourceLocation): TypeRefSyntax {
    const isOptional = text.endsWith('?');
    if (isOptional) {
        text = text.substring(0, text.length - 1);
    }
    const isCollection = text.endsWith('[]');
    if (isCollection) {
        text = text.substring(0, text.length - 2);
    }
    return { kind: 'TypeRefSyntax', name: text, isCollection, isOptional, location };
}

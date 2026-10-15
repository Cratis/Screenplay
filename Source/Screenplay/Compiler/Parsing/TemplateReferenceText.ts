// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { pattern } from '../Text/patterns';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';

// 'template blueprint <Blueprint> <Name> [version "<range>"]': a template a blueprint package ships, with the
// semver range of the blueprint the author accepts. The port of the C# TemplateReferenceText.
const blueprintReference = pattern(`^blueprint\\s+([A-Za-z_]\\w*(?:\\.\\w+)*)\\s+([A-Za-z_]\\w*(?:\\.\\w+)*)(?:\\s+version\\s+"(${stringBodyPattern})")?$`);

const rangeOperators = ['>=', '<=', '^', '~', '>', '<', '='];

export interface TemplateReferenceParts {
    readonly blueprint: string;
    readonly name: string;
    readonly version: string | null;
}

// Whether the text after 'template' starts the blueprint form, whether or not it is well formed.
export function startsBlueprintReference(text: string): boolean {
    return text.startsWith('blueprint ');
}

// Reads the blueprint form, or undefined when it is malformed.
export function parseBlueprintReference(text: string): TemplateReferenceParts | undefined {
    const match = blueprintReference.exec(text);
    if (match === null) return undefined;
    return { blueprint: match[1], name: match[2], version: match[3] === undefined ? null : unescapeString(match[3]) };
}

// Whether a template reference's version is a range Scene understands - the same subset as Scene's
// isValidVersionRange and PackageVersionRange.IsValidRange. Whether the installed blueprint satisfies it is
// decided where the blueprint is known, in Stage and Scene.
export function isValidTemplateVersionRange(range: string): boolean {
    const trimmed = range.trim();
    if (trimmed.length === 0 || trimmed === '*') return true;
    let literal = trimmed;
    for (const operator of rangeOperators) {
        if (trimmed.startsWith(operator)) {
            literal = trimmed.substring(operator.length).trim();
            break;
        }
    }
    const suffix = literal.search(/[-+]/);
    const numeric = (suffix >= 0 ? literal.substring(0, suffix) : literal).trim();
    const parts = numeric.split('.');
    return parts.length >= 1 && parts.length <= 3 && parts.every(part => /^[0-9]+$/.test(part));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { responseAnalysis } from './response-analysis';
import { withoutComment } from './document-context';

export const responseTokenTypes = ['keyword', 'property', 'type'] as const;

// Semantic overlays resolve the local returns ambiguity that a line-based grammar cannot.
export function responseTokens(lines: string[]): { line: number; column: number; length: number; type: number }[] {
    const tokens: { line: number; column: number; length: number; type: number }[] = [];
    const add = (line: number, column: number, length: number, type: number) => {
        if (line > 0 && length > 0) tokens.push({ line: line - 1, column: column - 1 + (lines[line - 1][column - 1] === '@' ? 1 : 0), length, type });
    };
    const analysis = responseAnalysis(lines);
    for (const command of analysis.commands.values()) {
        for (const property of command.properties) {
            if (!property.isGenerated) continue;
            const source = withoutComment(lines[property.location.line - 1]);
            add(property.location.line, source.lastIndexOf('generated') + 1, 9, 0);
        }
        const response = command.response;
        if (!response) continue;
        add(response.location.line, response.location.column, 7, 0);
        if (response.kind === 'ScalarCommandResponseSyntax') {
            add(response.source.location.line, response.source.location.column, response.source.property.length, 1);
        } else for (const field of response.fields) {
            add(field.location.line, field.location.column, field.name.length, 1);
            add(field.source.location.line, field.source.location.column, field.source.property.length, 1);
            if (field.type) add(field.type.location.line, field.type.location.column, field.type.name.length, 2);
        }
    }
    for (const specification of analysis.specifications.values()) {
        for (const fixture of specification.when?.generatedValues ?? []) {
            add(fixture.location.line, fixture.location.column, 9, 0);
        }
        if (specification.thenReturns) add(specification.thenReturns.location.line, lines[specification.thenReturns.location.line - 1].indexOf('returns') + 1, 7, 0);
    }
    return tokens.sort((left, right) => left.line - right.line || left.column - right.column);
}

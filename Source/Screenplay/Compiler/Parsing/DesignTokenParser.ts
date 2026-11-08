// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { designCategories } from '../Design/DesignVocabulary';
import { DesignTokenCategorySyntax, DesignTokenSyntax } from '../Syntax/DesignSystemSyntax';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { ParserContext } from './ParserContext';
import { SourceLine, locationOf } from './SourceLine';

const tokenPattern = new RegExp(`^(?<name>[A-Za-z_][\\w\\-]*)(?:\\s+"(?<description>${stringBodyPattern})")?$`);

// Parses the `tokens` block of a design system: the categories, and in each the token roles with an optional description.
export function parseDesignTokens(context: ParserContext, header: SourceLine, categories: DesignTokenCategorySyntax[]): void {
    if (header.content !== 'tokens') {
        context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Invalid tokens line '${header.content}' - expected 'tokens' followed by an indented block`, locationOf(header));
    }
    for (let categoryLine = context.peekChild(header.indent); categoryLine !== undefined; categoryLine = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (!designCategories.includes(categoryLine.content)) {
            context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Unknown token category '${categoryLine.content}' - expected one of ${designCategories.join(', ')}`, locationOf(categoryLine));
            context.skipBlock(categoryLine.indent);
            continue;
        }
        const tokens: DesignTokenSyntax[] = [];
        for (let tokenLine = context.peekChild(categoryLine.indent); tokenLine !== undefined; tokenLine = context.peekChild(categoryLine.indent)) {
            context.reader.takeSignificant();
            context.skipBlock(tokenLine.indent);
            const groups = tokenPattern.exec(tokenLine.content)?.groups;
            if (groups === undefined) {
                context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Invalid token '${tokenLine.content}' - expected '<name>' or '<name> "<what it is for>"'`, locationOf(tokenLine));
                continue;
            }
            const name = groups.name;
            const category = categoryLine.content;
            if (tokens.some(existing => existing.name === name) || categories.some(existing => existing.category === category && existing.tokens.some(token => token.name === name))) {
                context.error(DiagnosticCodes.DuplicateDesignToken, `The token '${category}.${name}' is already declared`, locationOf(tokenLine));
                continue;
            }
            const description = groups.description !== undefined ? unescapeString(groups.description) : null;
            tokens.push({ kind: 'DesignTokenSyntax', name, description, location: locationOf(tokenLine) });
        }
        categories.push({ kind: 'DesignTokenCategorySyntax', category: categoryLine.content, tokens, location: locationOf(categoryLine) });
    }
}

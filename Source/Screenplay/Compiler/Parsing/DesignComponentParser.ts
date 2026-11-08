// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { DesignAssignmentSyntax, DesignComponentSyntax, DesignStateSyntax, DesignVariantSyntax } from '../Syntax/DesignSystemSyntax';
import { parseDesignAssignment } from './DesignAssignmentParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { SourceLine, locationOf } from './SourceLine';

const namePattern = /^[A-Za-z_]\w*$/;
const partsPattern = /^parts\s+([A-Za-z_]\w*(?:\s*,\s*[A-Za-z_]\w*)*)$/;
const variantPattern = /^variant\s+([A-Za-z_][\w-]*)$/;
const statePattern = /^state\s+([A-Za-z_]\w*)$/;

// Parses the `components` block of a design system: each component with its parts, and its variants with their states.
export function parseDesignComponents(context: ParserContext, header: SourceLine, components: DesignComponentSyntax[]): void {
    if (header.content !== 'components') {
        context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Invalid components line '${header.content}' - expected 'components' followed by an indented block`, locationOf(header));
    }
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (!namePattern.test(line.content)) {
            context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Invalid component '${line.content}' - expected '<Name>' followed by its parts and variants`, locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        if (components.some(existing => existing.name === line.content)) {
            context.error(DiagnosticCodes.DuplicateDesignToken, `The component '${line.content}' is already declared`, locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        components.push(parseComponent(context, line));
    }
}

function parseComponent(context: ParserContext, header: SourceLine): DesignComponentSyntax {
    const parts: string[] = [];
    const variants: DesignVariantSyntax[] = [];
    let hasParts = false;
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const word = firstWord(line.content);
        if (word === 'parts') {
            context.skipBlock(line.indent);
            const match = partsPattern.exec(line.content);
            if (hasParts || match === null) {
                context.error(DiagnosticCodes.InvalidDesignSystemDeclaration,
                    hasParts ? `Component '${header.content}' already declares its parts` : `Invalid parts line '${line.content}' - expected 'parts <part>, <part>'`, locationOf(line));
                continue;
            }
            hasParts = true;
            parts.push(...match[1].split(',').map(part => part.trim()));
        } else if (word === 'variant') {
            const variant = parseVariant(context, line);
            if (variant === undefined) continue;
            if (variants.some(existing => existing.name === variant.name)) {
                context.error(DiagnosticCodes.DuplicateDesignToken, `The variant '${variant.name}' of '${header.content}' is already declared`, locationOf(line));
                continue;
            }
            variants.push(variant);
        } else {
            context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Unexpected '${word}' in component '${header.content}' - expected parts or variant`, locationOf(line));
            context.skipBlock(line.indent);
        }
    }
    return { kind: 'DesignComponentSyntax', name: header.content, parts, variants, location: locationOf(header) };
}

function parseVariant(context: ParserContext, header: SourceLine): DesignVariantSyntax | undefined {
    const match = variantPattern.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Invalid variant '${header.content}' - expected 'variant <name>'`, locationOf(header));
        context.skipBlock(header.indent);
        return undefined;
    }
    const parts: DesignAssignmentSyntax[] = [];
    const states: DesignStateSyntax[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'state') {
            const state = parseState(context, line);
            if (state !== undefined) states.push(state);
            continue;
        }
        context.skipBlock(line.indent);
        addPart(context, line, parts, `variant '${match[1]}'`);
    }
    return { kind: 'DesignVariantSyntax', name: match[1], parts, states, location: locationOf(header) };
}

function parseState(context: ParserContext, header: SourceLine): DesignStateSyntax | undefined {
    const match = statePattern.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Invalid state '${header.content}' - expected 'state <name>'`, locationOf(header));
        context.skipBlock(header.indent);
        return undefined;
    }
    const parts: DesignAssignmentSyntax[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        context.skipBlock(line.indent);
        addPart(context, line, parts, `state '${match[1]}'`);
    }
    return { kind: 'DesignStateSyntax', name: match[1], parts, location: locationOf(header) };
}

function addPart(context: ParserContext, line: SourceLine, parts: DesignAssignmentSyntax[], owner: string): void {
    const assignment = parseDesignAssignment(line);
    if (assignment === undefined || assignment.isLiteral) {
        context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Invalid part line '${line.content}' in ${owner} - expected '<part> = <category>.<token>'`, locationOf(line));
        return;
    }
    if (parts.some(existing => existing.name === assignment.name)) {
        context.error(DiagnosticCodes.DuplicateDesignToken, `The part '${assignment.name}' is already assigned in ${owner}`, locationOf(line));
        return;
    }
    parts.push(assignment);
}

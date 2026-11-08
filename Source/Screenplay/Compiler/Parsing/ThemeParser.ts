// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { DesignAssignmentSyntax, ThemeModeValuesSyntax, ThemeSyntax } from '../Syntax/DesignSystemSyntax';
import { parseDesignAssignment } from './DesignAssignmentParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { SourceLine, locationOf } from './SourceLine';

const headerPattern = /^theme\s+([A-Za-z_]\w*)$/;
const compatibleWithPattern = /^compatible\s+with\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)$/;
const implementsPattern = /^implements\s+([A-Za-z_]\w*)$/;
const modePattern = /^[A-Za-z_][\w-]*$/;

// Parses a top level `theme <Name>` block: the packages it is compatible with, the design system it implements, its palette, its
// values and a block of values per mode. The port of the C# ThemeParser.
export function parseTheme(context: ParserContext, header: SourceLine): ThemeSyntax {
    const match = headerPattern.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidThemeDeclaration, `Invalid theme declaration '${header.content}' - expected 'theme <Name>'`, locationOf(header));
        context.skipBlock(header.indent);
        return { kind: 'ThemeSyntax', name: firstWord(header.content), compatibleWith: [], location: locationOf(header) };
    }
    const name = match[1];
    const compatibleWith: string[] = [];
    const seen = new Set<string>();
    const directiveLocations: Record<string, SourceLocation> = {};
    let implementsName: string | null = null;
    const palette: DesignAssignmentSyntax[] = [];
    const values: DesignAssignmentSyntax[] = [];
    const modeValues: ThemeModeValuesSyntax[] = [];
    const declaredSections = new Set<string>();
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const word = firstWord(line.content);
        if (word === 'implements') {
            context.skipBlock(line.indent);
            const implementsMatch = implementsPattern.exec(line.content);
            if (implementsMatch === null || implementsName !== null) {
                context.error(DiagnosticCodes.InvalidThemeBlock,
                    implementsName !== null ? `Theme '${name}' already declares which design system it implements` : `Invalid implements line '${line.content}' - expected 'implements <DesignSystem>'`, locationOf(line));
                continue;
            }
            implementsName = implementsMatch[1];
            directiveLocations.implements = locationOf(line);
            continue;
        }
        if (word === 'palette' || word === 'values') {
            const target = word === 'palette' ? palette : values;
            if (line.content !== word || declaredSections.has(word)) {
                context.error(DiagnosticCodes.InvalidThemeBlock,
                    line.content !== word ? `Invalid ${word} line '${line.content}' - expected '${word}' followed by an indented block` : `Theme '${name}' already declares ${word}`, locationOf(line));
                context.skipBlock(line.indent);
                continue;
            }
            declaredSections.add(word);
            parseAssignments(context, line, word, target);
            continue;
        }
        const compatibleMatch = compatibleWithPattern.exec(line.content);
        if (compatibleMatch !== null) {
            const compatiblePackage = compatibleMatch[1];
            if (seen.has(compatiblePackage)) {
                context.error(DiagnosticCodes.DuplicateCompatibleWith, `Theme '${name}' already declares compatibility with '${compatiblePackage}'`, locationOf(line));
                continue;
            }
            seen.add(compatiblePackage);
            compatibleWith.push(compatiblePackage);
            directiveLocations[`compatible:${compatiblePackage.length}:${compatiblePackage}:0`] = locationOf(line);
            continue;
        }
        if (modePattern.test(line.content) && context.peekChild(line.indent) !== undefined) {
            if (modeValues.some(existing => existing.mode === line.content)) {
                context.error(DiagnosticCodes.InvalidThemeBlock, `Theme '${name}' already declares values for the mode '${line.content}'`, locationOf(line));
                context.skipBlock(line.indent);
                continue;
            }
            const modeAssignments: DesignAssignmentSyntax[] = [];
            parseAssignments(context, line, `mode ${line.content}`, modeAssignments);
            modeValues.push({ kind: 'ThemeModeValuesSyntax', mode: line.content, values: modeAssignments, location: locationOf(line) });
            continue;
        }
        context.error(DiagnosticCodes.InvalidCompatibleWithDeclaration,
            `Invalid theme line '${line.content}' - expected 'compatible with <Package>', 'implements <DesignSystem>', 'palette', 'values' or a mode block`, locationOf(line));
        context.skipBlock(line.indent);
    }
    return { kind: 'ThemeSyntax', name, compatibleWith, ...(implementsName === null ? {} : { implements: implementsName }), ...(palette.length === 0 ? {} : { palette }), ...(values.length === 0 ? {} : { values }), ...(modeValues.length === 0 ? {} : { modeValues }), directiveLocations, location: locationOf(header) };
}

function parseAssignments(context: ParserContext, header: SourceLine, section: string, assignments: DesignAssignmentSyntax[]): void {
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        context.skipBlock(line.indent);
        const assignment = parseDesignAssignment(line);
        if (assignment === undefined) {
            context.error(DiagnosticCodes.InvalidThemeBlock, `Invalid ${section} line '${line.content}' - expected '<name> = "<value>"' or '<name> = <reference>'`, locationOf(line));
            continue;
        }
        if (assignments.some(existing => existing.name === assignment.name)) {
            context.error(DiagnosticCodes.DuplicateDesignToken, `'${assignment.name}' is already assigned in ${section}`, locationOf(line));
            continue;
        }
        assignments.push(assignment);
    }
}

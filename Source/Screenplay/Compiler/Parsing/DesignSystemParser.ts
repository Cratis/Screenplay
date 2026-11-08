// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { DesignAssignmentSyntax, DesignComponentSyntax, DesignContrastRuleSyntax, DesignSystemSyntax, DesignTokenCategorySyntax, DesignWordsSyntax } from '../Syntax/DesignSystemSyntax';
import { parseDesignAssignment } from './DesignAssignmentParser';
import { parseDesignComponents } from './DesignComponentParser';
import { parseDesignTokens } from './DesignTokenParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { SourceLine, locationOf } from './SourceLine';

const sections = new Set(['modes', 'default', 'tokens', 'components', 'words', 'rules']);
const headerPattern = /^design\s+system\s+([A-Za-z_]\w*)$/;
const modesPattern = /^modes\s+([A-Za-z_][\w-]*(?:\s*,\s*[A-Za-z_][\w-]*)*)$/;
const defaultThemePattern = /^default\s+theme\s+([A-Za-z_]\w*)$/;
const contrastPattern = /^contrast\s+([A-Za-z_]\w*)\s+on\s+([A-Za-z_]\w*)\s+at\s+least\s+(\d+(?:\.\d+)?)$/;

// Parses a top level `design system <Name>` block: its modes, default theme, tokens, components, words and rules. The port of the
// C# DesignSystemParser.
export function parseDesignSystem(context: ParserContext, header: SourceLine): DesignSystemSyntax {
    const match = headerPattern.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Invalid design system declaration '${header.content}' - expected 'design system <Name>'`, locationOf(header));
        context.skipBlock(header.indent);
        return { kind: 'DesignSystemSyntax', name: firstWord(header.content), modes: [], tokens: [], components: [], defaultTheme: null, words: null, rules: [], location: locationOf(header) };
    }
    const modes: string[] = [];
    const tokens: DesignTokenCategorySyntax[] = [];
    const components: DesignComponentSyntax[] = [];
    const rules: DesignContrastRuleSyntax[] = [];
    let defaultTheme: string | null = null;
    let words: DesignWordsSyntax | null = null;
    const seen = new Set<string>();
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const word = firstWord(line.content);
        if (sections.has(word)) {
            if (seen.has(word)) {
                context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `The design system already declares '${word}' - declare it once`, locationOf(line));
                context.skipBlock(line.indent);
                continue;
            }
            seen.add(word);
        }
        switch (word) {
            case 'modes': parseModes(context, line, modes); break;
            case 'default': defaultTheme = parseDefaultTheme(context, line); break;
            case 'tokens': parseDesignTokens(context, line, tokens); break;
            case 'components': parseDesignComponents(context, line, components); break;
            case 'words': words = parseWords(context, line); break;
            case 'rules': parseRules(context, line, rules); break;
            default:
                context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Unexpected '${word}' in design system body - expected modes, default theme, tokens, components, words or rules`, locationOf(line));
                context.skipBlock(line.indent);
                break;
        }
    }
    return { kind: 'DesignSystemSyntax', name: match[1], modes, tokens, components, defaultTheme, words, rules, location: locationOf(header) };
}

function parseModes(context: ParserContext, line: SourceLine, modes: string[]): void {
    context.skipBlock(line.indent);
    const match = modesPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Invalid modes line '${line.content}' - expected 'modes <name>, <name>'`, locationOf(line));
        return;
    }
    for (const mode of match[1].split(',').map(entry => entry.trim())) {
        if (modes.includes(mode)) {
            context.error(DiagnosticCodes.DuplicateDesignToken, `The design system already declares the mode '${mode}'`, locationOf(line));
            continue;
        }
        modes.push(mode);
    }
}

function parseDefaultTheme(context: ParserContext, line: SourceLine): string | null {
    context.skipBlock(line.indent);
    const match = defaultThemePattern.exec(line.content);
    if (match !== null) return match[1];
    context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Invalid default theme line '${line.content}' - expected 'default theme <Name>'`, locationOf(line));
    return null;
}

function parseWords(context: ParserContext, header: SourceLine): DesignWordsSyntax {
    const voice: DesignAssignmentSyntax[] = [];
    const naming: DesignAssignmentSyntax[] = [];
    const seen = new Set<string>();
    for (let section = context.peekChild(header.indent); section !== undefined; section = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const target = section.content === 'voice' ? voice : section.content === 'naming' ? naming : undefined;
        if (target === undefined || seen.has(section.content)) {
            context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Unexpected '${section.content}' in words - expected 'voice' and 'naming', each once`, locationOf(section));
            context.skipBlock(section.indent);
            continue;
        }
        seen.add(section.content);
        for (let entry = context.peekChild(section.indent); entry !== undefined; entry = context.peekChild(section.indent)) {
            context.reader.takeSignificant();
            const assignment = parseDesignAssignment(entry);
            if (assignment === undefined || !assignment.isLiteral) {
                context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Invalid ${section.content} entry '${entry.content}' - expected '<key> = "<text>"'`, locationOf(entry));
                continue;
            }
            if (target.some(existing => existing.name === assignment.name)) {
                context.error(DiagnosticCodes.DuplicateDesignToken, `The ${section.content} entry '${assignment.name}' is already declared`, locationOf(entry));
                continue;
            }
            target.push(assignment);
        }
    }
    return { kind: 'DesignWordsSyntax', voice, naming, location: locationOf(header) };
}

function parseRules(context: ParserContext, header: SourceLine, rules: DesignContrastRuleSyntax[]): void {
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        context.skipBlock(line.indent);
        const match = contrastPattern.exec(line.content);
        if (match === null) {
            context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Invalid rule '${line.content}' - expected 'contrast <part> on <part> at least <ratio>'`, locationOf(line));
            continue;
        }
        rules.push({ kind: 'DesignContrastRuleSyntax', foreground: match[1], background: match[2], minimum: match[3], location: locationOf(line) });
    }
}

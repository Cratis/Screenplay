// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { compileApplication } from '../Files/PlayApplicationAssembly';
import { splitLines } from '../Parsing/SourceLineSplitter';
import { parseForAuthoring } from '../ScreenplayCompiler';
import { DeprecatedAppearanceMember, DesignAssignmentSyntax, DesignProfileFacts, DesignSystemSyntax, ThemeSyntax } from '../Syntax/DesignSystemSyntax';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { applyQuickFixEdits, QuickFix, QuickFixEdit, QuickFixOptions } from './QuickFixes';

type Options = Pick<QuickFixOptions, 'placement' | 'path' | 'documents'>;
type Lines = ReturnType<typeof splitLines>;

interface Located<T> {
    readonly path: string;
    readonly node: T;
}

const accentEntry = 'accent';
const radiusToken = 'radius.control';
const fontToken = 'font.sans';
const accentLine = new RegExp(`^accent\\s+"(${stringBodyPattern})"$`);
const assignmentLine = /^([A-Za-z_][\w.-]*)\s*=\s*(.*)$/;
const cornerRadius: Readonly<Record<string, string>> = { sharp: '2px', default: '6px', round: '12px' };

/**
 * Moves the deprecated `accent`, `corners` and `font` lines of a ui profile's `appearance` block into the theme the profile uses -
 * the port of the C# WorkspaceAppearanceRepairs. One repair covers the whole block and is offered on each of its deprecated lines.
 */
export function prepareAppearanceQuickFixes(source: string, options: Options): (line?: number, code?: string) => QuickFix[] {
    if (!/^\s*appearance\s*$/mu.test(source)) return () => [];
    const path = options.path ?? 'application.play';
    const documents = new Map(options.documents ?? [[path, source]]);
    if (documents.get(path) !== source || (options.documents === undefined && (options.placement?.length ?? 0) > 0)) return () => [];
    const compiled = compileApplication(documents);
    const designSystems: Located<DesignSystemSyntax>[] = [];
    const themes: Located<ThemeSyntax>[] = [];
    const profiles: Located<DesignProfileFacts>[] = [];
    for (const document of compiled.documents) {
        const parsed = parseForAuthoring(documents.get(document.path)!, document.path, document.placement);
        designSystems.push(...(parsed.value.designSystems ?? []).map(node => ({ path: document.path, node })));
        themes.push(...(parsed.value.themes ?? []).map(node => ({ path: document.path, node })));
        profiles.push(...(parsed.value.designProfiles ?? []).map(node => ({ path: document.path, node })));
    }
    if (designSystems.length !== 1) return () => [];
    const designSystem = designSystems[0].node;
    const fixes: QuickFix[] = [];
    for (const profile of profiles.filter(candidate => candidate.path === path && candidate.node.deprecatedAppearance.length > 0)) {
        const edits = repairEdits(documents, designSystem, themes, profile);
        if (edits === undefined || !verified(documents, edits.edits, profile)) continue;
        const outgoing = edits.edits.map(edit => options.documents === undefined ? { start: edit.start, length: edit.length, text: edit.text } : edit);
        for (const { location } of profile.node.deprecatedAppearance) {
            fixes.push({ line: location.line, diagnosticCode: DiagnosticCodes.DeprecatedAppearanceMember, title: `Move the appearance values into theme '${edits.themeName}'`, scope: 'occurrence', edits: outgoing });
        }
    }
    return (line, code) => fixes.filter(fix => (line === undefined || fix.line === line) && (code === undefined || fix.diagnosticCode === code));
}

// The repair must remove the profile's deprecation warnings.
function verified(documents: ReadonlyMap<string, string>, edits: readonly QuickFixEdit[], profile: Located<DesignProfileFacts>): boolean {
    const changed = new Map(documents);
    for (const path of new Set(edits.map(edit => edit.path!))) {
        // The edits never overlap: each touches its own line or section.
        changed.set(path, applyQuickFixEdits(documents.get(path)!, edits.filter(edit => edit.path === path))!);
    }
    const compiled = compileApplication(changed);
    const remaining = compiled.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.DeprecatedAppearanceMember && diagnostic.location.path === profile.path);
    return remaining.length < profile.node.deprecatedAppearance.length;
}

function repairEdits(documents: ReadonlyMap<string, string>, designSystem: DesignSystemSyntax, themes: readonly Located<ThemeSyntax>[], profile: Located<DesignProfileFacts>): { edits: QuickFixEdit[]; themeName: string } | undefined {
    const themeName = profile.node.theme ?? designSystem.defaultTheme;
    const matching = themes.filter(candidate => candidate.node.name === themeName && candidate.node.implements === designSystem.name);
    if (themeName === null || matching.length !== 1) return undefined;
    const theme = matching[0];
    const profileLines = splitLines(documents.get(profile.path)!);
    const members = new Map<DeprecatedAppearanceMember, number>(profile.node.deprecatedAppearance.map(({ member, location }) => [member, location.line - 1]));

    const palette: string[][] = [];
    const values: string[][] = [];
    if (members.has('accent')) {
        // The parser only reports an accent line of this shape.
        const accent = accentLine.exec(profileLines[members.get('accent')!].content)![1];
        const existing = (theme.node.palette ?? []).find(entry => entry.name === accentEntry);
        if (existing === undefined) palette.push([accentEntry, `"${accent}"`]);
        else if (!existing.isLiteral || existing.value.toLowerCase() !== unescapeString(accent).toLowerCase()) return undefined;
    }
    if (members.has('corners') && !moveValue(radiusToken, radiusOf(/^corners\s+(\S+)$/.exec(profileLines[members.get('corners')!].content)![1]), designSystem, theme.node, values)) return undefined;
    if (members.has('font') && !moveValue(fontToken, /^font\s+(\S+)$/.exec(profileLines[members.get('font')!].content)![1], designSystem, theme.node, values)) return undefined;

    const themeText = documents.get(theme.path)!;
    const themeEdits = themeInsertions(themeText, splitLines(themeText), theme.node.location.line - 1, palette, values, theme.path);
    if (themeEdits === undefined) return undefined;
    return { edits: [...profileRemovals(profileLines, documents.get(profile.path)!, [...members.values()], profile.path), ...themeEdits].sort(byPosition), themeName };
}

const radiusOf = (corners: string): string | undefined => Object.hasOwn(cornerRadius, corners) ? cornerRadius[corners] : undefined;

const byPosition = (left: QuickFixEdit, right: QuickFixEdit): number => (left.path === right.path ? 0 : (left.path! < right.path! ? -1 : 1)) || left.start - right.start;

// A token the theme already sets wins: the deprecated line was stale and removing it loses nothing the theme does not say.
function moveValue(token: string, value: string | undefined, designSystem: DesignSystemSyntax, theme: ThemeSyntax, values: string[][]): boolean {
    if (value === undefined || !designSystem.tokens.some(category => category.tokens.some(candidate => `${category.category}.${candidate.name}` === token))) return false;
    if (!(theme.values ?? []).some((entry: DesignAssignmentSyntax) => entry.name === token)) values.push([token, `"${value}"`]);
    return true;
}

function profileRemovals(lines: Lines, text: string, lineIndexes: readonly number[], path: string): QuickFixEdit[] {
    const removed = new Set(lineIndexes);
    const first = Math.min(...removed);
    const indent = lines[first].indent;
    let header = first - 1;
    while (header >= 0 && (lines[header].content === '' || lines[header].indent >= indent)) header--;
    if (header >= 0 && lines[header].content === 'appearance') {
        const end = blockEnd(lines, header);
        const others = lines.slice(header + 1, end + 1).filter((line, offset) => line.content !== '' && !removed.has(header + 1 + offset));
        if (others.length === 0) return [lineRemoval(lines, text, header, end, path)];
    }
    return [...removed].map(index => lineRemoval(lines, text, index, index, path));
}

// The last non-blank line of the block opened by the line at 'index'.
function blockEnd(lines: Lines, index: number): number {
    let end = index;
    for (let next = index + 1; next < lines.length && (lines[next].content === '' || lines[next].indent > lines[index].indent); next++) if (lines[next].content !== '') end = next;
    return end;
}

const lineStart = (lines: Lines, index: number): number => lines[index].startOffset;
const lineEnd = (lines: Lines, text: string, index: number): number => index + 1 < lines.length ? lines[index + 1].startOffset : text.length;
const lineRemoval = (lines: Lines, text: string, first: number, last: number, path: string): QuickFixEdit => ({ path, start: lineStart(lines, first), length: lineEnd(lines, text, last) - lineStart(lines, first), text: '' });

// Adds the entries to the theme's palette and values sections, creating the sections where the theme has none, in the printer's
// order: palette, then values, then the blocks of values per mode.
function themeInsertions(text: string, lines: Lines, headerIndex: number, palette: string[][], values: string[][], path: string): QuickFixEdit[] | undefined {
    if (palette.length === 0 && values.length === 0) return [];
    const header = lines[headerIndex];
    const end = blockEnd(lines, headerIndex);
    const children = lines.slice(headerIndex + 1, end + 1).filter(line => line.content !== '');
    const bodyIndent = children[0].indent;
    const unit = bodyIndent - header.indent;
    const sections = children.filter(line => line.indent === bodyIndent).map(line => ({ word: line.content.split(/\s+/)[0], line }));
    const sectionOf = (word: string) => sections.find(section => section.line.content === word);
    const ending = text.includes('\r\n') ? '\r\n' : '\n';
    const pad = ' '.repeat(bodyIndent);
    const edits: QuickFixEdit[] = [];
    const created: string[] = [];
    const append = (heading: string, entries: string[][]): boolean => {
        if (entries.length === 0) return true;
        const section = sectionOf(heading);
        if (section === undefined) {
            created.push(sectionText(heading, entries, pad, unit, ending));
            return true;
        }
        const index = section.line.number - 1;
        const last = blockEnd(lines, index);
        const entryIndent = lines.slice(index + 1, last + 1).find(line => assignmentLine.test(line.content))?.indent ?? bodyIndent + unit;
        const existing = lines.slice(index + 1, last + 1).filter(line => line.content !== '').map(line => ({ line, match: assignmentLine.exec(line.content) }));
        const width = Math.max(...entries.map(([name]) => name.length), ...existing.map(({ match }) => match?.[1].length ?? 0));
        const rendered = [
            ...existing.map(({ line, match }) => match === null ? line.raw : `${' '.repeat(entryIndent)}${match[1].padEnd(width)} = ${match[2]}`),
            ...entries.map(([name, value]) => `${' '.repeat(entryIndent)}${name.padEnd(width)} = ${value}`)
        ];
        // The section's comment-free body is rewritten as one aligned run; a blank or comment-only line inside it keeps the edit unsafe.
        if (lines.slice(index + 1, last + 1).some(line => line.content === '' || line.raw.trim() !== line.content)) return false;
        if (last === index) {
            edits.push({ path, start: lines[index].startOffset + lines[index].raw.length, length: 0, text: `${ending}${rendered.join(ending)}` });
            return true;
        }
        const from = lineStart(lines, index + 1);
        edits.push({ path, start: from, length: lines[last].startOffset + lines[last].raw.length - from, text: rendered.join(ending) });
        return true;
    };
    if (!append('palette', palette) || !append('values', values)) return undefined;
    if (created.length > 0) {
        const before = sectionOf('values') ?? sections.find(section => !['palette', 'values', 'implements', 'compatible'].includes(section.word));
        const point = before === undefined ? lines[end].startOffset + lines[end].raw.length : lineStart(lines, before.line.number - 1);
        const body = created.join(ending + ending);
        edits.push({ path, start: point, length: 0, text: before === undefined ? `${ending}${ending}${body}` : `${body}${ending}${ending}` });
    }
    return edits;
}

function sectionText(heading: string, entries: string[][], pad: string, unit: number, ending: string): string {
    const width = Math.max(...entries.map(([name]) => name.length));
    return [`${pad}${heading}`, ...entries.map(([name, value]) => `${pad}${' '.repeat(unit)}${name.padEnd(width)} = ${value}`)].join(ending);
}

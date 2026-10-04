// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CaptureAppendSyntax, CaptureChildrenSyntax, CaptureMapOperationSyntax, CaptureNestedSyntax, CaptureSourceSettingSyntax, CaptureSourceSyntax, CaptureSyntax, CaptureTranslationSyntax, CaptureWhenSyntax } from '../Syntax/Captures';
import { TagSyntax } from '../Syntax/Declarations';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { pattern } from '../Text/patterns';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { parseTag } from './DeclarationParsers';
import { parseMappingSource } from './ExpressionParser';
import { firstWord, unescapeIdentifier } from './LineText';
import { ParserContext } from './ParserContext';
import { parseProjectionExpression } from './ProjectionExpressionParser';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^capture\\s+([A-Za-z_]\\w*)$');
const appendPattern = pattern('^append\\s+([A-Z]\\w*)$');
const childrenPattern = pattern('^children\\s+([a-z_]\\w*)\\s+identified\\s+by\\s+([\\w.]+)$');
const nestedPattern = pattern('^nested\\s+([\\w.]+)$');
const mappingPattern = pattern('^(@?[\\w.]+)\\s*=(?!=|>)\\s*(.+)$');
const mapEntryPattern = pattern('^([a-z_]\\w*)\\s*=\\s*(.+)$');
const translationPattern = pattern(`^"(${stringBodyPattern})"\\s*=>\\s*(\\w+)$`);
const splitPattern = pattern(`^split\\s+(\\S+)\\s+by\\s+"(${stringBodyPattern})"$`);
const whenTokens = new RegExp(`"${stringBodyPattern}"|[\\w.]+`, 'gu');

// A structural port of the existing capture grammar. Transition operands remain authored strings, as
// C# models them; their numeric semantic interpretation belongs to ESM v7, not source admission.
export function parseCapture(context: ParserContext, line: SourceLine): CaptureSyntax {
    context = context.valueContext;
    const name = header.exec(line.content)?.[1] ?? '';
    if (name === '' && context.sourceOptions.numericMode === 'exact') context.error(DiagnosticCodes.InvalidCaptureDeclaration, `Invalid capture declaration '${line.content}' - expected 'capture <Name>'`, locationOf(line));
    let source: CaptureSourceSyntax | null = null;
    let key: string | null = null;
    const map: CaptureMapOperationSyntax[] = [];
    const appends: CaptureAppendSyntax[] = [];
    const children: CaptureChildrenSyntax[] = [];
    const nested: CaptureNestedSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        switch (firstWord(child.content)) {
            case 'source': source = parseSource(context, child); break;
            case 'key': key = child.content.substring('key'.length).trim(); break;
            case 'map': map.push(...parseMap(context, child)); break;
            case 'append': pushAppend(context, child, appends); break;
            case 'children': {
                const match = childrenPattern.exec(child.content);
                if (match === null) { context.error(DiagnosticCodes.InvalidChildrenDeclaration, `Invalid children declaration '${child.content}' - expected 'children <collection> identified by <key>'`, locationOf(child)); context.skipOpaqueBlock(child.indent); break; }
                const body = mapAndAppends(context, child);
                if (match !== null) children.push({ kind: 'CaptureChildrenSyntax', property: match[1], identifiedBy: match[2], ...body, location: locationOf(child) });
                break;
            }
            case 'nested': {
                const match = nestedPattern.exec(child.content);
                if (match === null) { context.error(DiagnosticCodes.InvalidNestedDeclaration, `Invalid nested declaration '${child.content}' - expected 'nested <Property>'`, locationOf(child)); context.skipOpaqueBlock(child.indent); break; }
                const body = mapAndAppends(context, child);
                if (match !== null) nested.push({ kind: 'CaptureNestedSyntax', property: match[1], ...body, location: locationOf(child) });
                break;
            }
            default:
                context.error(DiagnosticCodes.UnknownCaptureDirective, `Unexpected '${child.content}' in capture body`, locationOf(child));
                context.skipOpaqueBlock(child.indent);
        }
    }
    return { kind: 'CaptureSyntax', sourceOptions: context.sourceOptions, name, source, key, map, appends, children, nested, location: locationOf(line) };
}

function parseSource(context: ParserContext, line: SourceLine): CaptureSourceSyntax {
    const settings: CaptureSourceSettingSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const settingName = firstWord(child.content);
        settings.push({ kind: 'CaptureSourceSettingSyntax', name: settingName, value: child.content.substring(settingName.length).trim(), location: locationOf(child) });
    }
    return { kind: 'CaptureSourceSyntax', syntaxKind: line.content.substring('source'.length).trim(), settings, location: locationOf(line) };
}

function parseMap(context: ParserContext, line: SourceLine): CaptureMapOperationSyntax[] {
    const map: CaptureMapOperationSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const location = locationOf(child);
        if (firstWord(child.content) === 'split') {
            const match = splitPattern.exec(child.content);
            if (match === null) { context.error(DiagnosticCodes.InvalidSplitDeclaration, `Invalid split operation '${child.content}' - expected 'split <property> by "<separator>"'`, location); context.skipOpaqueBlock(child.indent); continue; }
            const targets: string[] = [];
            for (let target = context.peekChild(child.indent); target !== undefined; target = context.peekChild(child.indent)) {
                context.reader.takeSignificant();
                if (/^[\w.]+$/.test(target.content)) targets.push(target.content);
                else context.error(DiagnosticCodes.InvalidSplitTarget, `Invalid split target '${target.content}' - expected a property path`, locationOf(target));
            }
            if (match !== null) map.push({ kind: 'CaptureSplitSyntax', source: parseProjectionExpression(match[1], location, context), separator: unescapeString(match[2]), targets, location });
            continue;
        }
        const match = mapEntryPattern.exec(child.content);
        if (match === null) { context.error(DiagnosticCodes.InvalidMapEntry, `Invalid map entry '${child.content}' - expected '<property> = <source> [translate]'`, location); context.skipOpaqueBlock(child.indent); continue; }
        const raw = match[2].trim();
        const closing = raw.startsWith('`') ? raw.indexOf('`', 1) : -1;
        const translate = raw.startsWith('`') ? closing >= 0 && raw.substring(closing + 1).trim() === 'translate' : raw.endsWith(' translate');
        const text = raw.startsWith('`') && closing >= 0 && (context.sourceOptions.numericMode !== 'exact' || translate || raw.substring(closing + 1).trim() === '') ? raw.substring(0, closing + 1) : translate ? raw.substring(0, raw.length - ' translate'.length).trimEnd() : raw;
        const translations: CaptureTranslationSyntax[] = [];
        if (translate) {
            for (let entry = context.peekChild(child.indent); entry !== undefined; entry = context.peekChild(child.indent)) {
                context.reader.takeSignificant();
                const translated = translationPattern.exec(entry.content);
                if (translated !== null) translations.push({ kind: 'CaptureTranslationSyntax', from: unescapeString(translated[1]), to: translated[2], location: locationOf(entry) });
                else context.error(DiagnosticCodes.InvalidTranslation, `Invalid translation '${entry.content}' - expected '"<source>" => <target>'`, locationOf(entry));
            }
        }
        map.push({ kind: 'CaptureMapEntrySyntax', property: match[1], source: parseProjectionExpression(text, location, context), translations, location });
    }
    return map;
}

function mapAndAppends(context: ParserContext, line: SourceLine): { map: CaptureMapOperationSyntax[]; appends: CaptureAppendSyntax[] } {
    let map: CaptureMapOperationSyntax[] = [];
    const appends: CaptureAppendSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'map') map = parseMap(context, child);
        else if (firstWord(child.content) === 'append') pushAppend(context, child, appends);
        else {
            context.error(DiagnosticCodes.UnknownCaptureBlockDirective, `Unexpected '${child.content}' in ${firstWord(line.content)} block - expected 'map' or 'append <EventType>'`, locationOf(child));
            context.skipOpaqueBlock(child.indent);
        }
    }
    return { map, appends };
}

function pushAppend(context: ParserContext, line: SourceLine, appends: CaptureAppendSyntax[]): void {
    const match = appendPattern.exec(line.content);
    if (match === null) { context.error(DiagnosticCodes.InvalidAppendDeclaration, `Invalid append declaration '${line.content}' - expected 'append <EventType>'`, locationOf(line)); context.skipOpaqueBlock(line.indent); return; }
    const mappings: PropertyMappingSyntax[] = [];
    const tags: TagSyntax[] = [];
    let when: CaptureWhenSyntax | null = null;
    const mapping = (child: SourceLine, nested = false): void => {
        const location = locationOf(child);
        if (firstWord(child.content) === 'tag') {
            const tag = parseTag(context.valueContext, child, false);
            if (tag !== undefined) tags.push(tag);
            return;
        }
        const mapped = mappingPattern.exec(child.content);
        if (mapped !== null) mappings.push({ kind: 'PropertyMappingSyntax', property: unescapeIdentifier(mapped[1]), source: parseMappingSource(mapped[2], location, context.valueContext), location });
        else context.error(nested ? DiagnosticCodes.InvalidPropertyMapping : DiagnosticCodes.UnknownAppendDirective, `Unexpected '${child.content}' in append body`, location);
    };
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'when') {
            when = parseWhen(context, child);
            for (let value = context.peekChild(child.indent); value !== undefined; value = context.peekChild(child.indent)) {
                context.reader.takeSignificant();
                mapping(value, true);
            }
        } else mapping(child);
    }
    appends.push({ kind: 'CaptureAppendSyntax', event: match[1], when, mappings, tags, location: locationOf(line) });
}

function parseWhen(context: ParserContext, line: SourceLine): CaptureWhenSyntax | null {
    const trigger = line.content.substring('when'.length).trim();
    const location = locationOf(line);
    const value = (syntaxKind: CaptureWhenSyntax['syntaxKind'], properties: string[] = [], from: string | null = null, to: string | null = null, expression: string | null = null): CaptureWhenSyntax => ({ kind: 'CaptureWhenSyntax', syntaxKind, properties, fromValue: from, toValue: to, expression, location });
    if (trigger === 'added') return value('Added');
    if (trigger === 'removed') return value('Removed');
    if (trigger.startsWith('`')) {
        if (!trigger.endsWith('`') || trigger.length < 2) context.error(DiagnosticCodes.UnterminatedTemplateExpression, "Unterminated template expression in 'when' - expected a closing backtick", location);
        return value('Expression', [], null, null, trigger);
    }
    if (trigger.length === 0) { context.error(DiagnosticCodes.WhenWithoutTrigger, "Expected a trigger after 'when'", location); return null; }
    const tokens: string[] = [...(trigger.match(context.sourceOptions.numericMode === 'exact' ? new RegExp(`"${stringBodyPattern}"|[^\\s]+`, 'gu') : whenTokens) ?? [])];
    const unquote = (text: string): string => text.startsWith('"') && text.endsWith('"') ? unescapeString(text.substring(1, text.length - 1)) : text;
    if (tokens.length === 0) { context.error(DiagnosticCodes.InvalidWhenClause, `Invalid 'when' clause '${line.content}'`, location); return null; }
    if (tokens.length === 1) return value('PropertyChanged', [unquote(tokens[0])]);
    if (tokens[1] === 'from') {
        if (tokens.length === 5 && tokens[3] === 'to') return value('ValueTransition', [unquote(tokens[0])], unquote(tokens[2]), unquote(tokens[4]));
        context.error(DiagnosticCodes.InvalidWhenTransitionClause, `Invalid 'when ... from ... to ...' clause '${line.content}'`, location); return null;
    }
    if (tokens[1] !== 'or' && tokens[1] !== 'and') { context.error(DiagnosticCodes.InvalidWhenClause, `Invalid 'when' clause '${line.content}'`, location); return null; }
    if (tokens.length % 2 === 0) { context.error(DiagnosticCodes.WhenCombinatorWithoutProperty, `Expected a property after '${tokens[1]}'`, location); return null; }
    const properties = [unquote(tokens[0])];
    for (let index = 1; index < tokens.length - 1; index += 2) {
        if (tokens[index] !== tokens[1]) { context.error(DiagnosticCodes.MixedWhenCombinators, `Cannot mix 'and' and 'or' in a single 'when' clause '${line.content}'`, location); return null; }
        properties.push(unquote(tokens[index + 1]));
    }
    return value(tokens[1] === 'or' ? 'LogicalOr' : 'LogicalAnd', properties);
}

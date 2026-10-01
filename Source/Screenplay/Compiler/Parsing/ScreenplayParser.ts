// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ConceptAttributeSyntax, ConceptSyntax, DomainSyntax, ImportSyntax, TypeSyntax } from '../Syntax/Declarations';
import { ApplicationSyntax, FeatureSyntax, ModuleSyntax, SliceSyntax } from '../Syntax/Structure';
import { pattern } from '../Text/patterns';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { parseType } from './DeclarationParsers';
import { parseDescription } from './DescriptionParser';
import { isFileDirective } from './FileReferences';
import { firstWord, unescapeIdentifier } from './LineText';
import { ParserContext } from './ParserContext';
import { parseSlice } from './SliceParser';
import { locationOf, SourceLine, startOf } from './SourceLine';

const domainPattern = pattern('^domain\\s+([A-Za-z_]\\w*(?:\\.[A-Za-z_]\\w*)*)$');
const importPattern = pattern('^import\\s+([\\w.]+)$');
const conceptPattern = pattern('^concept\\s+(\\w+)\\s*:\\s*(\\w+)((?:\\s+@\\w+)*)$');
const enumValuePattern = pattern('^@?[a-z_]\\w*$');
const attributeReasonPattern = pattern(`^([a-z_]\\w*)\\s+reason\\s+"(${stringBodyPattern})"$`);
const modulePattern = pattern('^module\\s+([A-Za-z_]\\w*)$');
const featurePattern = pattern('^feature\\s+([A-Za-z_]\\w*)$');
const tabIndentPattern = /^[ ]*\t/;
const primitiveTypes = ['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime'];

// Top-level, module and feature constructs the C# compiler knows that this compiler does not model. They
// are skipped whole, not reported.
const opaqueTopLevel = new Set(['policy', 'persona', 'authentication', 'seed', 'ui', 'theme', 'trigger', 'layout', 'behavior']);
const opaqueModuleMembers = new Set(['authorize', 'on', 'uses', 'screen', 'dialog', 'form', 'contribute']);
const opaqueFeatureMembers = new Set(['authorize', 'on', 'uses', 'contribute']);

// Parses one document into its application syntax - the port of the C# ScreenplayParser.
export function parseApplication(context: ParserContext, lines: readonly SourceLine[]): ApplicationSyntax {
    warnOnTabIndentation(context, lines);
    let domain: DomainSyntax | null = null;
    const imports: ImportSyntax[] = [];
    const concepts: ConceptSyntax[] = [];
    const types: TypeSyntax[] = [];
    const modules: ModuleSyntax[] = [];
    let sawOtherConstruct = false;
    for (let line = context.reader.peekSignificant(); line !== undefined; line = context.reader.peekSignificant()) {
        context.reader.takeSignificant();
        const keyword = firstWord(line.content);
        if (keyword === 'domain') {
            domain = parseDomain(context, line, domain, sawOtherConstruct);
            continue;
        }
        // The C# parser asks whether anything was declared before the domain, so an import it rejected
        // and an unknown construct do not count.
        sawOtherConstruct ||= declaresConstruct(keyword, line);
        if (keyword === 'import') {
            const match = importPattern.exec(line.content);
            if (match === null) {
                context.error(DiagnosticCodes.InvalidImportDeclaration, `Invalid import '${line.content}' - expected 'import <Qualified.Name>'`, locationOf(line));
            } else {
                imports.push({ kind: 'ImportSyntax', qualifiedName: match[1], location: locationOf(line) });
            }
        } else if (keyword === 'concept') {
            concepts.push(parseConcept(context, line));
        } else if (keyword === 'type') {
            types.push(parseType(context, line));
        } else if (keyword === 'module') {
            modules.push(parseModule(context, line));
        } else if (opaqueTopLevel.has(keyword)) {
            context.skipOpaqueBlock(line.indent);
        } else {
            context.error(DiagnosticCodes.UnknownTopLevelConstruct, `Unexpected '${keyword}' at the top level - expected domain, import, concept, type, policy, persona, authentication, module, seed, trigger, behavior, ui profile, theme or layout`, locationOf(line));
            context.skipBlock(line.indent);
        }
    }
    return { kind: 'ApplicationSyntax', domain, imports, concepts, types, modules, location: context.start };
}

function declaresConstruct(keyword: string, line: SourceLine): boolean {
    if (keyword === 'import') {
        return importPattern.test(line.content);
    }
    return keyword === 'concept' || keyword === 'type' || keyword === 'module' || opaqueTopLevel.has(keyword);
}

function parseDomain(context: ParserContext, line: SourceLine, existing: DomainSyntax | null, sawOtherConstruct: boolean): DomainSyntax | null {
    const match = domainPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidDomainDeclaration, `Invalid domain declaration '${line.content}' - expected 'domain <Qualified.Name>'`, locationOf(line));
        return existing;
    }
    if (existing !== null) {
        context.error(DiagnosticCodes.DuplicateDomain, 'The document already declares a domain - a document can have at most one', locationOf(line));
        return existing;
    }
    if (sawOtherConstruct) {
        context.error(DiagnosticCodes.DomainNotFirst, '\'domain\' must be declared before any other construct', locationOf(line));
    }
    return { kind: 'DomainSyntax', name: match[1], location: locationOf(line) };
}

function warnOnTabIndentation(context: ParserContext, lines: readonly SourceLine[]): void {
    let inFence = false;
    for (const line of lines) {
        if (line.raw.trimStart().startsWith('```')) {
            inFence = !inFence;
            continue;
        }
        if (!inFence && tabIndentPattern.test(line.raw)) {
            context.warning(DiagnosticCodes.TabIndentation, 'Screenplay is indentation based - use spaces, not tabs', startOf(line));
        }
    }
}

function parseConcept(context: ParserContext, line: SourceLine): ConceptSyntax {
    const match = conceptPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidConceptDeclaration, `Invalid concept declaration '${line.content}' - expected 'concept <Name> : <Type>'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'ConceptSyntax', name: firstWord(line.content), type: '', attributes: [], values: [], location: locationOf(line) };
    }
    const [, name, type, attributeText] = match;
    const attributes: ConceptAttributeSyntax[] = attributeText.split(' ').filter(attribute => attribute.length > 0)
        .map(attribute => ({ kind: 'ConceptAttributeSyntax', name: attribute.replace(/^@+/, ''), reason: null, location: locationOf(line) }));
    if (type !== 'Enum' && !primitiveTypes.includes(type)) {
        context.error(DiagnosticCodes.UnknownPrimitiveType, `Unknown primitive type '${type}' - expected ${primitiveTypes.join(', ')} or Enum`, locationOf(line));
    }
    const values: string[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const reason = attributeReasonPattern.exec(child.content);
        if (isFileDirective(child)) {
            continue;
        } else if (firstWord(child.content) === 'validate') {
            if (type === 'Enum' && child.content === 'validate' && context.peekChild(child.indent) === undefined) {
                context.warning(DiagnosticCodes.ValidateReadAsEnumerationBlock,
                    `'validate' in enumeration concept '${name}' declares an empty validate block, not a value named 'validate' - write '@validate' for the value`,
                    locationOf(child));
            }
            // Concept validations are not modeled; the block is skipped whole.
            context.skipOpaqueBlock(child.indent);
        } else if (reason !== null) {
            applyAttributeReason(context, child, name, attributes, reason[1], unescapeString(reason[2]));
        } else if (type === 'Enum' && enumValuePattern.test(child.content)) {
            values.push(unescapeIdentifier(child.content));
        } else if (type === 'Enum') {
            context.error(DiagnosticCodes.InvalidEnumerationValue, `Invalid enum value '${child.content}' - expected an identifier`, locationOf(child));
        } else {
            context.error(DiagnosticCodes.UnknownConceptDirective, `Unexpected '${child.content}' in concept body - expected validate, 'file <path>' or '<attribute> reason "<text>"'`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return { kind: 'ConceptSyntax', name, type, attributes, values, location: locationOf(line) };
}

function applyAttributeReason(context: ParserContext, line: SourceLine, concept: string, attributes: ConceptAttributeSyntax[], attribute: string, reason: string): void {
    const index = attributes.findIndex(candidate => candidate.name === attribute);
    if (index < 0) {
        context.error(DiagnosticCodes.AttributeReasonWithoutAttribute,
            `Concept '${concept}' declares a reason for '${attribute}' without the attribute - write 'concept ${concept} : <Type> @${attribute}'`, locationOf(line));
    } else if (attributes[index].reason !== null) {
        context.error(DiagnosticCodes.DuplicateAttributeReason, `Concept '${concept}' already declares a reason for '${attribute}' - at most one is allowed`, locationOf(line));
    } else {
        attributes[index] = { ...attributes[index], reason };
    }
}

function parseModule(context: ParserContext, line: SourceLine): ModuleSyntax {
    const name = modulePattern.exec(line.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidModuleDeclaration, `Invalid module declaration '${line.content}' - expected 'module <Name>'`, locationOf(line));
    }
    let description: string | null = null;
    const features: FeatureSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword === 'description') {
            description = parseDescription(context, child, description, `Module '${name}'`);
        } else if (keyword === 'feature') {
            features.push(parseFeature(context, child));
        } else if (opaqueModuleMembers.has(keyword)) {
            context.skipOpaqueBlock(child.indent);
        } else {
            context.error(DiagnosticCodes.UnknownModuleDirective, `Unexpected '${keyword}' in module body - expected description, authorize, screen template, dialog template, form, contribute, feature, 'on <trigger>' or 'uses <Behavior>'`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return { kind: 'ModuleSyntax', name, description, features, location: locationOf(line) };
}

function parseFeature(context: ParserContext, line: SourceLine): FeatureSyntax {
    const name = featurePattern.exec(line.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidFeatureDeclaration, `Invalid feature declaration '${line.content}' - expected 'feature <Name>'`, locationOf(line));
    }
    let description: string | null = null;
    const features: FeatureSyntax[] = [];
    const slices: SliceSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword === 'description') {
            description = parseDescription(context, child, description, `Feature '${name}'`);
        } else if (keyword === 'feature') {
            features.push(parseFeature(context, child));
        } else if (keyword === 'slice') {
            slices.push(parseSlice(context, child));
        } else if (opaqueFeatureMembers.has(keyword)) {
            context.skipOpaqueBlock(child.indent);
        } else {
            context.error(DiagnosticCodes.UnknownFeatureDirective, `Unexpected '${keyword}' in feature body - expected description, authorize, feature, slice, contribute, 'on <trigger>' or 'uses <Behavior>'`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return { kind: 'FeatureSyntax', name, description, features, slices, location: locationOf(line) };
}

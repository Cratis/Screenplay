// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { recordAuthoredDocument } from '../Syntax/SourceOptions';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { describePlacement, documentPlacement, isDocumentPlacement, PlayPlacement } from '../Files/PlayPlacement';
import { PersonaSyntax } from '../Syntax/Authorization';
import { ConceptSyntax, DomainSyntax, ImportSyntax, TypeSyntax } from '../Syntax/Declarations';
import { SpecificationExampleSyntax } from '../Syntax/Specifications';
import { ApplicationSyntax, FeatureSyntax, FileImportSyntax, ModuleSyntax } from '../Syntax/Structure';
import { parseTriggerDeclaration } from './TriggerDataParser';
import { pattern } from '../Text/patterns';
import { parseComplianceDirective, parseComplianceMarkers } from './ConceptComplianceParser';
import { parseType } from './DeclarationParsers';
import { ValidateSyntax } from '../Syntax/Commands';
import { parseValidate } from './CommandParser';
import { parseDescription } from './DescriptionParser';
import { FeatureBody, featureBodyExpected } from './FeatureBody';
import { isFileImport, parseFileImport } from './FileImportParser';
import { isFileDirective } from './FileReferences';
import { collectInputUses } from './InputUses';
import { firstWord, unescapeIdentifier } from './LineText';
import { ModuleBody, moduleBodyExpected, modulePattern, parseModule } from './ModuleBody';
import { ParserContext } from './ParserContext';
import { PolicySyntax } from '../Syntax/Policies';
import { parsePolicy } from './PolicyParser';
import { PurposeSyntax } from '../Syntax/Purposes';
import { parsePurpose } from './PurposeParser';
import { SeedSyntax } from '../Syntax/Seeds';
import { parseSeed } from './SeedParser';
import { parseSystem } from './OperationParser';
import { SystemSyntax } from '../Syntax/Operations';
import { EventSourceSyntax } from '../Syntax/EventSources';
import { parseEventSource } from './EventSourceParser';
import { parseExample } from './SpecificationExampleParser';
import { locationOf, SourceLine, startOf } from './SourceLine';

const domainPattern = pattern('^domain\\s+([A-Za-z_]\\w*(?:\\.[A-Za-z_]\\w*)*)$');
const importPattern = pattern('^import\\s+([\\w.]+)$');
const conceptPattern = pattern('^concept\\s+(\\w+)\\s*:\\s*(\\w+)((?:\\s+@?\\w+)*)$');
const enumValuePattern = pattern('^@?[a-z_]\\w*$');
const personaPattern = pattern('^persona\\s+([A-Za-z_]\\w*)$');
const personaPolicyPattern = pattern('^policy\\s+([A-Za-z_]\\w*)$');
const tabIndentPattern = /^[ ]*\t/;
const primitiveTypes = ['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime'];

// Top-level, module and feature constructs the C# compiler knows that this compiler does not model. They
// are skipped whole, not reported.
const opaqueTopLevel = new Set(['policy', 'authentication', 'seed', 'ui', 'theme', 'trigger', 'layout', 'behavior']);

// What belongs in a module or feature body; at the top level of a whole document it gets a hint saying so.
const bodyKeywords = new Set(['slice', 'feature', 'description', 'documentation', 'authorize', 'screen', 'dialog', 'form', 'contribute', 'on', 'uses']);

// Parses one document into its application syntax - the port of the C# ScreenplayParser. The placement says
// where the document's top level belongs: the application, unless an import placed it in a module or feature.
export function parseApplication(context: ParserContext, lines: readonly SourceLine[], placement: PlayPlacement = documentPlacement): ApplicationSyntax {
    warnOnTabIndentation(context, lines);
    const moduleBody = placement.length === 1 ? new ModuleBody(placement[0]) : undefined;
    const featureBody = placement.length > 1 ? new FeatureBody(placement[placement.length - 1]) : undefined;
    const placedBody = moduleBody ?? featureBody;
    let domain: DomainSyntax | null = null;
    const imports: ImportSyntax[] = [];
    const fileImports: FileImportSyntax[] = [];
    const concepts: ConceptSyntax[] = [];
    const types: TypeSyntax[] = [];
    const systems: SystemSyntax[] = [];
    const eventSources: EventSourceSyntax[] = [];
    const modules: ModuleSyntax[] = [];
    const personas: PersonaSyntax[] = [];
    const policies: PolicySyntax[] = [];
    const purposes: PurposeSyntax[] = [];
    const seeds: SeedSyntax[] = [];
    const examples: SpecificationExampleSyntax[] = [];
    const declaredTriggers: NonNullable<ApplicationSyntax['declaredTriggers']>[number][] = [];
    let sawOtherConstruct = false;
    for (let line = context.reader.peekSignificant(); line !== undefined; line = context.reader.peekSignificant()) {
        context.reader.takeSignificant();
        const keyword = firstWord(line.content);
        if (keyword === 'domain') {
            context.authoredDeclarations = true;
            domain = parseDomain(context, line, domain, sawOtherConstruct);
            continue;
        }
        // The C# parser asks whether anything was declared before the domain, so an import it rejected,
        // a file import and an unknown construct do not count.
        sawOtherConstruct ||= declaresConstruct(keyword, line, placement);
        context.authoredDeclarations ||= keyword !== 'import' && declaresConstruct(keyword, line, placement);
        if (keyword === 'import' && isFileImport(line.content)) {
            // A top level import belongs to whatever the document's top level is - the application, or the
            // module or feature the document was itself imported into.
            if (placedBody !== undefined) {
                placedBody.tryParse(context, line);
            } else {
                parseFileImport(context, line, fileImports);
            }
        } else if (keyword === 'import') {
            const match = importPattern.exec(line.content);
            if (match === null) {
                context.error(DiagnosticCodes.InvalidImportDeclaration, `Invalid import '${line.content}' - expected 'import <Qualified.Name>'`, locationOf(line));
            } else {
                imports.push({ kind: 'ImportSyntax', qualifiedName: match[1], location: locationOf(line) });
            }
        } else if (keyword === 'example') {
            if (placedBody !== undefined) placedBody.tryParse(context, line);
            else examples.push(parseExample(context, line));
        } else if (keyword === 'eventsource') {
            eventSources.push(parseEventSource(context, line));
        } else if (keyword === 'system') {
            systems.push(parseSystem(context, line));
        } else if (keyword === 'concept') {
            concepts.push(parseConcept(context, line));
        } else if (keyword === 'type') {
            types.push(parseType(context, line));
        } else if (keyword === 'module' && !isDocumentPlacement(placement)) {
            parseModuleInPlacedFile(context, line, placement, moduleBody);
        } else if (keyword === 'module') {
            modules.push(parseModule(context, line));
        } else if (keyword === 'persona') {
            personas.push(parsePersona(context, line));
        } else if (keyword === 'seed') {
            seeds.push(parseSeed(context.valueContext, line));
        } else if (keyword === 'purpose' && placedBody !== undefined) {
            placedBody.tryParse(context, line);
        } else if (keyword === 'purpose') {
            purposes.push(parsePurpose(context, line));
        } else if (keyword === 'policy') {
            // Legacy policies were opaque: enrich their structure without adding diagnostics.
            policies.push(parsePolicy(context, line));
        } else if (keyword === 'trigger') {
            const data = parseTriggerDeclaration(context, line);
            const name = /^trigger\s+([A-Za-z_]\w*)$/.exec(line.content)?.[1];
            if (name !== undefined) declaredTriggers.push({ name, data });
        } else if (opaqueTopLevel.has(keyword)) {
            if (keyword === 'behavior' || keyword === 'layout') collectInputUses(context, line);
            else context.skipOpaqueBlock(line.indent);
        } else if (placedBody?.tryParse(context, line) !== true) {
            reportUnexpectedTopLevel(context, line, placement);
        }
    }
    if (moduleBody !== undefined) {
        modules.unshift(moduleBody.build(context.start, true));
    } else if (featureBody !== undefined) {
        modules.unshift(place(placement, featureBody.build(context.start, true), context.start));
    }
    const root: ApplicationSyntax = { kind: 'ApplicationSyntax', sourceOptions: context.sourceOptions, domain, imports, concepts, types, systems, eventSources, examples, modules, personas, policies, purposes, seeds, declaredTriggers, fileImports, location: context.start };
    if (context.authoredDeclarations) recordAuthoredDocument(root);
    return root;
}

function declaresConstruct(keyword: string, line: SourceLine, placement: PlayPlacement): boolean {
    if (keyword === 'import') {
        return importPattern.test(line.content);
    }
    if (keyword === 'module') {
        return isDocumentPlacement(placement);
    }
    return keyword === 'purpose' || keyword === 'example' || keyword === 'eventsource' || keyword === 'system' || keyword === 'concept' || keyword === 'type' || keyword === 'persona' || opaqueTopLevel.has(keyword);
}

function parseModuleInPlacedFile(context: ParserContext, line: SourceLine, placement: PlayPlacement, moduleBody: ModuleBody | undefined): void {
    const name = modulePattern.exec(line.content)?.[1] ?? '';

    // Restating the module a file is placed in says nothing new, so its body simply joins the placement.
    if (moduleBody !== undefined && name === placement[0]) {
        context.authoredDeclarations = true;
        moduleBody.parseChildren(context, line);
        return;
    }
    context.error(DiagnosticCodes.ModuleInPlacedFile,
        `This file is imported into ${describePlacement(placement)}, so it cannot declare module '${name}' - import it at the top level of a document instead`, locationOf(line));
    context.skipBlock(line.indent);
}

function reportUnexpectedTopLevel(context: ParserContext, line: SourceLine, placement: PlayPlacement): void {
    const word = firstWord(line.content);
    if (!isDocumentPlacement(placement)) {
        const expected = placement.length === 1 ? moduleBodyExpected : featureBodyExpected;
        context.error(DiagnosticCodes.UnexpectedInPlacedFile,
            `Unexpected '${word}' in a file imported into ${describePlacement(placement)} - expected an application declaration or ${expected}`, locationOf(line));
    } else {
        const hint = bodyKeywords.has(word)
            ? ` - '${word}' belongs in a module or feature; wrap it in one, or import this file from inside one`
            : ' - expected domain, import, concept, type, policy, persona, authentication, module, seed, trigger, behavior, ui profile, theme or layout';
        context.error(DiagnosticCodes.UnknownTopLevelConstruct, `Unexpected '${word}' at the top level${hint}`, locationOf(line));
    }
    context.skipBlock(line.indent);
}

// Wraps the feature a file is placed in in the features and module around it, each marked as a placement.
function place(placement: PlayPlacement, innermost: FeatureSyntax, start: SourceLocation): ModuleSyntax {
    let feature = innermost;
    for (let index = placement.length - 2; index >= 1; index--) {
        feature = {
            kind: 'FeatureSyntax', name: placement[index], description: null, authorize: null,
            features: [feature], slices: [], fileImports: [], isPlacement: true, location: start,
        };
    }
    return {
        kind: 'ModuleSyntax', name: placement[0], description: null, authorize: null,
        features: [feature], fileImports: [], isPlacement: true, location: start,
    };
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
    const attributes = parseComplianceMarkers(context, line, attributeText);
    if (type !== 'Enum' && !primitiveTypes.includes(type)) {
        context.error(DiagnosticCodes.UnknownPrimitiveType, `Unknown primitive type '${type}' - expected ${primitiveTypes.join(', ')} or Enum`, locationOf(line));
    }
    const values: string[] = [];
    let description: string | null = null;
    const validations: ValidateSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'description' && (type !== 'Enum' || child.content !== 'description' || context.peekChild(child.indent)?.content.startsWith('```') === true)) {
            description = parseDescription(context, child, description, `Concept '${name}'`);
        } else if (isFileDirective(child)) {
            continue;
        } else if (firstWord(child.content) === 'validate') {
            if (type === 'Enum' && child.content === 'validate' && context.peekChild(child.indent) === undefined) {
                context.warning(DiagnosticCodes.ValidateReadAsEnumerationBlock,
                    `'validate' in enumeration concept '${name}' declares an empty validate block, not a value named 'validate' - write '@validate' for the value`,
                    locationOf(child));
            }
            const parsed = parseValidate(context, child, 'concept');
            if (parsed !== undefined) validations.push(parsed);
        } else if (parseComplianceDirective(context, child, name, attributes)) {
            // Compliance directives are modeled by their original concept marker.
        } else if (type === 'Enum' && enumValuePattern.test(child.content)) {
            values.push(unescapeIdentifier(child.content));
        } else if (type === 'Enum') {
            context.error(DiagnosticCodes.InvalidEnumerationValue, `Invalid enum value '${child.content}' - expected an identifier`, locationOf(child));
        } else {
            context.error(DiagnosticCodes.UnknownConceptDirective, `Unexpected '${child.content}' in concept body - expected validate, 'file <path>' or '<attribute> reason "<text>"'`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return { kind: 'ConceptSyntax', name, type, attributes, values, validations, description, location: locationOf(line) };
}

// 'persona <Name>' with an optional description and the policies it holds - the port of the C# ParsePersona.
function parsePersona(context: ParserContext, line: SourceLine): PersonaSyntax {
    const name = personaPattern.exec(line.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidPersonaDeclaration, `Invalid persona declaration '${line.content}' - expected 'persona <Name>'`, locationOf(line));
    }
    let description: string | null = null;
    const policies: string[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword === 'description') {
            description = parseDescription(context, child, description, `Persona '${name}'`);
        } else if (keyword === 'policy') {
            const policy = personaPolicyPattern.exec(child.content);
            if (policy === null) {
                context.error(DiagnosticCodes.InvalidPersonaPolicyReference, `Invalid policy reference '${child.content}' - expected 'policy <Name>'`, locationOf(child));
            } else {
                policies.push(policy[1]);
            }
        } else {
            context.error(DiagnosticCodes.UnknownPersonaDirective, `Unexpected '${keyword}' in persona body - expected description or policy`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return { kind: 'PersonaSyntax', name, description, policies, location: locationOf(line) };
}

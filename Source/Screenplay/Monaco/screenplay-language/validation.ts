// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes, legacyOptionalTypeLength } from '@cratis/screenplay-compiler';
import { AnalysisDiagnostic, responseAnalysis } from './response-analysis';
import { validateInlineEvents } from './inline-event-validation';
import { causedByProperties, contextRoots, identityProperties, primitiveTypes, sliceTypes } from './language';
import { DiagnosticCode, diagnosticCodes } from './diagnostic-codes';
import { enclosingChain, fenceMap, indentOf } from './document-context';
import { eventAnalysisSource } from './event-analysis-source';
import { resolveEventContextPath } from './event-context';
import {
    DocumentSymbols,
    PropertySymbol,
    propertyTypeReference,
    knownEventNames,
    knownTypeNames,
    mergeSymbols,
    scanDocument,
    symbolsForBuffer,
} from './symbols';
import { fileImportOn, isFileImportLine } from './file-imports';

export type ValidationSeverity = 'error' | 'warning' | 'information';

export interface ValidationIssue {
    line: number;
    startColumn: number;
    endColumn: number;
    message: string;
    severity: ValidationSeverity;

    // The compiler code for this condition, absent on the structural checks the editor makes and the
    // compiler does not. See ./diagnostic-codes.ts.
    code?: DiagnosticCode;
}

// What a document is validated against beyond itself.
export interface ValidationContext {
    // What the rest of the application declares - the other .play files of a folder, or the files an import
    // brings in - so a name declared in another file is not reported unknown. Merge the scanned symbols of
    // those files with mergeSymbols. The document's own declarations are always known.
    application?: DocumentSymbols;
    placement?: readonly string[];
    path?: string;
    compilerDiagnostics?: readonly AnalysisDiagnostic[];
}

function issue(
    severity: ValidationSeverity,
    line: number,
    startColumn: number,
    length: number,
    message: string,
    code: DiagnosticCode,
): ValidationIssue {
    return { severity, message, line, startColumn, endColumn: startColumn + length, code };
}

function tokenIssue(
    severity: ValidationSeverity,
    line: number,
    text: string,
    token: string,
    message: string,
    code: DiagnosticCode,
): ValidationIssue {
    const column = text.indexOf(token) + 1;
    return issue(severity, line, column, token.length, message, code);
}

// Reports every property whose type reference names nothing the document declares, and
// every command that marks more than one property as its identifier.
function validateDeclarations(lines: string[], symbols: DocumentSymbols, application: DocumentSymbols): ValidationIssue[] {
    const issues: ValidationIssue[] = [];
    const types = new Set([...knownTypeNames(symbols, primitiveTypes), ...knownTypeNames(application, [])]);

    const checkProperties = (properties: PropertySymbol[], owner: string) => {
        for (const property of properties.filter(
            (candidate) => !types.has(propertyTypeReference(candidate).name),
        )) {
            const bare = propertyTypeReference(property).name;
            issues.push(
                issue(
                    'warning',
                    property.line,
                    property.sourceType!.startColumn,
                    property.sourceType!.text.length,
                    bare === 'optional'
                        ? `Unknown type 'optional' on '${property.name}' of ${owner} — did you forget the type before 'optional'?`
                        : `Unknown type '${bare}' on '${property.name}' of ${owner} — declare it with 'concept ${bare} : <Primitive>' or 'type ${bare}'.`,
                    diagnosticCodes.unknownType,
                ),
            );
        }
    };

    for (const type of symbols.types) checkProperties(type.properties, `type '${type.name}'`);
    for (const event of symbols.events) checkProperties(event.properties, `event '${event.name}'`);
    for (const query of symbols.queries) checkProperties(query.parameters, `query '${query.name}'`);
    for (const command of symbols.commands) {
        checkProperties(command.properties, `command '${command.name}'`);
        const identifiers = command.properties.filter((property) => property.isIdentifier);
        for (const extra of identifiers.slice(1)) {
            issues.push(
                tokenIssue(
                    'error',
                    extra.line,
                    lines[extra.line],
                    'identifier',
                    `Command '${command.name}' already marks '${identifiers[0].name}' as identifier — only one property can be the identifier.`,
                    diagnosticCodes.duplicateCommandIdentifier,
                ),
            );
        }
    }

    const declared = new Map<string, number>();
    for (const declaration of [...symbols.concepts, ...symbols.types]) {
        const first = declared.get(declaration.name);
        if (first === undefined) {
            declared.set(declaration.name, declaration.line);
            continue;
        }
        issues.push(
            tokenIssue(
                'error',
                declaration.line,
                lines[declaration.line],
                declaration.name,
                `Duplicate declaration of '${declaration.name}' — concept and type names must be unique.`,
                diagnosticCodes.duplicateDeclaration,
            ),
        );
    }

    return issues;
}

// Validates a Screenplay document without any editor dependency — both the Monaco
// service and the VSCode extension adapt these issues to their marker/diagnostic APIs.
//
// The checks read the document line by line, never as a whole document's top level, so a file an import
// places in a module or feature - holding slices or features at its top level - validates as it is written.
// Where it is placed, and whether its imports resolve, is the compiler's to say.
export function validateLines(lines: string[], context: ValidationContext = {}): ValidationIssue[] {
    const fences = fenceMap(lines);
    const scanned = scanDocument(lines);
    const application = context.application ?? mergeSymbols();
    const input = symbolsForBuffer(lines, { ...application, authoringPath: context.path ?? application.authoringPath, authoringPlacement: context.placement ?? application.authoringPlacement });
    const analysis = responseAnalysis(lines, input.authoringDocuments ?? input.authoringSources?.filter(source => source !== lines.join('\n')), input.authoringPlacement, input.authoringPath, input.authoringPlacementResolved);
    const routeLines = new Set(analysis.eventSources.routes.map(route => route.location.line - 1));
    const symbols = { ...scanned, commands: scanned.commands.map(command => ({ ...command,
        properties: command.properties.filter(property => !routeLines.has(property.line)),
        produces: command.produces?.filter(production => !analysis.operationProductionLines?.has(production.line)),
        productionHeaders: command.productionHeaders?.filter(line => !analysis.operationProductionLines?.has(line)),
    })) };
    const events = new Set([...knownEventNames(symbols), ...knownEventNames(application)]);
    const policies = new Set([...symbols.policies, ...application.policies].map((policy) => policy.name));
    const issues: ValidationIssue[] = validateDeclarations(lines, symbols, application);
    issues.push(...validateProductionDestinations(lines, symbols));
    // One parser pass covers committed types, including query results and trigger data, without
    // speculative property scans mistaking tags, paths, strings or code for optionality.
    const optionalCodes = new Set<string>([DiagnosticCodes.LegacyOptionalSuffix, DiagnosticCodes.InvalidOptionalModifierOrder, DiagnosticCodes.OptionalReadsNotSupported]);
    const policyCodes = new Set<string>([DiagnosticCodes.UnexpectedTokenInPolicyCondition, DiagnosticCodes.ExpectedPolicyCondition, DiagnosticCodes.UnclosedPolicyConditionGroup, DiagnosticCodes.ExpectedRoleName, DiagnosticCodes.ExpectedClaimName, DiagnosticCodes.ExpectedClaimMatches, DiagnosticCodes.ExpectedClaimMatchTarget]);
    const consistencyCodes = new Set<string>([DiagnosticCodes.UnknownEvent, DiagnosticCodes.UnknownReadModelProperty, DiagnosticCodes.PiiNotSupportedOnIdentifier]);
    const timelineCodes = new Set<string>([DiagnosticCodes.EventFromLaterSlice, DiagnosticCodes.TimelineCycleGroup]);
    const dependencyCodes = new Set<string>([DiagnosticCodes.UndeclaredDependency, DiagnosticCodes.UnusedDependencyDeclaration, DiagnosticCodes.InvalidDependencyTarget, DiagnosticCodes.RepeatedDependencyDeclaration, DiagnosticCodes.MutualDependencyDeclarations, DiagnosticCodes.AmbiguousReference]);
    // These parser diagnostics must reach the marker adapter. C#-only completeness and reference
    // checks are not synthesized here, but are preserved when a caller supplies compiler diagnostics.
    const authoringCodes = new Set<string>([DiagnosticCodes.UnsupportedInteractionAlternatives, DiagnosticCodes.MixedInteractionAlternatives, DiagnosticCodes.InteractionAlternativeWithoutActions, DiagnosticCodes.InlineInteractionAlternative, DiagnosticCodes.LegacyInteractionWhere, DiagnosticCodes.InvalidDocumentation, DiagnosticCodes.ConflictingDocumentationAcrossFiles, DiagnosticCodes.InvalidAbsentReadModelStep, DiagnosticCodes.UnknownConstraintProperty, DiagnosticCodes.OmittedProductionDestination, DiagnosticCodes.InvalidSpecificationExample, DiagnosticCodes.DuplicateSpecificationAssignment]);
    const complianceCodes = new Set<string>([DiagnosticCodes.UnknownConceptDirective, DiagnosticCodes.AttributeReasonWithoutAttribute, DiagnosticCodes.DuplicateAttributeReason, DiagnosticCodes.LegacyComplianceMarker, DiagnosticCodes.UnknownComplianceMarker, DiagnosticCodes.InvalidSecretScope, DiagnosticCodes.DuplicateSecretScope, DiagnosticCodes.SecretScopeIgnoredForPii, DiagnosticCodes.InvalidPersonalDataQualifier, DiagnosticCodes.DuplicateSpecialCategory]);
    const compilerUnknownEventLines = new Set<number>();
    for (const diagnostic of context.compilerDiagnostics ?? analysis.diagnostics) {
        if (!complianceCodes.has(diagnostic.code) && !optionalCodes.has(diagnostic.code) && !policyCodes.has(diagnostic.code) && !consistencyCodes.has(diagnostic.code) && !timelineCodes.has(diagnostic.code) && !dependencyCodes.has(diagnostic.code) && !authoringCodes.has(diagnostic.code) && diagnostic.code !== DiagnosticCodes.InvalidSpecificationExampleBody && diagnostic.code !== DiagnosticCodes.UnknownRuleImplementationDirective && diagnostic.code !== DiagnosticCodes.InvalidValidationRule && diagnostic.code !== DiagnosticCodes.RepeatedDeclarationAcrossFiles && !/^PLAY049[0-9]$|^PLAY050[0-7]$|^PLAY054[7-9]$|^PLAY055[01]$|^PLAY048[2-9]$|^PLAY004[56]$|^PLAY053[0-9]$|^PLAY054[0-6]$|^PLAY034[1-8]$|^PLAY0557$/.test(diagnostic.code)) continue;
        const line = diagnostic.location.line - 1;
        if (diagnostic.code === DiagnosticCodes.OmittedProductionDestination && issues.some(existing => existing.code === diagnostic.code && existing.line === line)) continue;
        if (diagnostic.code === DiagnosticCodes.UnknownEvent) compilerUnknownEventLines.add(line);
        const length = legacyOptionalTypeLength(lines[line], diagnostic) || lines[line].length - diagnostic.location.column + 1;
        issues.push(issue(diagnostic.severity, line, diagnostic.location.column, length, diagnostic.message, diagnostic.code as DiagnosticCode));
    }

    const checkEvent = (line: number, text: string, name: string) => {
        if (!events.has(name) && !compilerUnknownEventLines.has(line)) {
            issues.push(
                tokenIssue(
                    'warning',
                    line,
                    text,
                    name,
                    `Unknown event type '${name}' — declare it in a slice or import it.`,
                    diagnosticCodes.unknownEvent,
                ),
            );
        }
    };

    let authorizeIndent = -1;

    for (let index = 0; index < lines.length; index++) {
        const line = lines[index];
        const trimmed = line.trim();

        if (fences[index] || trimmed.length === 0) continue;

        const leading = line.match(/^[ ]*(\t+)/);
        if (leading) {
            issues.push(
                issue(
                    'warning',
                    index,
                    leading[0].length - leading[1].length + 1,
                    leading[1].length,
                    'Screenplay is indentation-based — use spaces, not tabs.',
                    diagnosticCodes.tabIndentation,
                ),
            );
        }

        // Policy references continue on indented lines below an authorize clause.
        if (authorizeIndent >= 0) {
            if (
                indentOf(line) > authorizeIndent &&
                /^(?:or\s+)?[A-Z]\w*(?:\s+or\s+[A-Z]\w*)*$/.test(trimmed)
            ) {
                for (const name of trimmed.split(/\s+/).filter((token) => token !== 'or')) {
                    if (!policies.has(name)) {
                        issues.push(
                            tokenIssue(
                                'warning',
                                index,
                                line,
                                name,
                                `Unknown policy '${name}' — declare it with 'policy ${name}'.`,
                                diagnosticCodes.unknownPolicy,
                            ),
                        );
                    }
                }
                continue;
            }
            authorizeIndent = -1;
        }

        const fileImportIssue = validateImport(lines, fences, index);
        if (fileImportIssue) issues.push(fileImportIssue);

        const slice = trimmed.match(/^slice\s+(\w+)/);
        if (slice && !sliceTypes.includes(slice[1])) {
            issues.push(
                tokenIssue(
                    'error',
                    index,
                    line,
                    slice[1],
                    `Unknown slice type '${slice[1]}' — expected ${sliceTypes.join(', ')}.`,
                    diagnosticCodes.unknownSliceType,
                ),
            );
        }

        const concept = trimmed.match(/^concept\s+\w+\s*:\s*(\w+)/);
        if (concept && concept[1] !== 'Enum' && !primitiveTypes.includes(concept[1])) {
            issues.push(
                tokenIssue(
                    'error',
                    index,
                    line,
                    concept[1],
                    `Unknown primitive type '${concept[1]}' — expected ${primitiveTypes.join(', ')} or Enum.`,
                    diagnosticCodes.unknownPrimitiveType,
                ),
            );
        }

        const authorize = trimmed.match(/^authorize\s+(.*)$/);
        if (authorize) {
            authorizeIndent = indentOf(line);
            for (const name of authorize[1].split(/\s+/).filter((token) => token !== 'or' && token.length > 0)) {
                if (!policies.has(name)) {
                    issues.push(
                        tokenIssue(
                            'warning',
                            index,
                            line,
                            name,
                            `Unknown policy '${name}' — declare it with 'policy ${name}'.`,
                            diagnosticCodes.unknownPolicy,
                        ),
                    );
                }
            }
        }

        const reactsOn = trimmed.match(/^on\s+([A-Z]\w*)\s*$/);
        if (reactsOn) checkEvent(index, line, reactsOn[1]);

        const produces = trimmed.match(/^produces\s+([A-Z]\w*)\s*$/);
        if (produces && !analysis.operationProductionLines?.has(index)) checkEvent(index, line, produces[1]);

        if (/^produces\s+when\b/.test(trimmed) && !analysis.operationProductionLines?.has(index)) {
            for (let next = index + 1; next < lines.length; next++) {
                const candidate = lines[next];
                if (fences[next] || candidate.trim().length === 0) continue;
                if (indentOf(candidate) > indentOf(line)) {
                    const name = candidate.trim().match(/^([A-Z]\w*)\s*$/);
                    if (name) checkEvent(next, candidate, name[1]);
                }
                break;
            }
        }

        const uniqueEvent = trimmed.match(/^unique\s+event\s+([A-Z]\w*)/);
        if (uniqueEvent) checkEvent(index, line, uniqueEvent[1]);

        const uniqueProperty = trimmed.match(/^unique\s+[a-z_]\w*\s+on\s+([A-Z]\w*)/);
        if (uniqueProperty) checkEvent(index, line, uniqueProperty[1]);

        // Every $context. path must name something CommandContext or QueryContext carries.
        for (const path of line.match(/\$context\.[\w.]+/g) ?? []) {
            const segments = path.substring('$context.'.length).split('.');
            if (!contextRoots.includes(segments[0])) {
                issues.push(
                    tokenIssue(
                        'warning',
                        index,
                        line,
                        path,
                        `Unknown $context path '${segments.join('.')}' — expected one of ${contextRoots.join(', ')}.`,
                        diagnosticCodes.unknownContextPath,
                    ),
                );
            } else if (
                segments[0] === 'causedBy' &&
                segments.length > 1 &&
                !causedByProperties.includes(segments[1])
            ) {
                issues.push(
                    tokenIssue(
                        'warning',
                        index,
                        line,
                        path,
                        `Unknown $context.causedBy property '${segments[1]}' — expected ${causedByProperties.join(', ')}.`,
                        diagnosticCodes.unknownContextCausedByProperty,
                    ),
                );
            } else if (
                segments[0] === 'identity' &&
                segments.length > 1 &&
                !identityProperties.includes(segments[1])
            ) {
                issues.push(
                    tokenIssue(
                        'warning',
                        index,
                        line,
                        path,
                        `Unknown $context.identity property '${segments[1]}' — expected ${identityProperties.join(', ')}.`,
                        diagnosticCodes.unknownContextIdentityProperty,
                    ),
                );
            }
        }
    }

    issues.push(...validateEventContextPaths(lines, fences));
    issues.push(...validateInlineEvents(lines, symbols, application, analysis.operationProductionLines));

    const fenceLines = lines
        .map((line, index) => ({ line, index }))
        .filter(({ line }) => /^\s*```(?:[a-z]+)?\s*$/.test(line));
    if (fenceLines.length % 2 === 1) {
        const last = fenceLines[fenceLines.length - 1];
        issues.push(
            issue(
                'error',
                last.index,
                1,
                last.line.length + 1,
                'Unclosed inline code block — expected a closing ``` line.',
                diagnosticCodes.unclosedCodeBlock,
            ),
        );
    }

    return issues;
}

// This is advice, not a semantic default: accepting the typed workspace repair changes routing.
function validateProductionDestinations(lines: string[], symbols: DocumentSymbols): ValidationIssue[] {
    const issues: ValidationIssue[] = [];
    lines = eventAnalysisSource(lines);
    for (const command of symbols.commands) {
        const identifiers = command.properties.filter(property => property.isIdentifier && !propertyTypeReference(property).isOptional && !propertyTypeReference(property).isCollection);
        if (identifiers.length !== 1) continue;
        for (const production of command.produces ?? []) {
            if (production.inline || production.conditional || production.target !== undefined) continue;
            const line = production.line;
            issues.push(issue('information', line, indentOf(lines[line]) + 1, lines[line].trim().length,
                `Plain 'produces ${production.name}' omits its destination — use 'for ${identifiers[0].name}' to explicitly select the command's identifier.`,
                diagnosticCodes.omittedProductionDestination));
        }
    }
    return issues;
}

// Inside a module or feature an import names files, and a quoted import anywhere has the compiler's shape.
function validateImport(lines: string[], fences: boolean[], index: number): ValidationIssue | undefined {
    const line = lines[index];
    const trimmed = line.trim().replace(/\s*\/\/.*$/, '');
    if (!/^import\b/.test(trimmed) || fileImportOn(line, index)) return undefined;
    const indent = indentOf(line);
    const inBody = indent > 0 && ['module', 'feature'].includes(enclosingChain(lines, fences, index, indent)[0]);
    if (!inBody && !isFileImportLine(line)) return undefined;
    return issue(
        'error',
        index,
        indent + 1,
        trimmed.length,
        `Invalid import '${trimmed}' — inside a module or feature, import names files: 'import "<path or glob>"'.`,
        diagnosticCodes.invalidFileImport,
    );
}

// A mapping target: the first word of a mapping line, after the keyword that opens it if there is one.
const mappingTarget = /^(?:(?:increment|decrement|count|clear|add|subtract|set)\s+)?@?([$\w.]+)(?=\s|=|$)/;

// Every $eventContext.<path> - in an expression or as the dynamic key of a mapping target - checked against the
// event-context catalog the way the compiler checks it (Parsing/EventContextPathValidator.cs).
export function validateEventContextPaths(lines: string[], fences: boolean[]): ValidationIssue[] {
    const issues: ValidationIssue[] = [];
    const names = (members: readonly { name: string }[]) => members.map((member) => member.name).join(', ');

    for (let index = 0; index < lines.length; index++) {
        const line = lines[index];
        if (fences[index]) continue;

        for (const match of line.matchAll(/(\.)?\$eventContext((?:\.(?:\w+(?:\(\))?)?)*)/g)) {
            const token = match[0].substring(match[1]?.length ?? 0);
            const written = match[2];
            if (written.length === 0) {
                // A bare $eventContext names nothing only as a dynamic key; in an expression the compiler rejects it as invalid.
                if (match[1]) {
                    issues.push(issue('error', index, (match.index ?? 0) + 2, token.length, `'$eventContext' names no member - expected one of ${names(resolveEventContextPath('').expected)}.`, diagnosticCodes.missingEventContextPath));
                }
                continue;
            }

            const path = written.substring(1);
            const resolution = resolveEventContextPath(path);
            const column = (match.index ?? 0) + (match[1]?.length ?? 0) + 1;
            switch (resolution.status) {
                case 'missing':
                    issues.push(issue('error', index, column, token.length, `'$eventContext.${path}' names no member - expected one of ${names(resolution.expected)}.`, diagnosticCodes.missingEventContextPath));
                    break;
                case 'unknownMember':
                    issues.push(issue('warning', index, column, token.length, `Unknown event context member '${resolution.segment}' in '$eventContext.${path}' - expected one of ${names(resolution.expected)}.`, diagnosticCodes.unknownEventContextMember));
                    break;
                case 'unknownSubPath': {
                    const has = resolution.expected.length === 0 ? 'no members to address' : `members ${names(resolution.expected)}`;
                    issues.push(issue('warning', index, column, token.length, `Unknown event context path '$eventContext.${path}' - '${resolution.member?.name}' (${resolution.member?.type}) has ${has}.`, diagnosticCodes.unknownEventContextPath));
                    break;
                }
                case 'belowCollection':
                    issues.push(issue('error', index, column, token.length, `'$eventContext.${path}' cannot resolve - '${resolution.member?.name}' is a collection (${resolution.member?.type}) and cannot be addressed below.`, diagnosticCodes.eventContextPathBelowCollection));
                    break;
            }
        }

        // Only $eventContext resolves in a dynamic key; any other $ source is kept as the literal key text.
        const target = line.trim().match(mappingTarget)?.[1];
        const source = target?.match(/\.\$(\w+)/)?.[1];
        if (target && source && source !== 'eventContext') {
            issues.push(tokenIssue('warning', index, line, target, `Dynamic dictionary key '$${source}' in '${target}' is never resolved - only '$eventContext.<path>' is, so the literal text '$${source}' becomes the key.`, diagnosticCodes.unresolvedDynamicKeySource));
        }
    }

    return issues;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { validateHandlerContracts } from './CommandHandlerValidator';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { RequirementSyntax } from '../Syntax/Conditions';
import { misplacedRuleHintForCommand } from './MisplacedRuleHint';
import { parseCondition } from './ConditionParser';
import { AuthorizeSyntax } from '../Syntax/Authorization';
import { CommandSyntax, ValidateSyntax, ValidationRuleKind, ValidationRuleSyntax, ValidationSeverity } from '../Syntax/Commands';
import { PropertySyntax } from '../Syntax/Declarations';
import { CommandCalendarSyntax } from '../Syntax/Calendar';
import { CommandStreamSyntax } from '../Syntax/EventSources';
import { parseCommandStream } from './EventSourceParser';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { ProducesSyntax } from '../Syntax/Reactions';
import { CommandResponseSyntax } from '../Syntax/Responses';
import { pattern } from '../Text/patterns';
import { sourceStreamPattern } from '../Text/SourceStreamNames';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { combineAuthorize, parseAuthorize } from './AuthorizeParser';
import { parseCommandResponse, scalarResponsePattern } from './CommandResponseParser';
import { parseDescription } from './DescriptionParser';
import { parseDocumentation } from './DocumentationParser';
import { isCode, isFile, parseCode, parseFile, parseHandler, parseImplementationWrapper } from './ImplementationParser';
import { CodeBlockSyntax, FileReferenceSyntax, HandlerSyntax, ImplementationSyntax } from '../Syntax/Implementations';
import { parseMappingSource } from './ExpressionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { matchSubject, validateMatchOperand } from './MatchOperandValidation';
import { isReactionIdentityLine, misplacedReactionIdentity } from './ReactionIdentityParser';
import { commandReadSources } from './CommandReadSources';
import { dependencySources, ReadsSyntax, ConcurrencySyntax } from '../Syntax/DependencySources';
import { captureConcurrency } from './DependencySourceParser';
import { parseReads } from './ReadsParser';
import { parseProduces } from './ProducesParser';
import { reportInvalidModifierOrder, reportLegacyOptionalSuffix, tryParseProperty } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';
import { isFileDirectiveAmongProperties } from './FileReferences';

const header = pattern('^command\\s+([A-Za-z_]\\w*)$');
const routeHeader = sourceStreamPattern('^stream\\s+[A-Za-z_]\\w*\\.[A-Za-z_]\\w*$');
const calendarPrefix = pattern('^calendar\\s+(?:"|from\\s+\\$context\\.)');
const calendarPattern = pattern(`^calendar\\s+(?:"(${stringBodyPattern})"|from\\s+\\$context\\.([A-Za-z_][A-Za-z0-9_]*(?:\\.[A-Za-z_][A-Za-z0-9_]*)*))$`);
const severityPattern = pattern('\\bseverity\\s+(\\S+)$');
const messagePattern = pattern(`\\bmessage\\s+(?:"(${stringBodyPattern})"|(\\$strings\\.\\S*))$`);
const rulePattern = pattern('^([\\w.]+)\\s+(.+)$');
const operandPattern = pattern('^(?:(not empty|matches|not contains|contains|starts with|ends with|max|min|rule)(?:\\s+|$)|(length ==|decimals <=|all >=|all >|>=|<=|==|!=|>|<)\\s*)(.*)$');
const ruleNamePattern = sourceStreamPattern('^[A-Za-z_]\\w*$');
const optionalReads = pattern('^reads\\s+[A-Z]\\w*\\s+optional(?:\\s|$)');

// Every operand the pattern matches has a kind, so an operand never goes unrecognized here.
const operandKinds: Record<string, ValidationRuleKind> = {
    max: 'Max',
    min: 'Min',
    '>': 'GreaterThan',
    '>=': 'GreaterThanOrEqual',
    '<': 'LessThan',
    '<=': 'LessThanOrEqual',
    '==': 'Equal',
    '!=': 'NotEqual',
    'length ==': 'Length',
    'decimals <=': 'DecimalPlaces',
    matches: 'Matches',
    contains: 'Contains',
    'not contains': 'NotContains',
    'starts with': 'StartsWith',
    'ends with': 'EndsWith',
    'all >': 'AllGreaterThan',
    'all >=': 'AllGreaterThanOrEqual',
};

const severities: Record<string, ValidationSeverity> = { information: 'Information', warning: 'Warning', error: 'Error' };

// Command directives this compiler does not model. They are skipped whole.
const opaqueDirectives = new Set(['reads', 'concurrency']);

// The bare directives cannot take a type reference, so a line with property shape is a property whatever
// keyword it starts with - 'description String' declares a property called description.
const propertyShapedDirectives = new Set(['description', 'documentation', 'handler', 'concurrency']);

export function parseCommand(context: ParserContext, line: SourceLine): CommandSyntax {
    // Properties are leaves, not indentation owners. Resolve ambiguous returns spelling
    // before the committed pass decides whether its deeper lines belong to a response.
    const names = new Set<string>();
    const lookahead = new ParserContext(context.reader.fork(), context.path, context.languages);
    lookahead.sourceOptions = context.sourceOptions;
    lookahead.streamCandidates = context.streamCandidates;
    parseCommandBody(lookahead, line, undefined, names);
    return parseCommandBody(context, line, names);
}

function parseCommandBody(context: ParserContext, line: SourceLine, responseNames?: ReadonlySet<string>, discoveredNames?: Set<string>): CommandSyntax {
    const name = header.exec(line.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidCommandDeclaration, `Invalid command declaration '${line.content}' - expected 'command <Name>'`, locationOf(line));
    }
    const state = createCommandBodyState(name, responseNames);
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        dispatchCommandDirective(context, child, state);
    }
    return buildCommand(context, line, state, discoveredNames);
}

// What the command body loop gathers while it reads the directives of one command.
interface CommandBodyState {
    readonly name: string;
    readonly responseNames: ReadonlySet<string> | undefined;
    readonly properties: PropertySyntax[];
    readonly responses: { line: SourceLine; candidate: PropertySyntax | null; response: CommandResponseSyntax | null }[];
    identifier: PropertySyntax | undefined;
    readonly validations: ValidateSyntax[];
    readonly produces: ProducesSyntax[];
    readonly reads: { readModel: string; alias: string | null }[];
    readonly referenceReads: ReadsSyntax[];
    concurrency: ConcurrencySyntax | undefined;
    description: string | null;
    documentation: string | null;
    authorize: AuthorizeSyntax | null;
    authorizationDepthExceeded: boolean;
    handler: HandlerSyntax | null;
    readonly handlers: { handler: HandlerSyntax | null; location: SourceLocation }[];
    stream: CommandStreamSyntax | null;
    readonly streamCandidates: CommandStreamSyntax[];
    calendar: CommandCalendarSyntax | null;
}

// A command body rule: whether it takes a line - by keyword and, where the keyword alone cannot decide, by the line - and what to do with it.
interface CommandDirectiveRule {
    readonly matches: (keyword: string, line: SourceLine, property: PropertySyntax | undefined, context: ParserContext) => boolean;
    readonly parse: (context: ParserContext, line: SourceLine, state: CommandBodyState, property: PropertySyntax | undefined) => void;
}

function createCommandBodyState(name: string, responseNames: ReadonlySet<string> | undefined): CommandBodyState {
    return {
        name, responseNames, properties: [], responses: [], identifier: undefined, validations: [], produces: [], reads: [], referenceReads: [],
        concurrency: undefined, description: null, documentation: null, authorize: null, authorizationDepthExceeded: false, handler: null,
        handlers: [], stream: null, streamCandidates: [], calendar: null
    };
}

const isDirective = (name: string) => (keyword: string): boolean => keyword === name;

// The command body rules in the order they are tried: the first one that takes the line handles it, so a keyword with a property shaped rule keeps it
// ahead of its directive rule. A line no rule takes is a property, or an unexpected directive.
const commandDirectiveRules: readonly CommandDirectiveRule[] = [
    { matches: (keyword, line) => keyword === 'calendar' && calendarPrefix.test(line.content), parse: parseCalendarDirective },
    { matches: (keyword, line, property) => property !== undefined && (propertyShapedDirectives.has(keyword) || (keyword === 'validate' && line.content !== 'validate csharp')), parse: addPropertyDirective },
    { matches: isDirective('returns'), parse: parseReturnsDirective },
    { matches: (keyword, line, property, context) => keyword === 'stream' && routeHeader.test(line.content) && property !== undefined && context.streamCandidates?.hasSource(property.type.name.split('.')[0]) === true, parse: parseStreamDirective },
    { matches: (keyword, line) => keyword === 'runs' && isReactionIdentityLine(line), parse: (context, line) => { misplacedReactionIdentity(context, line); } },
    { matches: isDirective('description'), parse: (context, line, state) => { state.description = parseDescription(context, line, state.description, `Command '${state.name}'`); } },
    { matches: isDirective('documentation'), parse: (context, line, state) => { state.documentation = parseDocumentation(context, line, state.documentation, `Command '${state.name}'`); } },
    { matches: isDirective('authorize'), parse: parseAuthorizeDirective },
    { matches: isDirective('validate'), parse: parseValidateDirective },
    { matches: isDirective('produces'), parse: parseProducesDirective },
    { matches: isDirective('handler'), parse: (context, line, state) => { state.handler = parseHandler(context, line); state.handlers.push({ handler: state.handler, location: locationOf(line) }); } },
    { matches: keyword => opaqueDirectives.has(keyword), parse: parseOpaqueDirective }
];

function dispatchCommandDirective(context: ParserContext, child: SourceLine, state: CommandBodyState): void {
    const keyword = firstWord(child.content);
    const asProperty = tryParseProperty(child);
    const rule = commandDirectiveRules.find(candidate => candidate.matches(keyword, child, asProperty, context));
    if (rule !== undefined) {
        rule.parse(context, child, state, asProperty);
    } else if (asProperty !== undefined) {
        state.identifier = addProperty(context, state.properties, asProperty, state.name, child, state.identifier);
    } else {
        reportInvalidModifierOrder(context, child);
        context.error(DiagnosticCodes.UnknownCommandDirective, `Unexpected '${child.content}' in command body${misplacedRuleHintForCommand(child.content)}`, locationOf(child));
        context.skipBlock(child.indent);
    }
}

function addPropertyDirective(context: ParserContext, child: SourceLine, state: CommandBodyState, property: PropertySyntax | undefined): void {
    state.identifier = addProperty(context, state.properties, property!, state.name, child, state.identifier);
}

function parseCalendarDirective(context: ParserContext, child: SourceLine, state: CommandBodyState): void {
    const match = calendarPattern.exec(child.content);
    if (state.calendar !== null || match === null) {
        context.error(DiagnosticCodes.InvalidCommandCalendar, 'A command declares at most one \'calendar "<id>"\' or \'calendar from $context.<member>\'.', locationOf(child));
    } else {
        state.calendar = { kind: 'CommandCalendarSyntax', id: match[1] === undefined ? null : unescapeString(match[1]), contextMember: match[2] ?? null, location: locationOf(child) };
    }
}

function parseReturnsDirective(context: ParserContext, child: SourceLine, state: CommandBodyState, property: PropertySyntax | undefined): void {
    const scalar = scalarResponsePattern.exec(child.content);
    if (scalar !== null && !scalar[1].startsWith('@') && property !== undefined) {
        parseScalarReturns(context, child, state, property);
    } else if (property !== undefined) {
        state.identifier = addProperty(context, state.properties, property, state.name, child, state.identifier);
    } else if (reportInvalidModifierOrder(context, child)) {
        context.skipBlock(child.indent);
    } else {
        state.responses.push({ line: child, candidate: null, response: parseCommandResponse(context, child) });
    }
}

function parseScalarReturns(context: ParserContext, child: SourceLine, state: CommandBodyState, property: PropertySyntax): void {
    if (state.responseNames === undefined) {
        state.properties.push(property);
        state.responses.push({ line: child, candidate: property, response: null });
    } else if (state.responseNames.has(property.type.name)) {
        state.responses.push({ line: child, candidate: null, response: parseCommandResponse(context, child) });
    } else {
        state.identifier = addProperty(context, state.properties, property, state.name, child, state.identifier);
    }
}

function parseStreamDirective(context: ParserContext, child: SourceLine, state: CommandBodyState, property: PropertySyntax | undefined): void {
    const [source, streamName] = property!.type.name.split('.');
    const ambiguous = context.streamCandidates!.hasPropertyType(property!.type.name) && context.streamCandidates!.hasUniqueStream(source, streamName);
    const route = parseCommandStream(context, child, property!, ambiguous);
    if (state.stream !== null || state.streamCandidates.length > 0) context.error(DiagnosticCodes.InvalidCommandStream, 'A command declares at most one stream route.', locationOf(child));
    if (ambiguous || state.stream !== null) state.streamCandidates.push(route);
    else state.stream = route;
}

function parseAuthorizeDirective(context: ParserContext, child: SourceLine, state: CommandBodyState): void {
    const next = parseAuthorize(context, child);
    if (state.authorizationDepthExceeded) return;
    const previous = state.authorize;
    state.authorize = combineAuthorize(state.authorize, next, context);
    state.authorizationDepthExceeded = previous !== null && state.authorize === null;
}

function parseValidateDirective(context: ParserContext, child: SourceLine, state: CommandBodyState): void {
    const validate = parseValidate(context, child, 'command', state.name);
    if (validate !== undefined) state.validations.push(validate);
}

function parseProducesDirective(context: ParserContext, child: SourceLine, state: CommandBodyState): void {
    const production = parseProduces(context, child, true);
    if (production !== undefined) state.produces.push(production);
}

function parseOpaqueDirective(context: ParserContext, child: SourceLine, state: CommandBodyState): void {
    const keyword = firstWord(child.content);
    const reference = keyword === 'reads' ? parseReads(context, child) : undefined;
    if (reference !== undefined) state.referenceReads.push(reference);
    if (keyword === 'concurrency' && state.concurrency === undefined) state.concurrency = captureConcurrency(context, child);
    const read = /^reads\s+([A-Z]\w*)(?:\s+as\s+([a-z_]\w*))?(?:\s+by\s+([a-z_]\w*))?$/.exec(child.content);
    if (read !== null) state.reads.push({ readModel: read[1], alias: read[2] ?? null });
    if (optionalReads.test(child.content)) {
        context.error(DiagnosticCodes.OptionalReadsNotSupported, 'Optional reads are not yet supported (see #308).', locationOf(child));
    }
    context.skipOpaqueBlock(child.indent);
}

// Settles which 'returns' lines are the response and which are properties named like one, and the one response a command may declare.
function resolveResponse(context: ParserContext, state: CommandBodyState, discoveredNames: Set<string> | undefined): { response: CommandResponseSyntax | null; removed: Set<PropertySyntax> } {
    const { properties, responses } = state;
    const candidates = new Set(responses.flatMap(entry => entry.candidate === null ? [] : [entry.candidate]));
    const names = new Set(properties.filter(property => !candidates.has(property)).map(property => property.name));
    if (responses.some(entry => entry.candidate !== null && !names.has(entry.candidate.type.name) && entry.candidate.type.name !== 'returns')) names.add('returns');
    for (const name of names) discoveredNames?.add(name);
    let response: CommandResponseSyntax | null = null;
    const removed = new Set<PropertySyntax>();
    for (const entry of responses) {
        let parsed = entry.response;
        if (entry.candidate !== null) {
            if (!names.has(entry.candidate.type.name)) continue;
            removed.add(entry.candidate);
            parsed = { kind: 'ScalarCommandResponseSyntax', source: { kind: 'PropertyResponseSourceSyntax', property: entry.candidate.type.name, location: entry.candidate.type.location }, location: locationOf(entry.line) };
        }
        if (parsed === null) continue;
        if (response !== null) {
            context.error(DiagnosticCodes.InvalidCommandResponse, 'A command declares at most one unconditional response.', locationOf(entry.line));
        } else {
            response = parsed;
        }
    }
    return { response, removed };
}

function buildCommand(context: ParserContext, line: SourceLine, state: CommandBodyState, discoveredNames: Set<string> | undefined): CommandSyntax {
    const { name, properties, validations, produces, reads, referenceReads, concurrency, description, documentation, authorize, handler, handlers, stream, streamCandidates, calendar } = state;
    const { response, removed } = resolveResponse(context, state, discoveredNames);
    validateHandlerContracts(handlers, handler, produces.length > 0, context);
    const syntax: CommandSyntax = { kind: 'CommandSyntax', name, description, documentation, authorize, properties: properties.filter(property => !removed.has(property)), validations, produces, handler, response, stream, streamCandidates, ...(calendar === null ? {} : { calendar }), location: locationOf(line) };
    commandReadSources.set(syntax, reads);
    dependencySources.set(syntax, { reads: referenceReads, concurrency });
    return syntax;
}

function addProperty(context: ParserContext, properties: PropertySyntax[], property: PropertySyntax, commandName: string, line: SourceLine, identifier: PropertySyntax | undefined): PropertySyntax | undefined {
    reportLegacyOptionalSuffix(context, property.type, line);
    if (property.isIdentifier && identifier !== undefined) {
        context.error(DiagnosticCodes.DuplicateCommandIdentifier, `Command '${commandName}' already marks '${identifier.name}' as identifier - only one property can be the identifier`, property.location);
        property = { ...property, isIdentifier: false };
    }
    properties.push(property);
    return identifier ?? (property.isIdentifier ? property : undefined);
}

function parseValidationFile(context: ParserContext, header: SourceLine, body: SourceLine): ValidateSyntax | undefined {
    const file = parseFile(context, body);
    return file === undefined ? undefined : { kind: 'FileValidateSyntax', file, location: locationOf(header) };
}

function hasValidationOperand(line: SourceLine): boolean {
    const match = rulePattern.exec(line.content);
    if (match === null) return false;
    return startsWithRuleOperand(match[2]);
}

function startsWithRuleOperand(rule: string): boolean {
    return (firstWord(rule) === 'not' && firstWord(rule.substring('not'.length).trimStart()) === 'empty') ||
        firstWord(rule) === 'checksum' || operandPattern.test(rule);
}

/** Recognizes a declarative predicate, with any message or severity suffix; a named 'rule <Name>' is not one. */
export function isDeclarativePredicate(rule: string): boolean {
    return startsWithRuleOperand(rule) && firstWord(rule) !== 'rule';
}

function isValidationFileBody(context: ParserContext, header: SourceLine, body: SourceLine): boolean {
    if (!isFileDirectiveAmongProperties(body) || hasValidationOperand(body)) return false;
    const lookahead = context.reader.fork();
    lookahead.takeSignificant();
    const next = lookahead.peekSignificant();
    return next === undefined || next.indent <= header.indent;
}

// Reads a 'validate' block: declarative rules, or code - which is recognized but not modeled.
export function parseValidate(context: ParserContext, line: SourceLine, owner: 'command' | 'concept', ownerName: string): ValidateSyntax | undefined {
    if (line.content === 'validate') {
        const fence = context.peekChild(line.indent);
        if (fence !== undefined && isValidationFileBody(context, line, fence)) {
            context.reader.takeSignificant();
            return parseValidationFile(context, line, fence);
        }
        if (fence !== undefined && fence.content.startsWith('```')) {
            context.reader.takeSignificant();
            const code = parseCode(context.valueContext, fence);
            if (code === null) context.skipFencedBody();
            return { kind: 'CodeValidateSyntax', code, location: locationOf(line) };
        }
        const rules: ValidationRuleSyntax[] = [];
        const requirements: RequirementSyntax[] = [];
        let wrapperIndent: number | undefined;
        for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
            context.reader.takeSignificant();
            if (owner === 'concept') {
                if (context.sourceOptions.numericMode === 'legacy' && child.content.startsWith('```')) { context.skipFencedBody(); continue; }
                if (wrapperIndent !== undefined && child.indent <= wrapperIndent) wrapperIndent = undefined;
                if (wrapperIndent !== undefined || firstWord(child.content) === 'implementation') {
                    context.error(DiagnosticCodes.InvalidValidationRule, `Invalid validation rule '${child.content}'`, locationOf(child));
                    wrapperIndent ??= child.indent;
                    continue;
                }
            }
            if (firstWord(child.content) === 'require') {
                // Keep legacy operand enrichment quiet, but never discard the depth refusal.
                const condition = parseCondition(owner === 'concept' ? context.valueContext : context, child.content.substring('require'.length).trim(), locationOf(child), false, context);
                let message: string | null = null;
                let severity: ValidationSeverity = 'Error';
                let hasSeverity = false;
                for (let directive = context.peekChild(child.indent); directive !== undefined; directive = context.peekChild(child.indent)) {
                    context.reader.takeSignificant();
                    const matched = messagePattern.exec(directive.content);
                    if (matched !== null && matched.index === 0) message = matched[1] !== undefined ? unescapeString(matched[1]) : matched[2];
                    else if (firstWord(directive.content) === 'severity') {
                        if (!hasSeverity) severity = severities[directive.content.substring('severity'.length).trim()] ?? 'Error';
                        hasSeverity = true;
                    } else context.skipOpaqueBlock(directive.indent);
                }
                if (condition !== null) requirements.push({ kind: 'RequirementSyntax', condition, message, severity, location: locationOf(child) });
                continue;
            }
            const rule = parseValidationRule(context, owner === 'concept' ? { ...child, content: `value ${child.content}` } : child, owner, ownerName);
            if (rule !== undefined) {
                rules.push(rule);
            }
        }
        return { kind: 'DeclarativeValidateSyntax', rules, requirements, location: locationOf(line) };
    }
    if (owner === 'concept' && firstWord(line.content.substring('validate'.length).trimStart()) === 'checksum') {
        const ruleLine = { ...line, content: `value ${line.content.substring('validate'.length).trimStart()}` };
        const rule = parseValidationRule(context, ruleLine, owner, ownerName);
        return rule === undefined ? undefined : { kind: 'DeclarativeValidateSyntax', rules: [rule], requirements: [], location: locationOf(line) };
    }
    if (line.content === 'validate csharp') {
        const code = parseCode(context.valueContext, { ...line, content: 'csharp' });
        if (code === null) context.skipOpaqueBlock(line.indent);
        return { kind: 'CodeValidateSyntax', code, location: locationOf(line) };
    }
    context.error(DiagnosticCodes.InvalidValidateDeclaration, `Invalid validate declaration '${line.content}' - expected 'validate' or 'validate csharp'`, locationOf(line));
    context.skipBlock(line.indent);
    return undefined;
}

function parseValidationRule(context: ParserContext, line: SourceLine, owner: 'command' | 'concept', ownerName: string): ValidationRuleSyntax | undefined {
    // Checksum is a new declaration, not enrichment of a formerly opaque Legacy rule.
    const checksum = firstWord(rulePattern.exec(line.content)?.[2] ?? '') === 'checksum';
    const valueContext = owner === 'concept' && !checksum ? context.valueContext : context;
    const { content: withSeverity, message } = splitMessage(line.content);
    const severity = splitSeverity(valueContext, line, withSeverity);
    if (severity === undefined) {
        return undefined;
    }
    const match = rulePattern.exec(severity.content);
    if (match === null) {
        valueContext.error(DiagnosticCodes.InvalidValidationRule, `Invalid validation rule '${line.content}'`, locationOf(line));
        return undefined;
    }
    const parsed = parseRule(valueContext, match[2], line);
    if (parsed === undefined) {
        return undefined;
    }
    if (parsed.kind === 'Matches' && parsed.value !== null) validateMatchOperand(context, parsed.value, locationOf(line), line.raw, matchSubject(context, owner, ownerName, match[1]));
    const source = parsed.kind === 'Rule' ? parseRuleImplementation(context, line, owner === 'command') : { file: null, code: null, implementation: null };
    return {
        kind: 'ValidationRuleSyntax',
        property: match[1],
        rule: parsed.kind,
        value: parsed.value,
        message,
        severity: severity.severity,
        ...source,
        location: locationOf(line),
    };
}

function parseRuleImplementation(context: ParserContext, rule: SourceLine, commandOwner: boolean): { file: FileReferenceSyntax | null; code: CodeBlockSyntax | null; implementation: ImplementationSyntax | null } {
    const body = context.peekChild(rule.indent);
    if (body === undefined) return { file: null, code: null, implementation: null };
    context.reader.takeSignificant();
    const wrapped = commandOwner && firstWord(body.content) === 'implementation';
    const source = wrapped ? parseImplementationWrapper(context, body) : {
        file: isFile(body) ? parseFile(context, body) : null,
        code: !isFile(body) && isCode(context, body) ? parseCode(context, body) : null,
        implementation: null,
    };
    if (!wrapped && source.file === null && source.code === null) {
        context.error(DiagnosticCodes.UnknownRuleImplementationDirective, `Unexpected '${body.content}' in rule implementation - expected 'file <path>' or an inline code block`, locationOf(body));
        context.skipBlock(body.indent);
    }
    for (let extra = context.peekChild(rule.indent); extra !== undefined && (wrapped || (commandOwner && firstWord(extra.content) === 'implementation')); extra = context.peekChild(rule.indent)) {
        context.reader.takeSignificant();
        context.error(wrapped && firstWord(extra.content) === 'implementation' ? DiagnosticCodes.InvalidImplementationBlock : DiagnosticCodes.ConflictingImplementationSources,
            'A named rule has one implementation wrapper and cannot mix wrapped and direct sources.', locationOf(extra));
        if (isCode(context, extra)) parseCode(context, extra);
        else context.skipBlock(extra.indent);
    }
    return source;
}

function splitMessage(content: string): { content: string; message: string | null } {
    const match = messagePattern.exec(content);
    if (match === null) {
        return { content, message: null };
    }
    const message = match[1] !== undefined ? unescapeString(match[1]) : match[2];
    return { content: content.substring(0, match.index).trimEnd(), message };
}

function splitSeverity(context: ParserContext, line: SourceLine, text: string): { content: string; severity: ValidationSeverity } | undefined {
    const match = severityPattern.exec(text);
    if (match === null) {
        return { content: text, severity: 'Error' };
    }
    const severity = severities[match[1]];
    if (severity === undefined) {
        context.error(DiagnosticCodes.InvalidValidationSeverity, `Unknown validation severity '${match[1]}' - expected information, warning or error`, locationOf(line));
        return undefined;
    }
    return { content: text.substring(0, match.index).trimEnd(), severity };
}

function parseRule(context: ParserContext, rule: string, line: SourceLine): { kind: ValidationRuleKind; value: ExpressionSyntax | null } | undefined {
    if (rule === 'not empty') {
        return { kind: 'NotEmpty', value: null };
    }
    if (firstWord(rule) === 'checksum') {
        const algorithm = rule.substring('checksum'.length).trim();
        if (algorithm !== 'luhn' && algorithm !== 'mod11' && algorithm !== 'mod97') {
            context.error(DiagnosticCodes.InvalidGenerationArgument, 'Checksum validation requires luhn, mod11 or mod97.', locationOf(line));
            return undefined;
        }
        return { kind: 'Checksum', value: { kind: 'LiteralExpressionSyntax', value: algorithm, location: locationOf(line) } };
    }
    const operand = operandPattern.exec(rule);
    if (operand === null) {
        context.error(DiagnosticCodes.InvalidValidationRule, `Invalid validation rule '${line.content}'`, locationOf(line));
        return undefined;
    }
    const operator = operand[1] ?? operand[2];
    if (operator === 'rule') {
        const name = operand[3].trim();
        if (!ruleNamePattern.test(name)) {
            context.error(DiagnosticCodes.InvalidRuleName, `Invalid rule name '${name}' in '${line.content}' - expected 'rule <Name>' with an identifier`, locationOf(line));
            return undefined;
        }
        return { kind: 'Rule', value: { kind: 'PathExpressionSyntax', path: name, location: locationOf(line) } };
    }
    return { kind: operandKinds[operator], value: parseMappingSource(operand[3], locationOf(line), context) };
}

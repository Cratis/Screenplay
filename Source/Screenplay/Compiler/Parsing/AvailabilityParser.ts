// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { LogicalOperator } from '../Syntax/Authorization';
import { AudienceSyntax, AvailabilityRequirementSyntax, AvailabilitySyntax, AvailableSyntax } from '../Syntax/Availability';
import { pattern } from '../Text/patterns';
import { ConditionDepthBudget } from './ConditionDepthBudget';
import { parseCondition } from './ConditionParser';
import { parseDescription } from './DescriptionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const gateExpected = 'available for <Audience> [and|or <Audience> ...] | available when <condition> | available with <Availability> [and|or <Availability> ...]';
const gateKinds = new Set(['for', 'when', 'with']);

const audienceHeaderPattern = pattern('^audience\\s+([A-Za-z_]\\w*)$');
const availabilityHeaderPattern = pattern('^availability\\s+([A-Za-z_]\\w*)$');
const gateStartPattern = pattern('^available\\s+(for|when|with)(?:\\s|$)');
const gatePattern = pattern('^available\\s+(for|when|with)(?:\\s+(.*))?$');
const tokenPattern = new RegExp(pattern('\\(|\\)|[A-Za-z_]\\w*|\\S').source, 'gu');
const namePattern = pattern('^[A-Z]\\w*$');

/** The directive-location key of the audience at an index: the C# DirectiveLocationKeys.ForValue. */
export function audienceLocationKey(values: readonly string[], index: number): string {
    const value = values[index];
    let occurrence = 0;
    for (let previous = 0; previous < index; previous++) if (values[previous] === value) occurrence++;
    return `audience:${value.length}:${value}:${occurrence}`;
}

/** Whether a line in a body is an 'available for | when | with' gate. */
export function isAvailabilityGate(content: string): boolean {
    return gateStartPattern.test(content);
}

// The port of the C# AvailabilityParser.ParseAudienceDeclaration.
export function parseAudienceDeclaration(context: ParserContext, header: SourceLine): AudienceSyntax {
    const match = audienceHeaderPattern.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidAudienceDeclaration, `Invalid audience declaration '${header.content}' - expected 'audience <Name>'`, locationOf(header));
    }
    const name = match?.[1] ?? '';
    let description: string | null = null;
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'description') {
            description = parseDescription(context, line, description, `Audience '${name}'`);
            continue;
        }
        context.error(DiagnosticCodes.InvalidAudienceDeclaration, `Unexpected '${firstWord(line.content)}' in audience '${name}' - expected description`, locationOf(line));
        context.skipBlock(line.indent);
    }
    return { kind: 'AudienceSyntax', name, ...(description === null ? {} : { description }), location: locationOf(header) };
}

// The port of the C# AvailabilityParser.ParseAvailability.
export function parseAvailabilityDeclaration(context: ParserContext, header: SourceLine): AvailabilitySyntax {
    const match = availabilityHeaderPattern.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidAvailabilityDeclaration, `Invalid availability declaration '${header.content}' - expected 'availability <Name>'`, locationOf(header));
    }
    const name = match?.[1] ?? '';
    let description: string | null = null;
    const requirements: AvailabilityRequirementSyntax[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(line.content);
        if (keyword === 'description') {
            description = parseDescription(context, line, description, `Availability '${name}'`);
        } else if (gateKinds.has(keyword)) {
            const requirement = parseRequirement(context, keyword, line.content.substring(keyword.length), locationOf(line));
            if (requirement !== null) requirements.push(requirement);
        } else {
            context.error(DiagnosticCodes.InvalidAvailabilityDeclaration, `Unexpected '${keyword}' in availability '${name}' - expected description, for, when or with`, locationOf(line));
            context.skipBlock(line.indent);
        }
    }
    return { kind: 'AvailabilitySyntax', name, requirements, ...(description === null ? {} : { description }), location: locationOf(header) };
}

/** Parses one 'available ...' line and adds the gate when it is well formed. */
export function addAvailabilityGate(context: ParserContext, line: SourceLine, gates: AvailableSyntax[]): void {
    const gate = parseGate(context, line);
    if (gate !== null) gates.push(gate);
}

function parseGate(context: ParserContext, line: SourceLine): AvailableSyntax | null {
    const match = gatePattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidAvailableGate, `Invalid available gate '${line.content}' - expected '${gateExpected}'`, locationOf(line));
        return null;
    }
    const requirement = parseRequirement(context, match[1], match[2] ?? '', locationOf(line));
    return requirement === null ? null : { kind: 'AvailableSyntax', requirement, location: locationOf(line) };
}

function parseRequirement(context: ParserContext, kind: string, rawText: string, location: SourceLocation): AvailabilityRequirementSyntax | null {
    const text = rawText.trim();
    if (text.length === 0) {
        context.error(DiagnosticCodes.InvalidAvailableGate, `Expected something after '${kind}' - ${gateExpected}`, location);
        return null;
    }
    if (kind === 'when') {
        const condition = parseCondition(context, text, location);
        return condition === null ? null : { kind: 'ConditionAvailabilityRequirementSyntax', condition, location };
    }
    const tokens = [...text.matchAll(tokenPattern)].map(match => match[0]);
    return new RequirementParser(context, tokens, location, kind === 'for').parse();
}

// The port of the C# LogicalConditionParser for audience and availability names: 'or' over 'and' over a group or a name.
class RequirementParser {
    #position = 0;
    readonly #budget: ConditionDepthBudget;

    constructor(
        private readonly context: ParserContext,
        private readonly tokens: readonly string[],
        private readonly location: SourceLocation,
        private readonly audiences: boolean) {
        this.#budget = new ConditionDepthBudget(context, location);
    }

    parse(): AvailabilityRequirementSyntax | null {
        const requirement = this.#chain('or', 'Or', () => this.#chain('and', 'And', () => this.#groupOrName()));
        if (requirement !== null && this.#position < this.tokens.length) {
            this.context.error(DiagnosticCodes.UnexpectedTokenInAvailable, `Unexpected '${this.tokens[this.#position]}' in available`, this.location);
        }
        return requirement;
    }

    #chain(token: string, operator: LogicalOperator, operand: () => AvailabilityRequirementSyntax | null): AvailabilityRequirementSyntax | null {
        let left = operand();
        while (left !== null && this.tokens[this.#position] === token) {
            this.#position++;
            const right = operand();
            if (right === null) return null;
            const previous: AvailabilityRequirementSyntax = left;
            left = this.#budget.build<AvailabilityRequirementSyntax>(
                [previous, right],
                () => ({ kind: 'LogicalAvailabilityRequirementSyntax', left: previous, operator, right, location: this.location }));
        }
        return left;
    }

    #groupOrName(): AvailabilityRequirementSyntax | null {
        if (this.tokens[this.#position] !== '(') return this.#name();
        if (!this.#budget.enter()) return null;
        this.#position++;
        try {
            const requirement = this.#chain('or', 'Or', () => this.#chain('and', 'And', () => this.#groupOrName()));
            if (this.#budget.exceeded) return null;
            if (this.tokens[this.#position] === ')') {
                this.#position++;
            } else {
                this.context.error(DiagnosticCodes.UnclosedAvailableGroup, 'Expected \')\' in available', this.location);
            }
            return requirement;
        } finally { this.#budget.leave(); }
    }

    #name(): AvailabilityRequirementSyntax | null {
        const expected = this.audiences ? 'an audience name' : 'an availability name';
        if (this.#position >= this.tokens.length) {
            this.context.error(DiagnosticCodes.InvalidAvailableGate, `Expected ${expected}`, this.location);
            return null;
        }
        const token = this.tokens[this.#position++];
        if (!namePattern.test(token)) {
            this.context.error(DiagnosticCodes.InvalidAvailableGate, `Invalid name '${token}' - expected ${expected} written as a PascalCase identifier`, this.location);
            return null;
        }
        return this.audiences
            ? { kind: 'AudienceAvailabilityRequirementSyntax', audience: token, location: this.location }
            : { kind: 'AvailabilityReferenceSyntax', availability: token, location: this.location };
    }
}

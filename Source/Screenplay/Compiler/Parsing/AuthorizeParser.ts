// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AuthorizeSyntax, LogicalOperator, PolicyRequirementSyntax } from '../Syntax/Authorization';
import { pattern } from '../Text/patterns';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const continuationPattern = pattern('^(?:(?:or|and)\\s+)?[A-Za-z_(][\\w\\s()]*$');
const tokenPattern = new RegExp(pattern('\\(|\\)|[A-Za-z_]\\w*').source, 'gu');
const namePattern = pattern('^[A-Z]\\w*$');

// The port of the C# AuthorizeParser: the requirement may continue on the lines below the 'authorize', and
// two operands written side by side are joined by an implicit 'and'.
export function parseAuthorize(context: ParserContext, line: SourceLine): AuthorizeSyntax | null {
    let text = line.content.substring('authorize'.length);
    for (let child = context.peekChild(line.indent); child !== undefined && continuationPattern.test(child.content); child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        text += ` ${child.content}`;
    }
    const tokens = tokenize(text);
    const location = locationOf(line);
    if (tokens.length === 0) {
        context.error(DiagnosticCodes.AuthorizeWithoutPolicy, 'Expected at least one policy after \'authorize\'', location);
        return null;
    }
    const parser = new RequirementParser(context, tokens, location);
    const requirement = parser.parse();
    return requirement === null ? null : { kind: 'AuthorizeSyntax', requirement, location };
}

// Several 'authorize' lines on one construct all have to hold, so they combine with 'and'.
export function combineAuthorize(first: AuthorizeSyntax | null, next: AuthorizeSyntax | null): AuthorizeSyntax | null {
    if (first === null) return next;
    if (next === null) return first;
    return {
        kind: 'AuthorizeSyntax',
        requirement: { kind: 'LogicalPolicyRequirementSyntax', left: first.requirement, operator: 'And', right: next.requirement, location: first.location },
        location: first.location,
    };
}

function tokenize(text: string): string[] {
    const tokens: string[] = [];
    for (const token of text.match(tokenPattern) ?? []) {
        if (tokens.length > 0 && isOperandEnd(tokens[tokens.length - 1]) && isOperandStart(token)) {
            tokens.push('and');
        }
        tokens.push(token);
    }
    return tokens;
}

const isName = (token: string): boolean => token !== '(' && token !== ')' && token !== 'and' && token !== 'or';
const isOperandEnd = (token: string): boolean => token === ')' || isName(token);
const isOperandStart = (token: string): boolean => token === '(' || isName(token);

// The port of the C# LogicalConditionParser: 'or' over 'and' over a group or a policy name.
class RequirementParser {
    #position = 0;

    constructor(private readonly context: ParserContext, private readonly tokens: readonly string[], private readonly location: SourceLocation) {}

    parse(): PolicyRequirementSyntax | null {
        const requirement = this.#or();
        if (requirement !== null && this.#position < this.tokens.length) {
            this.context.error(DiagnosticCodes.UnexpectedTokenInAuthorize, `Unexpected '${this.tokens[this.#position]}' in authorize`, this.location);
        }
        return requirement;
    }

    #or(): PolicyRequirementSyntax | null {
        return this.#chain('or', 'Or', () => this.#and());
    }

    #and(): PolicyRequirementSyntax | null {
        return this.#chain('and', 'And', () => this.#groupOrReference());
    }

    #chain(token: string, operator: LogicalOperator, operand: () => PolicyRequirementSyntax | null): PolicyRequirementSyntax | null {
        let left = operand();
        while (left !== null && this.tokens[this.#position] === token) {
            this.#position++;
            const right = operand();
            if (right === null) {
                return null;
            }
            left = { kind: 'LogicalPolicyRequirementSyntax', left, operator, right, location: this.location };
        }
        return left;
    }

    #groupOrReference(): PolicyRequirementSyntax | null {
        if (this.tokens[this.#position] !== '(') {
            return this.#reference();
        }
        this.#position++;
        const requirement = this.#or();
        if (this.tokens[this.#position] === ')') {
            this.#position++;
        } else {
            this.context.error(DiagnosticCodes.UnclosedAuthorizeGroup, 'Expected \')\' in authorize', this.location);
        }
        return requirement;
    }

    #reference(): PolicyRequirementSyntax | null {
        if (this.#position >= this.tokens.length) {
            this.context.error(DiagnosticCodes.AuthorizeWithoutPolicy, 'Expected a policy name', this.location);
            return null;
        }
        const token = this.tokens[this.#position++];
        if (!namePattern.test(token)) {
            this.context.error(DiagnosticCodes.InvalidPolicyReference, `Invalid policy reference '${token}' - policy names are PascalCase identifiers`, this.location);
            return null;
        }
        return { kind: 'PolicyReferenceSyntax', name: token, location: this.location };
    }
}

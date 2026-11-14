// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { TagSyntax } from '../Syntax/Declarations';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { SpecificationNoTagsSyntax } from '../Syntax/Specifications';
import { parseTag } from './DeclarationParsers';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseSpecificationValue } from './SpecificationCaseParser';
import { addFixtureValue } from './SpecificationExampleParser';
import { locationOf, SourceLine } from './SourceLine';

export class TagExpectationParser {
    readonly #context: ParserContext;
    readonly #eventKeyword: string | undefined;
    readonly #tags: TagSyntax[] = [];
    #noTags: SpecificationNoTagsSyntax | undefined;

    constructor(context: ParserContext, eventKeyword?: string) {
        this.#context = context;
        this.#eventKeyword = eventKeyword;
    }

    get syntax(): { tags?: TagSyntax[]; noTags?: SpecificationNoTagsSyntax } {
        const result: { tags?: TagSyntax[]; noTags?: SpecificationNoTagsSyntax } = {};
        if (this.#tags.length > 0) result.tags = this.#tags;
        if (this.#noTags !== undefined) result.noTags = this.#noTags;
        return result;
    }

    tryParseValue(line: SourceLine, mapping: RegExpExecArray | null, values: PropertyMappingSyntax[], other: readonly PropertyMappingSyntax[] = []): boolean {
        if (mapping !== null) {
            addFixtureValue(this.#context, values, {
                kind: 'PropertyMappingSyntax', property: mapping[1],
                source: parseSpecificationValue(mapping[2], locationOf(line), this.#context), location: locationOf(line),
            }, other);
            return true;
        }
        if (!this.#isTagDirective(line)) return false;
        this.#parse(line);
        return true;
    }

    #isTagDirective(line: SourceLine): boolean {
        switch (line.content) {
            case 'no tags': return true;
            case 'tag': return false;
            default: return firstWord(line.content) === 'tag';
        }
    }

    #parse(line: SourceLine): void {
        if (this.#eventKeyword !== 'then') {
            this.#context.error(DiagnosticCodes.InvalidEventTagExpectation, "Tags are asserted only on a 'then' event; a 'given', 'when', redelivery or example occurrence cannot declare 'tag' or 'no tags'.", locationOf(line));
            this.#context.skipBlock(line.indent);
            return;
        }
        if (line.content === 'no tags') this.#parseNoTags(line);
        else this.#parseTag(line);
    }

    #parseNoTags(line: SourceLine): void {
        if (this.#noTags !== undefined || this.#tags.length > 0) {
            this.#context.error(DiagnosticCodes.InvalidEventTagExpectation, "'no tags' cannot be repeated or combined with 'tag' lines.", locationOf(line));
        } else {
            this.#noTags = { kind: 'SpecificationNoTagsSyntax', location: locationOf(line) };
        }
        this.#rejectChildren(line);
    }

    #parseTag(line: SourceLine): void {
        this.#rejectChildren(line);
        if (this.#noTags !== undefined) {
            this.#context.error(DiagnosticCodes.InvalidEventTagExpectation, "'tag' cannot be combined with 'no tags'.", locationOf(line));
            return;
        }
        const tag = parseTag(this.#context, line, false);
        if (tag === undefined) return;
        if (this.#isExpectedLiteral(tag, line)) this.#tags.push(tag);
    }

    #isExpectedLiteral(tag: TagSyntax, line: SourceLine): boolean {
        if (tag.value.kind !== 'LiteralExpressionSyntax') {
            this.#context.error(DiagnosticCodes.InvalidEventTagExpectation, 'An expected tag is a literal - an identifier or a string; a context expression is resolved at append time and cannot be asserted.', locationOf(line));
            return false;
        }
        if (typeof tag.value.value === 'string' && tag.value.value.trim().length > 0) return true;
        this.#context.error(DiagnosticCodes.InvalidEventTagExpectation, 'An expected tag is a nonblank string literal or identifier.', locationOf(line));
        return false;
    }

    #rejectChildren(line: SourceLine): void {
        const child = this.#context.peekChild(line.indent);
        if (child !== undefined) {
            this.#context.error(DiagnosticCodes.InvalidEventTagExpectation, 'A tag expectation cannot have children.', locationOf(child));
            this.#context.skipBlock(line.indent);
        }
    }
}

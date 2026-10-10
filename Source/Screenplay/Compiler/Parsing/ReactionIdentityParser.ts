// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ReactionIdentitySyntax } from '../Syntax/ReactionIdentitySyntax';
import { dotNetWhitespace } from '../Text/patterns';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const identityPattern = new RegExp(`^runs${dotNetWhitespace}+as${dotNetWhitespace}+system(?:${dotNetWhitespace}+role${dotNetWhitespace}+"${stringBodyPattern}"(?:${dotNetWhitespace}+and${dotNetWhitespace}+role${dotNetWhitespace}+"${stringBodyPattern}")*)?$`, 'u');
const rolePattern = new RegExp(`"(${stringBodyPattern})"`, 'gu');

const identityPrefix = new RegExp(`^runs${dotNetWhitespace}+as\\b`, 'u');
export const isReactionIdentityLine = (line: SourceLine): boolean => identityPrefix.test(line.content);

export function parseReactionIdentity(context: ParserContext, line: SourceLine, repeated: boolean): ReactionIdentitySyntax | null {
    if (repeated) {
        context.error(DiagnosticCodes.InvalidReactionIdentity, "A reaction may declare 'runs as' only once.", locationOf(line));
        context.skipBlock(line.indent);
        return null;
    }
    if (!identityPattern.test(line.content) || context.peekChild(line.indent) !== undefined) {
        context.error(DiagnosticCodes.InvalidReactionIdentity, 'Expected one reaction-level line: \'runs as system [role "<Role>" and role "<Role>"]\'.', locationOf(line));
        context.skipBlock(line.indent);
        return null;
    }
    const roles = [...line.content.matchAll(rolePattern)].map(match => unescapeString(match[1]));
    if (roles.some(role => role.length === 0) || new Set(roles).size !== roles.length) {
        context.error(DiagnosticCodes.InvalidReactionIdentity, 'Reaction identity roles must be nonempty and distinct quoted literals.', locationOf(line));
        return null;
    }
    return { kind: 'ReactionIdentitySyntax', syntaxKind: 'system', roles, location: locationOf(line) };
}

export function misplacedReactionIdentity(context: ParserContext, line: SourceLine): void {
    context.error(DiagnosticCodes.InvalidReactionIdentity, "'runs as' belongs directly in a reaction body, not under a trigger or invocation.", locationOf(line));
    context.skipBlock(line.indent);
}

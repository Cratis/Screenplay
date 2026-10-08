// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ConditionSyntax } from '../Syntax/Conditions';
import { InteractionArgumentSyntax, ScreenActionAlternativeSyntax, ScreenActionOtherwiseSyntax, ScreenGuardedActionSyntax, ScreenNavigateSyntax } from '../Syntax/Screens';
import { dotNetWhitespace, pattern } from '../Text/patterns';
import { parseCondition } from './ConditionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

// .NET '.' stops only at LF; U+2028/U+2029 stay inside a CR/LF-delimited source line.
const alternative = pattern(`^when${dotNetWhitespace}+([^\\n]+)${dotNetWhitespace}+execute${dotNetWhitespace}+([A-Za-z_]\\w*(?:\\.\\w+)*)$`);
const otherwise = pattern(`^otherwise${dotNetWhitespace}+execute${dotNetWhitespace}+([A-Za-z_]\\w*(?:\\.\\w+)*)$`);
const argument = pattern(`^with${dotNetWhitespace}+([A-Za-z_]\\w*)${dotNetWhitespace}+from${dotNetWhitespace}+([^\\n]+)$`);
const itemPath = pattern('^item\\.[A-Za-z_]\\w*(?:\\.[A-Za-z_]\\w*)*$');
const bindingWhitespace = new RegExp(`^${dotNetWhitespace}+|${dotNetWhitespace}+$`, 'gu');

type ParseNavigate = (context: ParserContext, text: string, line: SourceLine) => ScreenNavigateSyntax | undefined;

export function parseGuardedAction(context: ParserContext, line: SourceLine, label: string, parseNavigate: ParseNavigate): ScreenGuardedActionSyntax {
    const alternatives: ScreenActionAlternativeSyntax[] = [];
    let fallback: ScreenActionOtherwiseSyntax | null = null;
    let navigate: ScreenNavigateSyntax | null = null;
    let sawOtherwise = false;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const match = alternative.exec(child.content);
        const fallbackMatch = otherwise.exec(child.content);
        if (match !== null) {
            if (sawOtherwise) context.error(DiagnosticCodes.MisplacedActionOtherwise, "A 'when' alternative must precede 'otherwise'", locationOf(child));
            const condition = parseCondition(context, match[1], locationOf(child), true);
            const args = parseArguments(context, child);
            if (condition !== null) {
                validateCondition(context, condition);
                alternatives.push({ kind: 'ScreenActionAlternativeSyntax', condition, command: match[2], arguments: args, location: locationOf(child) });
            }
        } else if (child.content === 'otherwise hidden' || fallbackMatch !== null) {
            if (sawOtherwise) context.error(DiagnosticCodes.MisplacedActionOtherwise, "A guarded action permits only one 'otherwise'", locationOf(child));
            sawOtherwise = true;
            if (fallbackMatch !== null) {
                fallback = { kind: 'ScreenActionOtherwiseSyntax', outcome: 'Execute', command: fallbackMatch[1], arguments: parseArguments(context, child), location: locationOf(child) };
            } else {
                fallback = { kind: 'ScreenActionOtherwiseSyntax', outcome: 'Hidden', command: null, arguments: [], location: locationOf(child) };
                for (let nested = context.peekChild(child.indent); nested !== undefined; nested = context.peekChild(child.indent)) {
                    context.reader.takeSignificant();
                    context.error(DiagnosticCodes.InvalidActionAlternative, "'otherwise hidden' cannot declare command arguments", locationOf(nested));
                    context.skipBlock(nested.indent);
                }
            }
        } else if (firstWord(child.content) === 'navigate') {
            if (navigate !== null) context.error(DiagnosticCodes.InvalidActionAlternative, "A guarded action permits only one 'navigate to'", locationOf(child));
            navigate = parseNavigate(context, child.content, child) ?? null;
        } else {
            const isLabel = firstWord(child.content) === 'label';
            context.error(isLabel ? DiagnosticCodes.UnknownActionDirective : DiagnosticCodes.InvalidActionAlternative, isLabel
                ? "A guarded action's header already supplies its label"
                : `Unexpected '${child.content}' in guarded action - expected 'when <condition> execute <Command>', 'otherwise hidden', 'otherwise execute <Command>' or 'navigate to ...'`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    if (alternatives.length === 0) context.error(DiagnosticCodes.GuardedActionWithoutAlternatives, "A guarded action must declare at least one 'when' alternative", locationOf(line));
    return { kind: 'ScreenGuardedActionSyntax', label, alternatives, otherwise: fallback, navigate, location: locationOf(line) };
}

function parseArguments(context: ParserContext, line: SourceLine): InteractionArgumentSyntax[] {
    const args: InteractionArgumentSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const match = argument.exec(child.content);
        if (match !== null) args.push({ kind: 'InteractionArgumentSyntax', name: match[1], binding: match[2].replace(bindingWhitespace, ''), location: locationOf(child) });
        else {
            context.error(DiagnosticCodes.InvalidInteractionArgument, `Invalid argument '${child.content}' - expected 'with <name> from <binding>'`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return args;
}

function validateCondition(context: ParserContext, condition: ConditionSyntax): void {
    if (condition.kind === 'LogicalConditionSyntax') {
        validateCondition(context, condition.left);
        validateCondition(context, condition.right);
        return;
    }
    const value = condition.right.kind === 'LiteralExpressionSyntax' ? condition.right.value : undefined;
    let supported = itemPath.test(condition.left) && condition.right.kind === 'LiteralExpressionSyntax';
    if (['GreaterThan', 'GreaterThanOrEqual', 'LessThan', 'LessThanOrEqual'].includes(condition.operator)) {
        supported &&= typeof value === 'number' || (typeof value === 'object' && value !== null);
    } else if (['Contains', 'StartsWith'].includes(condition.operator)) supported &&= typeof value === 'string';
    if (!supported) context.error(DiagnosticCodes.UnsupportedActionConditionOperand, "Guarded action conditions compare 'item.<field>' with a literal; ordering requires a number and text operators require a string", condition.location);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { StorylineElementKind, StorylineStepSyntax, StorylineSyntax } from '../Syntax/Storylines';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { parseDescription } from './DescriptionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

// The step keywords in source, each with the element kind it names - the port of the C# StorylineParser.StepKeywords.
export const storylineStepKeywords: Readonly<Record<string, StorylineElementKind>> = {
    module: 'Module',
    feature: 'Feature',
    slice: 'Slice',
    command: 'Command',
    event: 'Event',
    readmodel: 'ReadModel',
    query: 'Query',
    screen: 'Screen',
    reaction: 'Reaction',
};

const headerPattern = pattern(`^storyline\\s+([A-Za-z_]\\w*)(?:\\s+"(${stringBodyPattern})")?$`);
const stepPattern = pattern(`^([a-z]+)\\s+([A-Za-z_]\\w*(?:\\.[A-Za-z_]\\w*)*)(?:\\s+"(${stringBodyPattern})")?$`);

// 'storyline <Name> ["<title>"]' with its description and ordered steps - the port of the C# StorylineParser.Parse.
export function parseStoryline(context: ParserContext, header: SourceLine): StorylineSyntax {
    const match = headerPattern.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidStorylineDeclaration, `Invalid storyline declaration '${header.content}' - expected 'storyline <Name> ["<title>"]'`, locationOf(header));
        context.skipBlock(header.indent);
        return { kind: 'StorylineSyntax', name: '', title: null, steps: [], location: locationOf(header) };
    }

    const name = match[1];
    let description: string | null = null;
    const steps: StorylineStepSyntax[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'description') {
            description = parseDescription(context, line, description, `Storyline '${name}'`);
            continue;
        }
        const step = parseStep(context, line);
        if (step !== null) steps.push(step);
    }

    return {
        kind: 'StorylineSyntax',
        name,
        title: match[2] === undefined ? null : unescapeString(match[2]),
        steps,
        ...(description === null ? {} : { description }),
        location: locationOf(header),
    };
}

function parseStep(context: ParserContext, line: SourceLine): StorylineStepSyntax | null {
    const match = stepPattern.exec(line.content);
    const elementKind = match === null || !Object.hasOwn(storylineStepKeywords, match[1]) ? undefined : storylineStepKeywords[match[1]];
    if (match === null || elementKind === undefined) {
        context.error(
            DiagnosticCodes.InvalidStorylineStep,
            `Invalid storyline step '${line.content}' - expected '<kind> <reference> ["<note>"]' where kind is ${Object.keys(storylineStepKeywords).join(', ')}, or 'description "<text>"'`,
            locationOf(line));
        context.skipBlock(line.indent);
        return null;
    }

    // A step is one line: anything nested under it has no meaning, so it is reported instead of silently dropped.
    const child = context.peekChild(line.indent);
    if (child !== undefined) {
        context.error(DiagnosticCodes.InvalidStorylineStep, `A storyline step is one line; '${child.content}' has no place under it`, locationOf(child));
        context.skipBlock(line.indent);
    }

    return { kind: 'StorylineStepSyntax', elementKind, target: match[2], note: match[3] === undefined ? null : unescapeString(match[3]), location: locationOf(line) };
}

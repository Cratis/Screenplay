// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ExpressionSyntax, PropertyMappingSyntax } from '../Syntax/Expressions';
import { SpecificationGraphElementSyntax, SpecificationGraphSyntax } from '../Syntax/Graphs';
import { pattern } from '../Text/patterns';
import { parseMappingSource } from './ExpressionParser';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const stepPrefix = (keyword: string): RegExp => pattern(`^${keyword}\\s+graph(?:\\s|$)`);
export const givenGraphPrefix = stepPrefix('given');
export const thenGraphPrefix = stepPrefix('then');
const stepPattern = pattern('^(?:given|then)\\s+graph\\s+([A-Za-z_]\\w*)$');

interface Word {
    readonly text: string;
    readonly offset: number;
}

export interface GraphStepHelpers {
    readonly parseValues: (context: ParserContext, parent: SourceLine) => PropertyMappingSyntax[];
    readonly skipBody: (context: ParserContext, indent: number) => void;
}

// Reads 'given graph <Graph>' or 'then graph <Graph>' with its 'has node', 'has edge', 'no node' and 'no edge' lines.
export function parseGraphStep(context: ParserContext, line: SourceLine, keyword: 'given' | 'then', helpers: GraphStepHelpers): SpecificationGraphSyntax | undefined {
    const header = stepPattern.exec(line.content);
    if (header === null || !line.content.startsWith(keyword)) {
        context.error(DiagnosticCodes.InvalidGraphSpecification, `Invalid '${keyword} graph' declaration '${line.content}' - expected '${keyword} graph <Graph>'`, locationOf(line));
        helpers.skipBody(context, line.indent);
        return undefined;
    }
    const elements: SpecificationGraphElementSyntax[] = [];
    let stated = false;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        stated = true;
        const element = parseElement(context, child, keyword, helpers);
        if (element !== undefined) elements.push(element);
    }
    if (!stated) {
        context.error(DiagnosticCodes.InvalidGraphSpecification, `'${keyword} graph ${header[1]}' must state at least one node or edge`, locationOf(line));
    }
    return { kind: 'SpecificationGraphSyntax', graph: header[1], elements, location: locationOf(line) };
}

function parseElement(context: ParserContext, line: SourceLine, keyword: string, helpers: GraphStepHelpers): SpecificationGraphElementSyntax | undefined {
    const words = readWords(line.content);
    const invalid = (expected: string): undefined => {
        context.error(DiagnosticCodes.InvalidGraphSpecification, `Invalid graph step '${line.content}' - ${expected}`, locationOf(line));
        helpers.skipBody(context, line.indent);
        return undefined;
    };
    if (words.length < 4 || (words[0].text !== 'has' && words[0].text !== 'no') || (words[1].text !== 'node' && words[1].text !== 'edge')) {
        return invalid("expected 'has node <Label> <key>', 'has edge <TYPE> from <source> to <target>', 'no node ...' or 'no edge ...'");
    }
    const absent = words[0].text === 'no';
    if (absent && keyword === 'given') {
        return invalid("'given graph' seeds what exists; use 'no node' and 'no edge' in 'then graph'");
    }
    const value = (word: Word): ExpressionSyntax => parseMappingSource(word.text, { ...locationOf(line), column: locationOf(line).column + word.offset }, context.valueContext);
    let element: SpecificationGraphElementSyntax | undefined;
    if (words[1].text === 'node') {
        if (words.length === 4) {
            element = { kind: 'SpecificationGraphNodeSyntax', label: words[2].text, key: value(words[3]), absent, properties: [], location: locationOf(line) };
        }
    } else if ((words.length === 7 || words.length === 10) && words[3].text === 'from' && words[5].text === 'to' && (words.length === 7 || (words[7].text === 'identified' && words[8].text === 'by'))) {
        element = {
            kind: 'SpecificationGraphEdgeSyntax', type: words[2].text, source: value(words[4]), target: value(words[6]),
            ...(words.length === 10 ? { identity: value(words[9]) } : {}), absent, properties: [], location: locationOf(line),
        };
    }
    if (element === undefined) {
        return invalid("expected 'has node <Label> <key>' or 'has edge <TYPE> from <source> to <target> [identified by <value>]'");
    }
    if (absent) {
        const child = context.peekChild(line.indent);
        if (child !== undefined) {
            context.error(DiagnosticCodes.InvalidGraphSpecification, 'An absence step has no property assertions', locationOf(child));
            helpers.skipBody(context, line.indent);
        }
        return element;
    }
    return { ...element, properties: helpers.parseValues(context, line) };
}

// Splits a step into whitespace separated words, keeping a quoted string whole.
function readWords(text: string): Word[] {
    const words: Word[] = [];
    let index = 0;
    while (index < text.length) {
        if (/\s/.test(text[index])) {
            index++;
            continue;
        }
        const start = index;
        if (text[index] === '"') {
            index++;
            while (index < text.length && text[index] !== '"') index += text[index] === '\\' ? 2 : 1;
            index = Math.min(index + 1, text.length);
        } else {
            while (index < text.length && !/\s/.test(text[index])) index++;
        }
        words.push({ text: text.substring(start, index), offset: start });
    }
    return words;
}

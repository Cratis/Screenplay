// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { pattern } from '../Text/patterns';
import { firstWord, unescapeIdentifier } from './LineText';
import { validateInteractionBodies } from './InteractionAlternatives';
import { ParserContext } from './ParserContext';
import { parseDescription } from './DescriptionParser';
import { locationOf, SourceLine } from './SourceLine';

// Typed authoring facts from bodies the TS syntax tree does not yet model. Never serialized as ESM.
export interface InputUse {
    readonly command: string;
    readonly property: string;
    readonly scope: readonly string[];
    readonly location: SourceLocation;
    readonly isParameter?: boolean;
}

interface InputOwner { command: string; kind: 'form' | 'execute' | 'invokes' | 'other'; indent: number }
const form = pattern('^form\\s+[A-Za-z_]\\w*\\s+for\\s+([A-Za-z_]\\w*(?:\\.\\w+)*)$');
const execution = pattern('^(execute|invokes)\\s+([A-Za-z_]\\w*(?:\\.\\w+)*)$');
const field = pattern('^field\\s+([\\w.]+)(?:\\s|$)');
const argument = pattern('^with\\s+([A-Za-z_]\\w*)\\s+from\\s+.+$');
const mapping = pattern('^(@?[\\w.]+)\\s*=(?!=|>)\\s*.+$');
const parameter = pattern('^parameter\\s+([A-Za-z_]\\w*)(?:\\s+([\\w.]+(?:\\[\\])?))?$');
const actionBoundaries = new Set(['refresh', 'navigate', 'open', 'close', 'set', 'notify', 'confirm', 'raise', 'on', 'when', 'otherwise']);

// A single forward walk, tracking owners by indentation and skipping fences as opaque text.
export function collectInputUses(context: ParserContext, header: SourceLine): void {
    const owners: InputOwner[] = [];
    const parameters = new Set<string>();
    const inputs: InputUse[] = [];
    const isBehavior = firstWord(header.content) === 'behavior';
    let formDescription: string | null = null;
    const recognize = (line: SourceLine): void => {
        while (owners.length > 0 && owners[owners.length - 1].indent >= line.indent) owners.pop();
        const declared = isBehavior && owners.length === 0 ? parameter.exec(line.content) : null;
        if (declared !== null) parameters.add(declared[1]);
        const formMatch = form.exec(line.content);
        const execute = execution.exec(line.content);
        if (formMatch !== null) owners.push({ command: formMatch[1], kind: 'form', indent: line.indent });
        else if (execute !== null) owners.push({ command: execute[2], kind: execute[1] as 'execute' | 'invokes', indent: line.indent });
        else if (actionBoundaries.has(firstWord(line.content))) owners.push({ command: '', kind: 'other', indent: line.indent });
        else {
            const owner = owners.at(-1);
            if (owner === undefined || owner.kind === 'other') return;
            const supplied = (owner.kind === 'form' ? field : owner.kind === 'execute' ? argument : mapping).exec(line.content);
            if (supplied !== null) inputs.push({ command: owner.command, property: unescapeIdentifier(supplied[1]), scope: context.scope, location: locationOf(line) });
        }
    };
    recognize(header);
    const lines = [header];
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (form.test(header.content) && firstWord(child.content) === 'description' && owners.at(-1)?.kind === 'form') {
            formDescription = parseDescription(context, child, formDescription, `Form '${header.content.split(/\s+/)[1]}'`);
        } else if (child.content.startsWith('```')) context.skipFencedBody();
        else {
            lines.push(child);
            recognize(child);
        }
    }
    validateInteractionBodies(context, lines);
    // Parameters may be declared after their bindings; commit facts once their scope is complete.
    for (const input of inputs) context.inputUses.push(parameters.has(input.command) ? { ...input, isParameter: true } : input);
}

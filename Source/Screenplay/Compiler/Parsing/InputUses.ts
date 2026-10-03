// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { pattern } from '../Text/patterns';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

// Typed authoring facts from bodies the TS syntax tree does not yet model. Never serialized as ESM.
export interface InputUse {
    readonly command: string;
    readonly property: string;
    readonly scope: readonly string[];
    readonly location: SourceLocation;
}

interface InputOwner { command: string; kind: 'form' | 'execute' | 'invokes'; indent: number }
const form = pattern('^form\\s+[A-Za-z_]\\w*\\s+for\\s+([A-Za-z_]\\w*(?:\\.\\w+)*)$');
const execution = pattern('^(execute|invokes)\\s+([A-Za-z_]\\w*(?:\\.\\w+)*)$');
const field = pattern('^field\\s+([\\w.]+)(?:\\s|$)');
const argument = pattern('^with\\s+([A-Za-z_]\\w*)\\s+from\\s+.+$');
const mapping = pattern('^([\\w.]+)\\s*=(?!=|>)\\s*.+$');

// A single forward walk, tracking owners by indentation and skipping fences as opaque text.
export function collectInputUses(context: ParserContext, header: SourceLine): void {
    const owners: InputOwner[] = [];
    const recognize = (line: SourceLine): void => {
        while (owners.length > 0 && owners[owners.length - 1].indent >= line.indent) owners.pop();
        const formMatch = form.exec(line.content);
        const execute = execution.exec(line.content);
        if (formMatch !== null) owners.push({ command: formMatch[1], kind: 'form', indent: line.indent });
        else if (execute !== null) owners.push({ command: execute[2], kind: execute[1] as 'execute' | 'invokes', indent: line.indent });
        else {
            const owner = owners.at(-1);
            if (owner === undefined) return;
            const supplied = (owner.kind === 'form' ? field : owner.kind === 'execute' ? argument : mapping).exec(line.content);
            if (supplied !== null) context.inputUses.push({ command: owner.command, property: supplied[1], scope: [...context.scope], location: locationOf(line) });
        }
    };
    recognize(header);
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (child.content.startsWith('```')) context.skipFencedBody();
        else recognize(child);
    }
}

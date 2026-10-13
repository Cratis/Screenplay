// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { nativePattern as pattern } from '../Text/patterns';
import { ContinuationValueUse } from './ContinuationValueUse';
import { locationOf, SourceLine } from './SourceLine';

const formHeader = pattern(/^form\s+[A-Za-z_]\w*\s+for\s+([\w.]+)$/.source);
const executeHeader = pattern(/^execute\s+([\w.]+)$/.source);
const behaviorHeader = pattern(/^behavior\s+[A-Za-z_]\w*/.source);
const localDeclaration = pattern(/^(?:field|parameter)\s+([A-Za-z_]\w*)(?:\s|$)/.source);
const argument = pattern(/^with\s+\w+\s+from\s+(.+)$/.source);
const submitNavigation = pattern(/^on\s+submit\s+navigate\s+to\s+[\w.]+\s+by\s+(.+)$/.source);
const stateValue = pattern(/^set\s+[\w.$]+\s+to\s+(.+)$/.source);
const continuation = /^on\s+(success|failure|submit)$/;

// Lines have already had comments, fences and descriptions removed by the body reader.
// Each owner remains alive only for its greater-indented subtree; siblings restore their ancestry.
export function collectContinuationValues(lines: readonly SourceLine[], scope: readonly string[]): ContinuationValueUse[] {
    const owners: SourceLine[] = [];
    const parents = new Map<SourceLine, readonly SourceLine[]>();
    const locals = new Map<SourceLine, string[]>();
    for (const line of lines) {
        while (owners.length > 0 && owners.at(-1)!.indent >= line.indent) owners.pop();
        parents.set(line, [...owners]);
        const local = localDeclaration.exec(line.content);
        if (local) {
            const owner = [...owners].reverse().find(parent => formHeader.test(parent.content) || behaviorHeader.test(parent.content));
            if (owner) locals.set(owner, [...locals.get(owner) ?? [], local[1]]);
        }
        owners.push(line);
    }
    return lines.flatMap(line => {
        const text = continuationOperand(line.content) ?? '';
        const ancestry = parents.get(line)!;
        const success: string[] = [];
        const failure: string[] = [];
        for (let index = 0; index < ancestry.length; index++) {
            const branch = ancestry[index];
            const parent = ancestry[index - 1];
            const outcome = continuation.exec(branch.content)?.[1];
            const executed = parent ? executeHeader.exec(parent.content) : null;
            if (executed && outcome === 'success') success.push(executed[1]);
            if (executed && outcome === 'failure') failure.push(executed[1]);
            const form = parent ? formHeader.exec(parent.content) : null;
            if (form && outcome === 'submit') success.push(form[1]);
        }
        // One-line form sugar is its own success continuation, not a screen action's navigation.
        if (/^on\s+submit\s+navigate\s+to\s+/.test(line.content)) {
            const form = ancestry.at(-1) ? formHeader.exec(ancestry.at(-1)!.content) : null;
            if (form) success.push(form[1]);
        }
        const parameters = ancestry.filter(parent => behaviorHeader.test(parent.content)).flatMap(parent => locals.get(parent) ?? []);
        return [{ text, location: locationOf(line), scope, success, failure, parameters,
            locals: ancestry.flatMap(parent => locals.get(parent) ?? []) }];
    });
}

export function continuationOperand(content: string): string | null {
    return argument.exec(content)?.[1]
        ?? submitNavigation.exec(content)?.[1]
        ?? stateValue.exec(content)?.[1]
        ?? /^notify\s+(?:info|warning|error)\s+(.+)$/.exec(content)?.[1]
        ?? /^confirm\s+(.+)$/.exec(content)?.[1]
        ?? null;
}

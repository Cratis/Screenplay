// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { should } from 'chai';
import { parse } from '../../../ScreenplayCompiler';
import { toSyntaxJson } from '../../../Syntax/SyntaxJson';
import { commandCases } from './given/cases';

should();
const prefix = 'concept AccountId : Uuid\n\neventsource Account\n  identifier AccountId\n  stream Transactions\n  stream Other\n\nmodule M\n  feature F\n    slice StateChange S\n      command C\n';

function findCommand(node: unknown, name: string): unknown {
    if (Array.isArray(node)) {
        for (const item of node) {
            const found = findCommand(item, name);
            if (found !== undefined) return found;
        }
    } else if (typeof node === 'object' && node !== null) {
        const record = node as Record<string, unknown>;
        if (record.kind === 'CommandSyntax' && record.name === name) return node;
        for (const value of Object.values(record)) {
            const found = findCommand(value, name);
            if (found !== undefined) return found;
        }
    }
    return undefined;
}

function parseWithMarker(source: string) {
    return parse(`${prefix}${source}\n      command Marker\n        marker String\n`, 'fragment.play');
}

function markerProperties(source: string): string {
    const marker = findCommand(toSyntaxJson(parseWithMarker(source).value), 'Marker') as { properties: { name: string; type: { name: string } }[] } | undefined;
    return marker === undefined ? 'no marker command' : marker.properties.map(p => `${p.name} ${p.type.name}`).join(',');
}

export function snapshot(source: string): string {
    const result = parseWithMarker(source);
    const command = findCommand(toSyntaxJson(result.value), 'C');
    return [...result.diagnostics.map(d => `${d.severity} ${d.code} ${d.location.line}:${d.location.column} ${d.message}`), command === undefined ? 'no command' : JSON.stringify(command)].join('\n');
}

describe('when disambiguating properties and responses', () => {
    it.each(commandCases.map(c => [c.name, c] as const))('should keep the diagnostics, recovery and syntax for %s', (_, c) => {
        snapshot(c.source).should.equal(c.expected);
    });

    it.each(commandCases.map(c => [c.name, c] as const))('should recover into the sibling marker command for %s', (_, c) => {
        markerProperties(c.source).should.equal('marker String');
    });
});

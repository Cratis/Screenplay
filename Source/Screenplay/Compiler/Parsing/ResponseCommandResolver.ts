// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandSyntax } from '../Syntax/Commands';

// Shared authoring resolution for input/response validation and continuation editor scopes.
export function responseCommandResolver(entries: readonly { command: CommandSyntax; scope: readonly string[] }[], imports: readonly { qualifiedName: string }[]): (name: string, from: readonly string[]) => CommandSyntax | null {
    const resolved = new Map<string, CommandSyntax | null>();
    return (name, from) => {
        const key = `${from.join('.')}|${name}`;
        if (resolved.has(key)) return resolved.get(key)!;
        const imported = imports.filter(entry => entry.qualifiedName.split('.').at(-1) === name);
        const local = entries.filter(entry => entry.command.name === name.split('.').at(-1));
        const reference = local.length === 0 && imported.length === 1 ? imported[0].qualifiedName : name;
        const segments = reference.split('.');
        let candidates = entries.filter(entry => entry.command.name === segments.at(-1));
        if (segments.length > 1) {
            const qualifiers = segments.slice(0, -1);
            candidates = candidates.filter(entry => qualifiers.length <= entry.scope.length && qualifiers.every((value, index) => entry.scope[entry.scope.length - qualifiers.length + index] === value));
        } else {
            for (let depth = from.length; depth >= 0; depth--) {
                const visible = candidates.filter(entry => depth <= entry.scope.length && from.slice(0, depth).every((value, index) => entry.scope[index] === value));
                if (visible.length > 0) { candidates = visible; break; }
            }
        }
        const command = candidates.length === 1 ? candidates[0].command : null;
        resolved.set(key, command);
        return command;
    };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Matches the path or glob pattern of a file import against portable '/' separated paths - the port of the
// C# PlayGlob, so a folder resolves the same way in an editor and in a build. '**' as a whole segment matches
// any number of folders, including none; '*' matches any run of characters within one segment and '?' one
// character. A pattern is relative to the folder of the file that imports, may climb with '..', and only ever
// matches .play files.

// Whether a pattern holds a wildcard, as opposed to naming one file.
export const hasWildcard = (pattern: string): boolean => pattern.includes('*') || pattern.includes('?');

// Normalizes a portable path - separators to '/', '.' segments removed, '..' applied. Leading '..' segments
// that climb above the root are kept.
export function normalizePlayPath(path: string): string {
    const segments: string[] = [];
    for (const segment of path.replaceAll('\\', '/').split('/').filter(each => each.length > 0)) {
        if (segment === '.') continue;
        if (segment === '..' && segments.length > 0 && segments[segments.length - 1] !== '..') {
            segments.pop();
            continue;
        }
        segments.push(segment);
    }
    return segments.join('/');
}

// Resolves a pattern written in a file against the folder that file is in, relative to the same root as the
// importing path.
export function resolvePlayPattern(importingPath: string, pattern: string): string {
    const slash = importingPath.lastIndexOf('/');
    const folder = slash >= 0 ? importingPath.substring(0, slash) : '';
    const combined = pattern.startsWith('/') ? pattern.replace(/^\/+/, '') : (folder.length === 0 ? pattern : `${folder}/${pattern}`);
    return normalizePlayPath(combined);
}

// The folder a resolved pattern can only match beneath - everything before its first wildcard segment.
export function staticFolderOf(resolvedPattern: string): string {
    const segments = resolvedPattern.split('/').slice(0, -1);
    const wildcard = segments.findIndex(hasWildcard);
    return (wildcard < 0 ? segments : segments.slice(0, wildcard)).join('/');
}

// Whether a normalized portable path is a .play file a resolved pattern names.
export function matchesPlayPattern(resolvedPattern: string, path: string): boolean {
    if (path.slice(-5).toLowerCase() !== '.play') return false;
    const states = patternStates(resolvedPattern);
    let reachable = Array<boolean>(states.length + 1).fill(false);
    let next = Array<boolean>(states.length + 1).fill(false);
    reachable[0] = true;
    // Epsilon transitions only advance, so one forward pass closes each rolling state set.
    for (const character of path) {
        next.fill(false);
        for (let state = 0; state < states.length; state++) {
            if (!reachable[state]) continue;
            const { skip, consume } = states[state]!;
            if (skip > 0) reachable[state + skip] = true;
            const target = consume(character);
            if (target >= 0) next[target] = true;
        }
        const previous = reachable;
        reachable = next;
        next = previous;
    }
    for (let state = 0; state < states.length; state++) {
        if (reachable[state] && states[state]!.skip > 0) reachable[state + states[state]!.skip] = true;
    }
    return reachable[states.length]!;
}

function patternStates(pattern: string): { skip: number; consume: (character: string) => number }[] {
    const states: { skip: number; consume: (character: string) => number }[] = [];
    const segments = pattern.split('/');
    for (let segment = 0; segment < segments.length; segment++) {
        const index = states.length;
        const last = segment === segments.length - 1;
        if (segments[segment] === '**') {
            if (last) {
                // Like the former trailing `.*`, this loop does not cross line separators.
                states.push({ skip: 1, consume: character => '\n\r\u2028\u2029'.includes(character) ? -1 : index });
            } else {
                // (?:[^/]+/)*: skipping consumes no folder; entering requires a nonempty one.
                states.push({ skip: 2, consume: character => character === '/' ? -1 : index + 1 });
                states.push({ skip: 0, consume: character => character === '/' ? index : index + 1 });
            }
            continue;
        }
        for (const token of segments[segment]!) {
            const position = states.length;
            states.push({
                skip: token === '*' ? 1 : 0,
                consume: character => {
                    if (character === '/') return -1;
                    if (token === '*') return position;
                    return token === '?' || character === token ? position + 1 : -1;
                },
            });
        }
        // An ordinary segment always consumes its separator, including before a trailing **.
        if (!last) {
            const position = states.length;
            states.push({ skip: 0, consume: character => character === '/' ? position + 1 : -1 });
        }
    }
    return states;
}

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
    if (!path.toLowerCase().endsWith('.play')) return false;
    const parts = path.split('/').map(part => [...part]);
    let reachable = Array<boolean>(parts.length + 1).fill(false);
    reachable[0] = true;
    const segments = resolvedPattern.split('/');
    for (let index = 0; index < segments.length; index++) {
        const segment = segments[index]!;
        if (segment === '**' && index === segments.length - 1) return matchesTrailingGlobstar(parts, reachable);
        reachable = advanceSegment(segment, parts, reachable);
    }
    return reachable[parts.length]!;
}

// The old trailing '.*' excludes line separators, unlike a folder globstar.
function matchesTrailingGlobstar(parts: readonly (readonly string[])[], reachable: readonly boolean[]): boolean {
    let suffixMatches = true;
    for (let part = parts.length; part >= 0; part--) {
        if (part < parts.length) suffixMatches &&= !parts[part]!.some(character => '\n\r\u2028\u2029'.includes(character));
        if (reachable[part] && suffixMatches) return true;
    }
    return false;
}

function advanceSegment(segment: string, parts: readonly (readonly string[])[], reachable: readonly boolean[]): boolean[] {
    const next = Array<boolean>(parts.length + 1).fill(false);
    for (let part = 0; part < parts.length; part++) {
        if (segment === '**') {
            next[part] = reachable[part]! || (part > 0 && next[part - 1]! && parts[part - 1]!.length > 0);
        } else {
            next[part + 1] = reachable[part]! && matchesSegment(segment, parts[part]!);
        }
    }
    return next;
}

// Rolling dynamic-programming rows keep matching polynomial without recursive wildcard retries.
function matchesSegment(pattern: string, characters: readonly string[]): boolean {
    let reachable = Array<boolean>(characters.length + 1).fill(false);
    reachable[0] = true;
    for (const token of pattern) {
        const next = Array<boolean>(characters.length + 1).fill(false);
        next[0] = token === '*' && reachable[0]!;
        for (let index = 1; index <= characters.length; index++) {
            next[index] = token === '*'
                ? reachable[index]! || next[index - 1]!
                : reachable[index - 1]! && (token === '?' || token === characters[index - 1]);
        }
        reachable = next;
    }
    return reachable[characters.length]!;
}

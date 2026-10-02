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
    return path.toLowerCase().endsWith('.play') && toRegularExpression(resolvedPattern).test(path);
}

function toRegularExpression(pattern: string): RegExp {
    let expression = '^';
    const segments = pattern.split('/');
    segments.forEach((segment, index) => {
        const last = index === segments.length - 1;
        if (segment === '**') {
            expression += last ? '.*' : '(?:[^/]+/)*';
            return;
        }
        for (const character of segment) {
            expression += character === '*' ? '[^/]*' : character === '?' ? '[^/]' : character.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        }
        if (!last) expression += '/';
    });
    return new RegExp(`${expression}$`, 'u');
}

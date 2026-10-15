// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** RegExpIdentifierName uses Unicode identifiers, even in flagless JavaScript. */
export function isJavaScriptGroupName(name: string): boolean {
    const decoded = name.replace(/\\u(?:\{([0-9a-fA-F]+)\}|([0-9a-fA-F]{4}))/g, (_, braced: string | undefined, fixed: string | undefined) => {
        const value = parseInt(braced ?? fixed!, 16);
        return value <= 0x10ffff ? String.fromCodePoint(value) : '\\';
    });
    return /^[$_\p{ID_Start}][$\u200c\u200d\p{ID_Continue}]*$/u.test(decoded);
}

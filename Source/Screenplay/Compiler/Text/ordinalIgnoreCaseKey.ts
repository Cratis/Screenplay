// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// .NET OrdinalIgnoreCase uses simple uppercase, not lowercase or full (expanding) uppercase.
// It keeps dotless i and long s distinct from ASCII I and S. Greek letters with a subscript
// iota have a single-character uppercase that JavaScript's full casing expands instead.
export function ordinalIgnoreCaseKey(value: string): string {
    return Array.from(value, character => {
        if (character === '\u0131' || character === '\u017f') return character;
        const code = character.codePointAt(0)!;
        if ((code >= 0x1f80 && code <= 0x1f87) || (code >= 0x1f90 && code <= 0x1f97) || (code >= 0x1fa0 && code <= 0x1fa7)) return String.fromCodePoint(code + 8);
        if (code === 0x1fb3) return '\u1fbc';
        if (code === 0x1fc3) return '\u1fcc';
        if (code === 0x1ff3) return '\u1ffc';
        const upper = character.toUpperCase();
        return Array.from(upper).length === 1 ? upper : character;
    }).join('');
}

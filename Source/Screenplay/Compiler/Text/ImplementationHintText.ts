// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Unicode White_Space, matching C# Text/ImplementationHintText.cs. ECMAScript trim
// differs at NEL and BOM. Only test blankness; never normalize authored hint text.
const whiteSpace = new Set([0x0020, 0x0085, 0x00a0, 0x1680, 0x2028, 0x2029, 0x202f, 0x205f, 0x3000]);

export function isBlankImplementationHint(text: string | null): boolean {
    if (text === null) return true;
    for (const character of text) {
        const code = character.charCodeAt(0);
        if (!((code >= 0x0009 && code <= 0x000d) || (code >= 0x2000 && code <= 0x200a) || whiteSpace.has(code))) return false;
    }
    return true;
}

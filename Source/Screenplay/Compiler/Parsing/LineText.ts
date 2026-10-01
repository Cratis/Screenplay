// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The first space-separated word of a line's content - the keyword every construct is recognized by.
export const firstWord = (content: string): string => {
    const space = content.indexOf(' ');
    return space === -1 ? content : content.substring(0, space);
};

// Removes the '@' that lets an identifier read as a keyword ('@validate' names a property called validate).
export const unescapeIdentifier = (identifier: string): string => identifier.replaceAll('@', '');

// Splits text on a separator that is not inside a string or a template literal.
export function splitTopLevel(text: string, separator: string): string[] {
    const parts: string[] = [];
    let start = 0;
    let inString = false;
    let inTemplate = false;
    for (let index = 0; index < text.length; index++) {
        const current = text[index];
        if (current === '\\' && inString && index + 1 < text.length) {
            index++;
        } else if (current === '"' && !inTemplate) {
            inString = !inString;
        } else if (current === '`' && !inString) {
            inTemplate = !inTemplate;
        } else if (current === separator && !inString && !inTemplate) {
            parts.push(text.substring(start, index));
            start = index + 1;
        }
    }
    parts.push(text.substring(start));
    return parts;
}

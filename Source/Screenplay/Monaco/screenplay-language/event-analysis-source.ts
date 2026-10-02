// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Match SourceLineSplitter: comments begin outside strings/templates; fenced bodies remain opaque.
// Keep this local to event analysis so it does not change the other editor scanners.
export function eventAnalysisSource(lines: string[]): string[] {
    let fenced = false;
    return lines.map(line => {
        if (fenced) {
            if (line.trim() === '```') fenced = false;
            return line;
        }
        let inString = false;
        let inTemplate = false;
        let text = line;
        for (let index = 0; index < line.length; index++) {
            const current = line[index];
            if (current === '\\' && inString && index + 1 < line.length) index++;
            else if (current === '"' && !inTemplate) inString = !inString;
            else if (current === '`' && !inString) inTemplate = !inTemplate;
            else if (!inString && !inTemplate && current === '/' && line[index + 1] === '/') {
                text = line.substring(0, index);
                break;
            }
        }
        if (/^\s*```(?:[a-z]+)?\s*$/.test(text)) fenced = true;
        return text.trimEnd();
    });
}

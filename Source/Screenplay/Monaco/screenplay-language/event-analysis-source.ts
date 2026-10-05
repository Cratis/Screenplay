// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withoutComment } from './document-context';

// Match SourceLineSplitter: comments begin outside strings/templates; fenced bodies remain opaque.
export function eventAnalysisSource(lines: string[]): string[] {
    let fenced = false;
    return lines.map(line => {
        if (fenced) {
            if (line.trim() === '```') fenced = false;
            return line;
        }
        const text = withoutComment(line);
        if (/^\s*```(?:[a-z]+)?\s*$/.test(text)) fenced = true;
        return text.trimEnd();
    });
}

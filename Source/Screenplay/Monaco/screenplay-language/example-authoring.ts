// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fenceMap, withoutComment } from './document-context';
import { caseCompletions, caseHover } from './case-authoring';
import { responseAnalysis } from './response-analysis';
import { DocumentSymbols, symbolsForBuffer } from './symbols';

function analyze(lines: string[], application?: DocumentSymbols) {
    const symbols = symbolsForBuffer(lines, application);
    return responseAnalysis(lines, symbols.authoringDocuments ?? symbols.authoringSources?.filter(source => source !== lines.join('\n')) ?? [],
        symbols.authoringPlacement, symbols.authoringPath, symbols.authoringPlacementResolved);
}

export function exampleCompletions(lines: string[], line: number, before: string, application?: DocumentSymbols) {
    if (fenceMap(lines)[line] || withoutComment(before).length < before.length) return null;
    const analysis = analyze(lines, application);
    return caseCompletions(lines, line, before) ?? analysis.personas.completions(line, before) ?? analysis.examples.completions(line, before);
}

export function exampleHover(lines: string[], line: number, start: number, end: number, application?: DocumentSymbols) {
    const analysis = analyze(lines, application);
    return caseHover(lines, line, start, end) ?? analysis.personas.hover(line, start, end) ?? analysis.examples.hover(line, start, end);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ScreenVisualKind, ScreenVisualSyntax } from '../Syntax/Screens';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const kinds: Readonly<Record<string, ScreenVisualKind>> = { sketch: 'Sketch', mockup: 'Mockup', image: 'Image' };
const imageExtensions = ['.png', '.jpg', '.jpeg', '.gif', '.svg', '.webp'];
const mockupExtensions = ['.html', '.htm'];

const extensionOf = (path: string): string => {
    const name = path.substring(Math.max(path.lastIndexOf('/'), path.lastIndexOf('\\')) + 1);
    const dot = name.lastIndexOf('.');
    return dot < 0 ? '' : name.substring(dot).toLowerCase();
};

// 'sketch <path>', 'mockup <path>' or 'image <path>' - a visual a screen carries beside the model. Returns false
// when the line is something else (PLAY0950, PLAY0951).
export function parseScreenVisual(context: ParserContext, line: SourceLine, visuals: ScreenVisualSyntax[]): boolean {
    const keyword = firstWord(line.content);
    const kind = Object.hasOwn(kinds, keyword) ? kinds[keyword] : undefined;
    if (kind === undefined) return false;
    const path = line.content.substring(keyword.length).trim();
    if (path.length === 0) {
        context.error(DiagnosticCodes.InvalidScreenVisual, `'${keyword}' needs the path of the file it shows - write '${keyword} <path>', relative to the model root`, locationOf(line));
        return true;
    }
    if (/^(?:[/\\]|[A-Za-z]:[/\\]|~[/\\])/.test(path)) {
        context.warning(DiagnosticCodes.AbsoluteFileReference, `'${path}' is an absolute path - a file reference is relative to the repository root, so it means the same thing on every machine`, locationOf(line));
    }
    const extension = extensionOf(path);
    if (kind === 'Image' && !imageExtensions.includes(extension)) {
        context.warning(DiagnosticCodes.UnexpectedScreenVisualFile, `Image '${path}' is not a PNG, JPG, GIF, SVG or WebP file`, locationOf(line));
    } else if (kind === 'Mockup' && !mockupExtensions.includes(extension)) {
        context.warning(DiagnosticCodes.UnexpectedScreenVisualFile, `Mockup '${path}' is not an HTML page - a mockup page ends in .html or .htm`, locationOf(line));
    }
    visuals.push({ kind: 'ScreenVisualSyntax', syntaxKind: kind, file: { kind: 'FileReferenceSyntax', path, location: locationOf(line) }, location: locationOf(line) });
    return true;
}

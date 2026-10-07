// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { enclosingChain, fenceMap, indentOf, withoutComment } from './document-context';
import { ReactionReference } from './ReactionReference';
import { directBody, DocumentSymbols, NamedSymbol, propertyTypeReference, scanDocument } from './symbols';
import { typeReferenceText } from './TypeReferenceSymbol';

// Resolve only a reaction's named occurrence, never a specification step, comment or native fence.
// Both editors use the same event/import/declared-trigger precedence, including unsaved buffers.
export function reactionReferenceAt(lines: string[], lineIndex: number, startColumn: number, endColumn: number, application?: DocumentSymbols): ReactionReference | null {
    const fences = fenceMap(lines);
    if (fences[lineIndex]) return null;
    const line = withoutComment(lines[lineIndex] ?? '');
    const match = line.match(/^\s*when\s+([A-Za-z_][\p{L}\p{Mn}\p{Nd}\p{Pc}]*)\s*$/u);
    if (!match || enclosingChain(lines, fences, lineIndex, indentOf(line))[0] !== 'reaction') return null;
    const name = match[1];
    const column = line.indexOf(name, line.indexOf('when') + 4) + 1;
    if (startColumn !== column || endColumn !== column + name.length) return null;
    const current = application?.authoringPath ?? 'current.play';
    const documents = [...(application?.authoringDocuments ?? []).filter(document => document.path !== current), { path: current, source: lines.join('\n') }];
    const scanned = documents.map(document => ({ ...document, lines: document.source.split(/\r?\n/), symbols: scanDocument(document.source.split(/\r?\n/)) }));
    const events = scanned.flatMap(document => {
        const named = document.symbols.events.filter(event => event.name === name);
        const generation = Math.max(...named.map(event => event.generation ?? 1));
        return named.filter(event => (event.generation ?? 1) === generation).map(event => ({ document, declaration: event }));
    });
    const selected = (matches: { document: typeof scanned[number]; declaration: NamedSymbol }[]): ReactionReference => {
        if (matches.length !== 1) return { name, content: null, target: null };
        const { document, declaration } = matches[0];
        const body = directBody(document.lines, fenceMap(document.lines), declaration.line, indentOf(document.lines[declaration.line]));
        const content = [document.lines[declaration.line].trim(), ...body.map(line => document.lines[line].trim())].join('\n');
        const prefix = document.lines[declaration.line].match(/\b(?:event|trigger)\s+/)!;
        return { name, content: `\`\`\`screenplay\n${content}\n\`\`\``, target: { name, location: { path: document.path, line: declaration.line + 1, column: prefix.index! + prefix[0].length + 1 }, source: document.source } };
    };
    if (events.length > 0) return selected(events);
    // A host may supply symbols without physical documents. Hover their event shape, but
    // never invent a definition location or fall through to a shadowed local trigger.
    if (application?.authoringDocuments === undefined) {
        const known = application?.events.filter(event => event.name === name) ?? [];
        if (known.length > 0) {
            const generation = Math.max(...known.map(event => event.generation ?? 1));
            const current = known.filter(event => (event.generation ?? 1) === generation);
            const content = current.length === 1 ? `\`\`\`screenplay\nevent ${name}\n${current[0].properties.map(property => `${property.name} ${typeReferenceText(propertyTypeReference(property))}`).join('\n')}\n\`\`\`` : null;
            return { name, content, target: null };
        }
    }
    const imports = [...scanned.flatMap(document => document.symbols.imports), ...(application?.authoringDocuments === undefined ? application?.imports ?? [] : [])].filter(imported => imported.shortName === name);
    if (imports.length > 0) return { name, content: imports.length === 1 ? `\`\`\`screenplay\nimport ${imports[0].qualifiedName}\n\`\`\`` : null, target: null };
    const triggers = scanned.flatMap(document => document.symbols.triggers.filter(trigger => trigger.name === name).map(trigger => ({ document, declaration: trigger })));
    return selected(triggers);
}

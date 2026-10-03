// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLine } from '../Parsing/SourceLine';
import { CommandSyntax } from '../Syntax/Commands';
import { EventSyntax } from '../Syntax/Declarations';
import { ProducesSyntax } from '../Syntax/Reactions';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { ApplicationSyntax } from '../Syntax/Structure';
import { QuickFixCandidate } from './QuickFixCandidate';

// One index per buffer, not a document walk per production or diagnostic.
export class ProductionQuickFixes extends ScreenplaySyntaxWalker {
    readonly eventsByLine = new Map<number, EventSyntax>();
    readonly diagnostics: Diagnostic[] = [];
    private readonly events = new Map<string, { declaration: EventSyntax; properties: Set<string> } | undefined>();
    private readonly commands: CommandSyntax[] = [];
    private hasImports = false;

    constructor(application: ApplicationSyntax, private readonly lines: readonly SourceLine[]) {
        super();
        this.visitApplication(application);
    }

    override visitFileImport(): void { this.hasImports = true; }
    override visitImport(): void { this.hasImports = true; }

    override visitEvent(event: EventSyntax): void {
        this.eventsByLine.set(event.location.line, event);
        this.events.set(event.name, this.events.has(event.name) ? undefined : { declaration: event, properties: new Set(event.properties.map(property => property.name)) });
    }

    override visitCommand(command: CommandSyntax): void {
        this.commands.push(command);
        const identifiers = command.properties.filter(property => property.isIdentifier && !property.type.isOptional && !property.type.isCollection);
        if (identifiers.length === 1) {
            for (const production of command.produces) {
                if (this.isPlain(production) && production.for === null) {
                    this.diagnostics.push({ code: DiagnosticCodes.OmittedProductionDestination, severity: 'information', location: production.location,
                        message: `Plain 'produces ${production.event}' omits its destination - use 'for ${identifiers[0].name}' to explicitly select the command's identifier.` });
                }
            }
        }
        super.visitCommand(command);
    }

    candidates(source: string, lines: readonly SourceLine[]): QuickFixCandidate[] {
        if (this.hasImports) return [];
        // SemanticModelBinder.Commands sets UsesV2 for a destination path absent from the event's
        // payload, including the inferred destination of an inline event. Recognize that exact trigger
        // only with a local, unique, generation-1 contract and a required scalar identifier. Other
        // version triggers (context mappings, specifications, V3+) deliberately do not act as proof.
        const versionAnchored = this.commands.some(command => {
            const identifier = command.properties.find(property => property.isIdentifier && !property.type.isOptional && !property.type.isCollection);
            return command.produces.some(production => {
                const path = production.for?.kind === 'PathExpressionSyntax' ? production.for.path : production.inlineEvent !== null && production.for === null ? identifier?.name : undefined;
                const event = this.localEvent(production);
                return identifier !== undefined && path === identifier.name && event !== undefined && !event.properties.has(path);
            });
        });
        const candidates: QuickFixCandidate[] = [];
        for (const command of this.commands) {
            const identifiers = command.properties.filter(property => property.isIdentifier && !property.type.isOptional && !property.type.isCollection);
            if (identifiers.length !== 1) continue;
            const identifier = identifiers[0].name;
            const omitted = command.produces.filter(production => production.for === null);
            if (omitted.length !== 1 || command.produces.some(production => production.for !== null && (production.for.kind !== 'PathExpressionSyntax' || production.for.path !== identifier))) continue;
            const production = omitted[0];
            const event = this.localEvent(production);
            if (!this.isPlain(production) || event === undefined) continue;
            // If this path already names a payload property it cannot set UsesV2. Otherwise require
            // a preexisting typed destination: promotion must already apply to ALL commands. With no
            // omitted siblings and every explicit sibling naming this identifier, their effective
            // destinations cannot change when PromoteV2Destinations chooses its first destination.
            if (!event.properties.has(identifier) && !versionAnchored) continue;
            const line = lines[production.location.line - 1];
            const next = lines[production.location.line];
            const ending = next === undefined ? (source.includes('\r\n') ? '\r\n' : '\n') : source.slice(line.startOffset + line.raw.length, next.startOffset);
            const indent = next !== undefined && next.content.length > 0 && next.indent > line.indent ? next.indent : line.indent + 2;
            const edit = { start: next?.startOffset ?? source.length, length: 0, text: `${next === undefined ? ending : ''}${' '.repeat(indent)}for ${identifier}${next === undefined ? '' : ending}` };
            candidates.push({ line: production.location.line, fix: { diagnosticCode: DiagnosticCodes.OmittedProductionDestination, title: `State the destination: for ${identifier}`, scope: 'occurrence', edits: [edit] },
                change: { node: production, replacement: { ...production, for: { kind: 'PathExpressionSyntax', path: identifier, location: production.location } } as ProducesSyntax } });
        }
        return candidates;
    }

    private isPlain(production: ProducesSyntax): boolean {
        // Conditional productions intentionally have no condition member in the TS projection.
        return this.lines[production.location.line - 1].content.replace(/^produces\s+/u, '') === production.event;
    }

    private localEvent(production: ProducesSyntax) {
        const event = this.events.get(production.event);
        return event?.declaration.generation === 1 && !event.declaration.hasGenerationMarker ? event : undefined;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationCompilation, compileApplication, Diagnostic, DiagnosticCodes, normalizePlayPath, PlayPlacement } from '@cratis/screenplay-compiler';
import { DocumentSymbols, importablePaths, mergeSymbols, scanDocument } from '@cratis/screenplay-language';

// What the compiler says about a file of the application that the editor's own checks cannot: whether its
// imports resolve, and whether it holds what the module or feature it is placed in can. An import with the
// wrong shape is left out - the editor reports that itself, as it is typed.
const surfacedCodes = new Set<string>([
    DiagnosticCodes.InvalidProducesDeclaration,
    DiagnosticCodes.ProducesWhenWithoutEvent,
    DiagnosticCodes.InvalidPropertyMapping,
    DiagnosticCodes.DuplicateProducesTarget,
    DiagnosticCodes.DuplicateDeclaration,
    DiagnosticCodes.InvalidDescription,
    DiagnosticCodes.DuplicateDescription,
    DiagnosticCodes.EmptyDescription,
    DiagnosticCodes.ExpectedCodeFence,
    DiagnosticCodes.EventSourceIdInPayload,
    DiagnosticCodes.ExplicitProducesTargetsRequired,
    DiagnosticCodes.RedundantEventId,
    DiagnosticCodes.InvalidEventId,
    DiagnosticCodes.InlineEventCollision,
    DiagnosticCodes.InlineEventOutsideCommand,
    DiagnosticCodes.InlineEventGeneration,
    DiagnosticCodes.ReservedProductionMetadata,
    DiagnosticCodes.InvalidEventDocumentation,
    DiagnosticCodes.FileImportMatchesNothing,
    DiagnosticCodes.ImportedFileNotFound,
    DiagnosticCodes.ConflictingImportPlacement,
    DiagnosticCodes.ImportCycle,
    DiagnosticCodes.ModuleInPlacedFile,
    DiagnosticCodes.UnexpectedInPlacedFile,
]);

// The .play files of one workspace folder as one application, keyed by portable path relative to the folder.
// Each file's names are scanned when its text changes; the application is compiled - imports followed, every
// file parsed where it is placed - the first time it is asked about after a change, so a burst of edits costs
// one compilation.
export class WorkspaceApplication {
    readonly #texts = new Map<string, string>();
    readonly #symbols = new Map<string, DocumentSymbols>();
    #compilation: ApplicationCompilation | undefined;

    get paths(): string[] {
        return [...this.#texts.keys()];
    }

    // Sets the text of a file; false when it is what the application already holds.
    set(path: string, text: string): boolean {
        const key = normalizePlayPath(path);
        if (this.#texts.get(key) === text) return false;
        this.#texts.set(key, text);
        this.#symbols.set(key, scanDocument(text.split(/\r?\n/)));
        this.#compilation = undefined;
        return true;
    }

    // Removes a file; false when the application does not hold it.
    delete(path: string): boolean {
        const key = normalizePlayPath(path);
        if (!this.#texts.delete(key)) return false;
        this.#symbols.delete(key);
        this.#compilation = undefined;
        return true;
    }

    // What every other file of the application declares - the file's own names are its own to scan.
    symbolsExcept(path: string): DocumentSymbols {
        const key = normalizePlayPath(path);
        return mergeSymbols(...[...this.#symbols].filter(([other]) => other !== key).map(([, symbols]) => symbols));
    }

    // Inline events are scanned with their slice-owned declarations, so navigation is independent of syntax form.
    eventDeclarations(name: string): { path: string; line: number }[] {
        return [...this.#symbols].flatMap(([path, symbols]) => symbols.events.filter(event => event.name === name).map(event => ({ path, line: event.line })));
    }

    // What compiling the application reports in a file about its imports and its placement.
    diagnosticsFor(path: string): Diagnostic[] {
        const key = normalizePlayPath(path);
        return this.#compiled().diagnostics.filter(diagnostic => diagnostic.location.path === key && surfacedCodes.has(diagnostic.code));
    }

    // Where the imports of the application place a file - undefined when the application does not hold it.
    placementOf(path: string): PlayPlacement | undefined {
        const key = normalizePlayPath(path);
        return this.#compiled().documents.find(document => document.path === key)?.placement;
    }

    // What an import written in a file can name, relative to the file's folder.
    importablePathsFor(path: string): string[] {
        return importablePaths(normalizePlayPath(path), this.paths);
    }

    #compiled(): ApplicationCompilation {
        this.#compilation ??= compileApplication(this.#texts);
        return this.#compilation;
    }
}

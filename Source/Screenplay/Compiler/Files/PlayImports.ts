// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { DiscoveredImport, placementFrom } from '../Parsing/ImportDiscovery';
import { discoverImports } from '../ScreenplayCompiler';
import { PlacedPlayDocument, PlayDocumentSource } from './PlayDocumentSource';
import { hasWildcard, matchesPlayPattern, normalizePlayPath, resolvePlayPattern, staticFolderOf } from './PlayGlob';
import { describePlacement, documentPlacement, isWithinOrSame, PlayPlacement, samePlacement } from './PlayPlacement';

// How deep a placement may get, and how often one file's placement may change, before the imports behind it
// are taken to be a cycle.
export const maximumImportDepth = 32;

// The documents of an application, and the diagnostics settling them produced.
export interface ResolvedImports {
    readonly documents: readonly PlacedPlayDocument[];
    readonly diagnostics: readonly Diagnostic[];
}

// Settles which documents make up an application and where each one's top level belongs, by following the
// file imports from a set of root documents - the port of the C# PlayImports.Resolve.
//
// A root document is a whole document. Every file an import matches joins the application once, however many
// imports match it. When several imports place the same file, the deepest placement wins; two placements where
// neither lies inside the other are a conflict, and placements that keep deepening are an import cycle. A file's
// placement depends on the placements of the files importing it, so it is recomputed from every importer's
// current placement until nothing changes - the order files are found in never decides where one belongs. The
// unimported roots keep the order they were given in; their imports follow in authored traversal order.
export function resolveImports(roots: Iterable<string>, source: PlayDocumentSource, languages?: ReadonlySet<string>): ResolvedImports {
    const resolution = new Resolution(source, languages);
    for (const root of new Set([...roots].map(normalizePlayPath))) {
        resolution.addRoot(root);
    }
    resolution.settle();
    return { documents: resolution.documents(), diagnostics: resolution.diagnostics };
}

interface FoundImport {
    readonly discovered: DiscoveredImport;
    readonly targets: readonly string[];
}

// What each import currently contributes to a file's placement, keyed by the importing file and the import.
type Contributions = Map<string, { readonly importer: string; readonly index: number; readonly placement: PlayPlacement }>;

class Resolution {
    readonly diagnostics: Diagnostic[] = [];
    readonly #found: string[] = [];
    readonly #roots = new Set<string>();
    readonly #sources = new Map<string, string>();
    readonly #imports = new Map<string, readonly FoundImport[]>();
    readonly #contributions = new Map<string, Contributions>();
    readonly #placements = new Map<string, PlayPlacement>();
    readonly #pending: string[] = [];
    readonly #changes = new Map<string, number>();
    readonly #unresolved = new Set<string>();

    constructor(private readonly source: PlayDocumentSource, private readonly languages?: ReadonlySet<string>) {}

    addRoot(root: string): void {
        this.#roots.add(root);
        this.#find(root);
    }

    // Placements only deepen, and none is allowed past the maximum depth, so this ends.
    settle(): void {
        for (let file = this.#pending.shift(); file !== undefined; file = this.#pending.shift()) {
            this.#propagate(file);
        }
        this.#reportConflicts();
        const unresolved = [...this.#unresolved];
        for (const importer of unresolved) {
            for (const target of (this.#imports.get(importer) ?? []).flatMap(imported => imported.targets)) {
                if (!this.#unresolved.has(target)) {
                    this.#unresolved.add(target);
                    unresolved.push(target);
                }
            }
        }
    }

    documents(): PlacedPlayDocument[] {
        return this.#orderedPaths().map(path => ({ path, source: this.#sources.get(path) as string, placement: this.#placement(path) ?? documentPlacement, isPlacementResolved: !this.#unresolved.has(path) }));
    }

    #orderedPaths(): string[] {
        // Folder discovery also includes imported documents as roots. Barrel order wins over path order.
        const imported = new Set([...this.#imports.values()].flatMap(imports => imports.flatMap(imported => imported.targets)));
        const seen = new Set<string>();
        const ordered: string[] = [];
        for (const root of [...this.#found.filter(path => !imported.has(path)), ...this.#found]) {
            const pending = [root];
            for (let path = pending.pop(); path !== undefined; path = pending.pop()) {
                if (seen.has(path)) continue;
                seen.add(path);
                ordered.push(path);
                pending.push(...(this.#imports.get(path) ?? []).flatMap(imported => imported.targets).reverse());
            }
        }
        return ordered;
    }

    #find(file: string): void {
        if (this.#sources.has(file)) return;
        this.#found.push(file);
        const text = this.source.read(file);
        this.#sources.set(file, text);
        this.#imports.set(file, discoverImports(text, file, this.languages).map(discovered => ({ discovered, targets: this.#targets(file, discovered) })));
        this.#pending.push(file);
    }

    #targets(file: string, discovered: DiscoveredImport): string[] {
        const { pattern, location } = discovered.fileImport;
        const resolved = resolvePlayPattern(file, pattern);
        // The default sort compares UTF-16 code units, which is the C# ordinal order.
        const matches = [...new Set([...this.source.filesBeneath(staticFolderOf(resolved))]
            .map(normalizePlayPath)
            .filter(path => path !== file && matchesPlayPattern(resolved, path)))].sort();
        if (matches.length === 0) {
            this.#report(hasWildcard(pattern)
                ? { severity: 'warning', code: DiagnosticCodes.FileImportMatchesNothing, message: `Import '${pattern}' matches no .play file`, location }
                : error(DiagnosticCodes.ImportedFileNotFound, `Imported file '${pattern}' does not exist`, location));
        }
        return matches;
    }

    #propagate(file: string): void {
        const placement = this.#placement(file);
        (this.#imports.get(file) as readonly FoundImport[]).forEach(({ discovered, targets }, index) => {
            let target = placement === undefined ? undefined : placementFrom(discovered, placement);
            if (target !== undefined && target.length > maximumImportDepth) {
                this.#report(error(DiagnosticCodes.ImportCycle,
                    `Import '${discovered.fileImport.pattern}' places files deeper than ${maximumImportDepth} levels - the imports form a cycle`, discovered.fileImport.location));
                this.#unresolved.add(file);
                target = undefined;
            }
            for (const path of targets) {
                this.#find(path);
                this.#contribute(path, file, index, target);
            }
        });
    }

    #contribute(file: string, importer: string, index: number, placement: PlayPlacement | undefined): void {
        const contributions = this.#contributions.get(file) ?? new Map();
        this.#contributions.set(file, contributions);
        const key = JSON.stringify([importer, index]);
        const current = contributions.get(key);
        const changed = placement === undefined
            ? contributions.delete(key)
            : current === undefined || !samePlacement(current.placement, placement);
        if (placement !== undefined) {
            contributions.set(key, { importer, index, placement });
        }
        if (changed) {
            this.#recompute(file);
        }
    }

    #recompute(file: string): void {
        const placement = this.#deepest(file);
        const current = this.#placements.get(file);
        const changed = current === undefined ? placement !== undefined : placement === undefined || !samePlacement(current, placement);
        if (!changed) return;

        // A placement that keeps changing is held up by imports that feed back into themselves.
        const changes = (this.#changes.get(file) ?? 0) + 1;
        this.#changes.set(file, changes);
        if (changes > maximumImportDepth) {
            this.#unresolved.add(file);
            for (const { discovered: { fileImport } } of this.#imports.get(file) as readonly FoundImport[]) {
                this.#report(error(DiagnosticCodes.ImportCycle,
                    `Import '${fileImport.pattern}' is part of imports that keep placing each other - the imports form a cycle`, fileImport.location));
            }
            return;
        }
        if (placement === undefined) {
            this.#placements.delete(file);
        } else {
            this.#placements.set(file, placement);
        }
        this.#pending.push(file);
    }

    #placement(file: string): PlayPlacement | undefined {
        return this.#placements.get(file) ?? this.#deepest(file);
    }

    #deepest(file: string): PlayPlacement | undefined {
        const candidates = [...(this.#roots.has(file) ? [documentPlacement] : []), ...this.#ordered(file).map(contribution => contribution.placement)];
        return candidates.reduce<PlayPlacement | undefined>(
            (deepest, candidate) => deepest === undefined || isWithinOrSame(candidate, deepest) ? candidate : deepest, undefined);
    }

    #ordered(file: string) {
        return [...(this.#contributions.get(file)?.values() ?? [])]
            .sort((left, right) => left.importer === right.importer ? left.index - right.index : left.importer < right.importer ? -1 : 1);
    }

    #reportConflicts(): void {
        for (const file of this.#found) {
            const placement = this.#placement(file);
            if (placement === undefined) continue;
            for (const { importer, index, placement: contribution } of this.#ordered(file)) {
                if (!isWithinOrSame(placement, contribution)) {
                    this.#unresolved.add(file);
                    const { fileImport } = (this.#imports.get(importer) as readonly FoundImport[])[index].discovered;
                    this.#report(error(DiagnosticCodes.ConflictingImportPlacement,
                        `'${file}' is imported into both ${describePlacement(placement)} and ${describePlacement(contribution)} - a file belongs in one place`, fileImport.location));
                }
            }
        }
    }

    // Reports a diagnostic once - a file whose placement changes runs its imports again.
    #report(diagnostic: Diagnostic): void {
        const key = keyOf(diagnostic);
        if (!this.diagnostics.some(other => keyOf(other) === key)) {
            this.diagnostics.push(diagnostic);
        }
    }
}

const keyOf = (diagnostic: Diagnostic): string => JSON.stringify([diagnostic.severity, diagnostic.code, diagnostic.message, diagnostic.location]);
const error = (code: string, message: string, location: SourceLocation): Diagnostic => ({ severity: 'error', code, message, location });

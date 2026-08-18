// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { isAbsoluteFileReferencePath } from '@cratis/screenplay-language';

// Nothing records the root a `file` path is relative to — not the document, not the CLI, not any config
// — so the extension probes an ordered ladder of plausible roots and takes the first that exists.
export interface FileReferenceRoots {
    // The `screenplay.sourceRoot` setting, already made absolute. Probed first, so that a workspace
    // whose layout the automatic probing gets wrong has a deterministic way to state the answer.
    configured: readonly string[];
    workspaceFolder?: string;
    documentDirectory?: string;
    // Plausible source roots found beneath the workspace, such as `Source` and each `Source/*`.
    sourceRoots: readonly string[];
}

// What a search came back with, and whether it came back with all of it. A search that stopped at a cap
// hands back an arbitrary subset, so the count of what survived says nothing about how many files exist
// — `truncated` is what keeps a cut-short answer from being read as a complete one.
export interface FileReferenceSearch {
    files: readonly string[];
    truncated: boolean;
}

// The file system, narrowed to the two questions the ladder asks of it.
export interface FileReferenceProbe {
    exists(path: string): Promise<boolean>;
    // Every file in the workspace carrying the given base name.
    search(baseName: string): Promise<FileReferenceSearch>;
}

export type FileReferenceResolution =
    | { kind: 'resolved'; path: string }
    // `truncated` records that the search was cut short, so the candidates are the files that were seen
    // rather than every file that matches. It changes nothing about the outcome — nothing is linked
    // either way — but the tooltip must not claim a count it does not know.
    | { kind: 'ambiguous'; candidates: readonly string[]; truncated: boolean }
    | { kind: 'unresolved' };

function normalize(path: string): string {
    return path.replace(/\\/g, '/');
}

function join(root: string, path: string): string {
    return `${normalize(root).replace(/\/+$/, '')}/${normalize(path).replace(/^\/+/, '')}`;
}

export function baseNameOf(path: string): string {
    const segments = normalize(path).split('/');
    return segments[segments.length - 1] ?? path;
}

// The paths the ladder probes, in the order it probes them, with duplicates dropped so a root that
// happens to be the workspace folder is not asked about twice. An absolute path names one place and
// is probed there alone — the ladder has nothing to add to a path that already states where it is.
export function candidatePathsFor(declaredPath: string, roots: FileReferenceRoots): string[] {
    if (declaredPath.length === 0) return [];
    if (isAbsoluteFileReferencePath(declaredPath)) return [normalize(declaredPath)];

    const rootsInOrder = [
        ...roots.configured,
        ...(roots.workspaceFolder === undefined ? [] : [roots.workspaceFolder]),
        ...(roots.documentDirectory === undefined ? [] : [roots.documentDirectory]),
        ...roots.sourceRoots,
    ];

    return [...new Set(rootsInOrder.map((root) => join(root, declaredPath)))];
}

// Whether a file found by base name is the file the document declared — its path has to end with the
// declared path, not merely share its last segment, or `Register.cs` would match every `Register.cs`
// in the tree regardless of the folders the document named.
function matchesDeclaredPath(found: string, declaredPath: string): boolean {
    const candidate = normalize(found);
    const declared = normalize(declaredPath).replace(/^\/+/, '');
    return candidate === declared || candidate.endsWith(`/${declared}`);
}

// Walks the ladder and stops at the first hit. When nothing on it exists, falls back to searching the
// workspace for the declared base name and keeping only the files whose path ends with the declared
// path. That fallback answers only when it finds exactly one — several is reported as ambiguous rather
// than resolved, because picking one of them would be a guess, and a wrong link is worse than none.
//
// "Exactly one" is a claim about the whole workspace, so it can only be made about a complete search. A
// search that stopped at its cap saw an arbitrary subset: the lone survivor of that subset may sit
// beside a second match that was simply never returned. A truncated search therefore never resolves —
// it reports the candidates it did see and links none of them, which is the same honest answer as any
// other case where the document does not say which file it meant.
export async function resolveFileReference(
    declaredPath: string,
    roots: FileReferenceRoots,
    probe: FileReferenceProbe,
): Promise<FileReferenceResolution> {
    for (const candidate of candidatePathsFor(declaredPath, roots)) {
        if (await probe.exists(candidate)) return { kind: 'resolved', path: candidate };
    }

    if (declaredPath.length === 0 || isAbsoluteFileReferencePath(declaredPath)) {
        return { kind: 'unresolved' };
    }

    const found = await probe.search(baseNameOf(declaredPath));
    const matches = [...new Set(found.files.map(normalize))].filter((candidate) =>
        matchesDeclaredPath(candidate, declaredPath),
    );

    if (matches.length === 0) return { kind: 'unresolved' };
    if (matches.length === 1 && !found.truncated) return { kind: 'resolved', path: matches[0] };
    return { kind: 'ambiguous', candidates: matches, truncated: found.truncated };
}

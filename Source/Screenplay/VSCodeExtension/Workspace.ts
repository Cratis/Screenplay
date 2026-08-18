// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'node:path';
import * as vscode from 'vscode';
import { isAbsoluteFileReferencePath } from '@cratis/screenplay-language';
import { FileReferenceProbe, FileReferenceRoots, FileReferenceSearch } from './FileReferenceResolution';

// The setting a workspace states its root with when the automatic probing cannot work it out.
const sourceRootSetting = 'sourceRoot';
const configurationSection = 'screenplay';

// The folders a source root conventionally lives under. Each one, and each of its immediate children,
// is a plausible root — `Source` for a Cratis layout, `src` for the common alternative.
const sourceRootContainers = ['Source', 'src'];

// How many files the base-name fallback is willing to look at. It exists to answer a single question —
// is there exactly one file this path could mean — so a handful of results is enough to answer it.
// One more than this is asked for, because a set that fills the cap is a set that may have been cut
// short, and the resolver has to be told that rather than left to read a truncated set as a complete one.
const searchLimit = 32;

// A glob has no escape character, so a base name carrying pattern syntax cannot be searched for
// literally. `?` matches exactly one character in a segment, so substituting it for each metacharacter
// keeps the real file in the result set — `[id].tsx` is searched for as `?id?.tsx`, which it matches —
// and the literal suffix check in `matchesDeclaredPath` drops whatever else the widened pattern caught.
const globMetaCharacters = /[*?[\]{}]/g;

function asLiteralAsAGlobAllows(baseName: string): string {
    return baseName.replace(globMetaCharacters, '?');
}

function configuredRoots(document: vscode.TextDocument, workspaceFolder: string | undefined): string[] {
    const configured = vscode.workspace
        .getConfiguration(configurationSection, document.uri)
        .get<string | string[]>(sourceRootSetting);
    const entries = typeof configured === 'string' ? [configured] : (configured ?? []);

    return entries
        .filter((entry) => entry.trim().length > 0)
        .map((entry) => entry.trim())
        .map((entry) =>
            isAbsoluteFileReferencePath(entry) || workspaceFolder === undefined
                ? entry
                : path.join(workspaceFolder, entry),
        );
}

async function subDirectoriesOf(directory: string): Promise<string[]> {
    try {
        const entries = await vscode.workspace.fs.readDirectory(vscode.Uri.file(directory));
        return entries
            .filter(([, type]) => (type & vscode.FileType.Directory) !== 0)
            .map(([name]) => path.join(directory, name));
    } catch {
        return [];
    }
}

// The conventional source roots that actually exist beneath the workspace folder.
async function discoveredSourceRoots(workspaceFolder: string | undefined): Promise<string[]> {
    if (workspaceFolder === undefined) return [];

    const roots: string[] = [];
    for (const container of sourceRootContainers) {
        const candidate = path.join(workspaceFolder, container);
        const children = await subDirectoriesOf(candidate);
        if (children.length === 0) continue;
        roots.push(candidate, ...children);
    }

    return roots;
}

export async function rootsFor(document: vscode.TextDocument): Promise<FileReferenceRoots> {
    const workspaceFolder = vscode.workspace.getWorkspaceFolder(document.uri)?.uri.fsPath;

    return {
        configured: configuredRoots(document, workspaceFolder),
        workspaceFolder,
        documentDirectory: path.dirname(document.uri.fsPath),
        sourceRoots: await discoveredSourceRoots(workspaceFolder),
    };
}

export function workspaceProbe(): FileReferenceProbe {
    return {
        async exists(candidate: string): Promise<boolean> {
            try {
                const stat = await vscode.workspace.fs.stat(vscode.Uri.file(candidate));
                return (stat.type & vscode.FileType.File) !== 0;
            } catch {
                return false;
            }
        },
        async search(baseName: string): Promise<FileReferenceSearch> {
            const found = await vscode.workspace.findFiles(
                `**/${asLiteralAsAGlobAllows(baseName)}`,
                '**/node_modules/**',
                searchLimit + 1,
            );

            // Nothing about `maxResults` promises an order or a complete set, so the only thing a full
            // result set proves is that more files may exist beyond it. Asking for one over the limit is
            // what turns that into an answer: fewer back than asked for means the search saw everything.
            return { files: found.map((uri) => uri.fsPath), truncated: found.length > searchLimit };
        },
    };
}

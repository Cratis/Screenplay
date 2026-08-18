// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'node:path';
import * as vscode from 'vscode';
import { isAbsoluteFileReferencePath } from '@cratis/screenplay-language';
import { FileReferenceProbe, FileReferenceRoots } from './FileReferenceResolution';

// The setting a workspace states its root with when the automatic probing cannot work it out.
const sourceRootSetting = 'sourceRoot';
const configurationSection = 'screenplay';

// The folders a source root conventionally lives under. Each one, and each of its immediate children,
// is a plausible root — `Source` for a Cratis layout, `src` for the common alternative.
const sourceRootContainers = ['Source', 'src'];

// How many files the base-name fallback is willing to look at. It exists to answer a single question —
// is there exactly one file this path could mean — so a handful of results is enough to answer it.
const searchLimit = 32;

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
        async search(baseName: string): Promise<readonly string[]> {
            const found = await vscode.workspace.findFiles(
                `**/${baseName}`,
                '**/node_modules/**',
                searchLimit,
            );
            return found.map((uri) => uri.fsPath);
        },
    };
}

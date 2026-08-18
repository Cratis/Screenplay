// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { FileReference, fileReferences, languageId } from '@cratis/screenplay-language';
import { FileReferenceResolution, resolveFileReference } from './FileReferenceResolution';
import { rootsFor, workspaceProbe } from './Workspace';

// A document carries the same path many times over, and a real captured application carries hundreds of
// them, so each answer is kept until something could have changed it. Without this the base-name fallback
// would search the whole workspace once per unresolved reference, on every keystroke.
const resolutions = new Map<string, FileReferenceResolution>();

function toRange(reference: FileReference): vscode.Range {
    return new vscode.Range(
        reference.line,
        reference.startColumn - 1,
        reference.line,
        reference.endColumn - 1,
    );
}

// A reference that resolves to nothing gets no link at all. A `.play` is read in Studio and in CI where
// the tree is absent, and Arc emits a synthesised path when it does not know the real one, so a path that
// points nowhere is an ordinary state of an ordinary document rather than a defect worth reporting.
function toLink(reference: FileReference, resolution: FileReferenceResolution): vscode.DocumentLink | undefined {
    if (resolution.kind === 'resolved') {
        const link = new vscode.DocumentLink(toRange(reference), vscode.Uri.file(resolution.path));
        link.tooltip = resolution.path;
        return link;
    }

    // Several files could be meant and nothing in the document says which. Naming them is the honest
    // answer; picking one would be a guess, and a link to the wrong file is worse than no link.
    if (resolution.kind === 'ambiguous') {
        const link = new vscode.DocumentLink(toRange(reference));
        link.tooltip = `${resolution.candidates.length} files match '${reference.path}' — none linked:\n${resolution.candidates.join('\n')}`;
        return link;
    }

    return undefined;
}

async function resolutionFor(
    document: vscode.TextDocument,
    declaredPath: string,
): Promise<FileReferenceResolution> {
    const key = `${document.uri.toString()} ${declaredPath}`;
    const cached = resolutions.get(key);
    if (cached !== undefined) return cached;

    const resolution = await resolveFileReference(declaredPath, await rootsFor(document), workspaceProbe());
    resolutions.set(key, resolution);
    return resolution;
}

export function registerFileLinks(context: vscode.ExtensionContext): void {
    const forget = () => resolutions.clear();

    context.subscriptions.push(
        vscode.workspace.onDidCreateFiles(forget),
        vscode.workspace.onDidDeleteFiles(forget),
        vscode.workspace.onDidRenameFiles(forget),
        vscode.workspace.onDidChangeWorkspaceFolders(forget),
        vscode.workspace.onDidChangeConfiguration((event) => {
            if (event.affectsConfiguration('screenplay.sourceRoot')) forget();
        }),
        vscode.languages.registerDocumentLinkProvider(languageId, {
            async provideDocumentLinks(document, token) {
                const links: vscode.DocumentLink[] = [];

                for (const reference of fileReferences(document.getText().split(/\r?\n/))) {
                    if (token.isCancellationRequested) break;
                    const link = toLink(reference, await resolutionFor(document, reference.path));
                    if (link) links.push(link);
                }

                return links;
            },
        }),
    );
}

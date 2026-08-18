// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DocumentLink, DocumentLinkProviderStub, Uri, reset, state } from '../../vscode.stub';
import { registerFileLinks } from '../../FileLinks';

// The tree a spec states it is running against. Everything absent from it is absent from the workspace.
export interface Workspace {
    folder?: string;
    files?: readonly string[];
    sourceRoot?: string | readonly string[];
}

// `FileLinks` caches an answer per document and path in a module-global map, and that map outlives an
// individual spec in the same file. Handing every document its own uri keeps one spec's answer from
// being the next one's, without reaching into the module for a way to empty it.
let documentCount = 0;

export interface Document {
    uri: Uri;
    getText(): string;
}

export function an_editor_with(workspace: Workspace = {}): void {
    reset();
    state.workspaceFolder = workspace.folder;
    for (const file of workspace.files ?? []) state.files.add(file);
    if (workspace.sourceRoot !== undefined) {
        state.configuration.set('screenplay.sourceRoot', workspace.sourceRoot);
    }

    registerFileLinks({ subscriptions: [] } as unknown as Parameters<typeof registerFileLinks>[0]);
}

export function a_document(text: string, directory = '/repo/Documentation'): Document {
    documentCount += 1;
    return {
        uri: Uri.file(`${directory}/document-${documentCount}.play`),
        getText: () => text,
    };
}

function the_provider(): DocumentLinkProviderStub {
    const registered = state.linkProviders[state.linkProviders.length - 1];
    if (registered === undefined) throw new Error('No document link provider was registered');
    return registered.provider;
}

export async function links_in(document: Document): Promise<DocumentLink[]> {
    return await the_provider().provideDocumentLinks(document, { isCancellationRequested: false });
}

export function completed(link: DocumentLink): DocumentLink | undefined {
    const provider = the_provider();
    if (provider.resolveDocumentLink === undefined) {
        throw new Error('The provider offers no way to complete a link');
    }
    return provider.resolveDocumentLink(link, { isCancellationRequested: false });
}

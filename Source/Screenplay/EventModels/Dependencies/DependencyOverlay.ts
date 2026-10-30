// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** A link between two slices, drawn from the consumer to the producer. All ids are board ids. */
export interface DependencyOverlayLink {
    readonly from: string;
    readonly to: string;
    readonly crossing: boolean;
}

/** What selecting one slice, feature or module emphasises on the board. All ids are board ids. */
export interface DependencyOverlayScope {
    readonly id: string;
    readonly kind: 'slice' | 'feature' | 'module';
    readonly selected: readonly string[];
    readonly dependencies: readonly string[];
    readonly dependants: readonly string[];
    /** Features and modules that hold an emphasised slice but are not emphasised themselves; they are not dimmed. */
    readonly ancestors: readonly string[];
    readonly links: readonly DependencyOverlayLink[];
}

/** The emphasis of every scope of a model, precomputed so it can be sent to a webview as JSON. */
export interface DependencyOverlay {
    readonly scopes: readonly DependencyOverlayScope[];
}

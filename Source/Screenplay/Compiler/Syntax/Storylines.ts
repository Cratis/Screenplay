// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

// The kinds of element a storyline step names - the port of the C# StorylineElementKind.
export type StorylineElementKind = 'Module' | 'Feature' | 'Slice' | 'Command' | 'Event' | 'ReadModel' | 'Query' | 'Screen' | 'Reaction';

// 'storyline <Name> ["<title>"]' - a named, ordered path through the model. Authoring metadata, never executable.
export interface StorylineSyntax extends SyntaxNode {
    readonly kind: 'StorylineSyntax';
    readonly name: string;
    readonly title: string | null;
    readonly steps: readonly StorylineStepSyntax[];
    readonly description?: string;
}

// '<kind> <reference> ["<note>"]' - one step of a storyline.
export interface StorylineStepSyntax extends SyntaxNode {
    readonly kind: 'StorylineStepSyntax';
    readonly elementKind: StorylineElementKind;
    readonly target: string;
    readonly note: string | null;
}

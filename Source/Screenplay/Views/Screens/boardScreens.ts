// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { screenDocumentKey } from '@cratis/screenplay-event-models';

// The screens a compiled board carries, by the key the compiler wrote them under: the identity the board's
// document gives the slice, and the identity of the actor whose row draws the screen.
export type BoardScreenDocuments = Readonly<Record<string, string>>;

export const noBoardScreens: BoardScreenDocuments = {};

// The Scene document of one role's screen, or nothing when the board carries none for it. Matching is exact -
// drawing a near-miss would put one role's screen on another's row - and an unnamed slice or actor matches
// nothing at all rather than the first key that happens to start with a colon.
export const boardScreenDocumentFor = (screens: BoardScreenDocuments, sliceId: string, actorId: string): string | undefined =>
    sliceId.length === 0 || actorId.length === 0 ? undefined : screens[screenDocumentKey(sliceId, actorId)];

// Whether the board carries any screen at all, so a model without one builds no Scene runtime.
export const hasBoardScreens = (screens: BoardScreenDocuments): boolean => Object.keys(screens).length > 0;

// The screens out of a message a host was sent, accepting only string values: the webview is handed its board
// over a message channel, so anything else in it is left out rather than handed to Scene's reader.
export const readBoardScreens = (value: unknown): BoardScreenDocuments => {
    if (typeof value !== 'object' || value === null || Array.isArray(value)) return noBoardScreens;
    const screens: Record<string, string> = {};
    for (const [key, document] of Object.entries(value as Record<string, unknown>)) {
        if (typeof document === 'string') screens[key] = document;
    }
    return screens;
};

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Where a screen's Scene document is held: the identity the board document gave the slice, and the identity of
// the user experience actor whose row draws it. The board asks for exactly these two when it draws a cell, so a
// key that matches is the one screen that belongs there and a key that does not simply finds nothing.
export const screenDocumentKey = (sliceId: string, actorId: string): string => `${sliceId}:${actorId}`;

// The Scene documents of an application's screens, by the key above.
export type ScreenDocuments = Readonly<Record<string, string>>;

export const noScreenDocuments: ScreenDocuments = {};

// The document of one role's screen, or nothing when there is none. Matching is exact: a near miss would draw
// one slice's screen in another's place.
export const screenDocumentFor = (screens: ScreenDocuments, sliceId: string, actorId: string): string | undefined =>
    sliceId.length === 0 || actorId.length === 0 ? undefined : screens[screenDocumentKey(sliceId, actorId)];

// Whether there is any screen at all, so a board without one builds no renderer and no Scene runtime.
export const hasScreenDocuments = (screens: ScreenDocuments): boolean => Object.keys(screens).length > 0;

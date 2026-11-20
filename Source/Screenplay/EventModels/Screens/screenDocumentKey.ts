// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The key one role's screen is held under: the identity the board's document gives the slice, and the identity
// of the user experience actor whose row draws it. Studio's own preview writes the same key, so a host that
// draws screens from either looks them up the same way.
export const screenDocumentKey = (sliceId: string, actorId: string): string => `${sliceId}:${actorId}`;

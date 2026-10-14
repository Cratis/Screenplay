// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface GenerationPatternToken {
    readonly characters: string;
    readonly minimum: number;
    readonly maximum: number;
    readonly segment?: string;
}

export function tokenWidth(token: GenerationPatternToken): number { return token.segment === 'yyyy' ? 4 : token.segment !== undefined ? 2 : 1; }

export function expandToken(token: GenerationPatternToken, date?: Date): string[] {
    if (token.segment === undefined) return [token.characters];
    if (date === undefined) return Array<string>(tokenWidth(token)).fill('0123456789');
    const year = date.getUTCFullYear();
    const value = token.segment === 'yyyy' ? year : token.segment === 'yy' ? year % 100 : token.segment === 'MM' ? date.getUTCMonth() + 1 : date.getUTCDate();
    return [...value.toString().padStart(tokenWidth(token), '0')];
}

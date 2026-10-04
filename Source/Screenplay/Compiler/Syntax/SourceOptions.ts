// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InvalidSyntaxJson } from './InvalidSyntaxJson';

export type NumericMode = 'legacy' | 'exact';
export interface SourceOptions { readonly numericMode: NumericMode; }
export const legacySourceOptions: SourceOptions = Object.freeze({ numericMode: 'legacy' });
export const exactSourceOptions: SourceOptions = Object.freeze({ numericMode: 'exact' });
export const invalidSourceOptions: SourceOptions = Object.freeze({ numericMode: 'invalid' as NumericMode });

// Validate before selecting neutrality or a mode. Missing options are defaulted by the owning root,
// not here: an explicitly present null/undefined/malformed assertion must never become Legacy.
export function validatedSourceOptions(value: unknown): SourceOptions {
    if (typeof value !== 'object' || value === null || (Object.getPrototypeOf(value) !== Object.prototype && Object.getPrototypeOf(value) !== null) || Reflect.ownKeys(value).length !== 1 || !Object.hasOwn(value, 'numericMode')) throw new InvalidSyntaxJson('Malformed source numeric options.');
    const mode = (value as { numericMode: unknown }).numericMode;
    if (mode !== 'legacy' && mode !== 'exact') throw new InvalidSyntaxJson('Malformed source numeric options.');
    return mode === 'exact' ? exactSourceOptions : legacySourceOptions;
}

const authoredDocuments = new WeakSet<object>();
export function recordAuthoredDocument(root: object): void { authoredDocuments.add(root); }
export function isAuthoredDocument(root: object): boolean { return authoredDocuments.has(root); }

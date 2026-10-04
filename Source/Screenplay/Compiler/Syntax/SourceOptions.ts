// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export type NumericMode = 'legacy' | 'exact';
export interface SourceOptions { readonly numericMode: NumericMode; }
export const legacySourceOptions: SourceOptions = Object.freeze({ numericMode: 'legacy' });
export const exactSourceOptions: SourceOptions = Object.freeze({ numericMode: 'exact' });

const authoredDocuments = new WeakSet<object>();
export function recordAuthoredDocument(root: object): void { authoredDocuments.add(root); }
export function isAuthoredDocument(root: object): boolean { return authoredDocuments.has(root); }

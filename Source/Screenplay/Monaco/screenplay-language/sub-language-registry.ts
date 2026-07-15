// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { languages } from 'monaco-editor';

export type MonarchTokenRules = languages.IMonarchLanguage['tokenizer'][string];

export interface SubLanguageCompletion {
    label: string;
    insertText: string;
    documentation: string;
}

export interface SubLanguageDefinition {
    tokens: MonarchTokenRules;
    completions?: SubLanguageCompletion[];
    hovers?: Record<string, string>;
}

export interface SubLanguage extends SubLanguageDefinition {
    keyword: string;
}

const subLanguages = new Map<string, SubLanguage>();
const listeners = new Set<() => void>();

export function registerSubLanguage(keyword: string, definition: SubLanguageDefinition): void {
    subLanguages.set(keyword, { keyword, ...definition });
    listeners.forEach((listener) => listener());
}

export function getSubLanguages(): SubLanguage[] {
    return [...subLanguages.values()];
}

export function getSubLanguage(keyword: string): SubLanguage | undefined {
    return subLanguages.get(keyword);
}

export function onSubLanguagesChanged(listener: () => void): void {
    listeners.add(listener);
}

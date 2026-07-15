// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    Monaco,
    languageConfiguration,
    languageExtensionPoint,
    languageId,
} from './language';
import { createTokensProvider } from './tokens';
import { createCompletionProvider } from './completions';
import { createHoverProvider } from './hover';
import { attachDiagnostics } from './diagnostics';
import {
    getSubLanguage,
    getSubLanguages,
    onSubLanguagesChanged,
    registerSubLanguage,
} from './sub-language-registry';
import { pdl } from './sub-languages/pdl';
import { cdl } from './sub-languages/cdl';
import { screenplayDark, screenplayDarkThemeName } from './themes/screenplay-dark';
import { screenplayLight, screenplayLightThemeName } from './themes/screenplay-light';

const registeredInstances = new Set<Monaco>();

function applyTokensProvider(monaco: Monaco): void {
    monaco.languages.setMonarchTokensProvider(languageId, createTokensProvider(getSubLanguages()));
}

// Sub-languages registered after register() recompose the tokenizer on the fly.
onSubLanguagesChanged(() => registeredInstances.forEach(applyTokensProvider));

// Registers the built-in PDL and CDL sub-languages. Hosts that do not go through
// register() — such as the VSCode extension — call this before using the registry.
export function ensureBuiltInSubLanguages(): void {
    if (!getSubLanguage('projection')) registerSubLanguage('projection', pdl);
    if (!getSubLanguage('capture')) registerSubLanguage('capture', cdl);
}

export function register(monaco: Monaco): void {
    if (registeredInstances.has(monaco)) return;

    ensureBuiltInSubLanguages();

    registeredInstances.add(monaco);

    monaco.languages.register(languageExtensionPoint);
    monaco.languages.setLanguageConfiguration(languageId, languageConfiguration);
    applyTokensProvider(monaco);
    monaco.languages.registerCompletionItemProvider(languageId, createCompletionProvider(monaco));
    monaco.languages.registerHoverProvider(languageId, createHoverProvider());
    monaco.editor.defineTheme(screenplayDarkThemeName, screenplayDark);
    monaco.editor.defineTheme(screenplayLightThemeName, screenplayLight);
    attachDiagnostics(monaco);
}

export {
    clauseKeywords,
    codeBlockTags,
    conceptAttributes,
    constructKeywords,
    languageId,
    primitiveTypes,
    sliceTypes,
} from './language';
export type { Monaco } from './language';
export { getSubLanguage, getSubLanguages, registerSubLanguage } from './sub-language-registry';
export type {
    MonarchTokenRules,
    SubLanguage,
    SubLanguageCompletion,
    SubLanguageDefinition,
} from './sub-language-registry';
export { pdl } from './sub-languages/pdl';
export { cdl } from './sub-languages/cdl';
export { screenplayDarkThemeName } from './themes/screenplay-dark';
export { screenplayLightThemeName } from './themes/screenplay-light';
export { enclosingChain, fenceMap, firstWord, indentOf } from './document-context';
export { knownEventNames, scanDocument } from './symbols';
export type {
    ConceptSymbol,
    DocumentSymbols,
    EventSymbol,
    ImportSymbol,
    NamedSymbol,
    PolicySymbol,
    PropertySymbol,
    QuerySymbol,
} from './symbols';
export { attributeDocs, contextVariableDocs, keywordDocs } from './keyword-docs';
export { contextVariableItems, producesItems } from './completion-items';
export type { CompletionEntry } from './completion-items';
export { completionEntriesFor, planCompletions } from './completion-planner';
export type { CompletionPlan } from './completion-planner';
export { hoverContent } from './hover-content';
export { validateLines } from './validation';
export type { ValidationIssue, ValidationSeverity } from './validation';

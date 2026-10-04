// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    Monaco,
    languageConfiguration,
    languageExtensionPoint,
    languageId,
} from './language';
import { createTokensProvider } from './tokens';
import { CompletionOptions, createCompletionProvider } from './completions';
import { responseTokens, responseTokenTypes } from './response-tokens';
import { createHoverProvider } from './hover';
import { createInlayHintsProvider } from './inlay-hints';
import { eventSourceIdentifier, eventSourceReferenceAt } from './event-source-authoring';
import { createCodeActionProvider } from './code-actions';
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

// What a host can tell the language service beyond the models it holds.
export type LanguageServiceOptions = CompletionOptions;

export function register(monaco: Monaco, options: LanguageServiceOptions = {}): void {
    if (registeredInstances.has(monaco)) return;

    ensureBuiltInSubLanguages();

    registeredInstances.add(monaco);

    monaco.languages.register(languageExtensionPoint);
    monaco.languages.setLanguageConfiguration(languageId, languageConfiguration);
    applyTokensProvider(monaco);
    monaco.languages.registerCompletionItemProvider(languageId, createCompletionProvider(monaco, options));
    monaco.languages.registerHoverProvider(languageId, createHoverProvider(options));
    monaco.languages.registerDefinitionProvider(languageId, {
        provideDefinition(model, position) {
            const word = model.getWordAtPosition(position);
            if (!word) return [];
            const symbols = options.application?.(model);
            const reference = eventSourceReferenceAt(model.getLinesContent(), position.lineNumber - 1, word.startColumn, word.endColumn, symbols);
            const target = reference?.target;
            if (!target) return [];
            const current = symbols?.authoringPath ?? 'current.play';
            const source = target.location.path === current ? model.getValue() : symbols?.authoringDocuments?.find(document => document.path === target.location.path)?.source;
            const location = source && eventSourceIdentifier(target.location, target.name, source);
            if (!location) return [];
            // Foreign navigation requires an actual host model, not a guessed path or merged line.
            const targetUri = target.location.path && model.uri.path.endsWith(`/${current}`)
                ? model.uri.with({ path: model.uri.path.slice(0, -current.length) + target.location.path }) : undefined;
            const targetModels = targetUri ? monaco.editor.getModels().filter(candidate => candidate.uri.toString() === targetUri.toString()) : [];
            const targetModel = target.location.path === current ? model : targetModels.length === 1 ? targetModels[0] : undefined;
            return targetModel ? [{ uri: targetModel.uri, range: new monaco.Range(location.line, location.column, location.line, location.column + target.name.length) }] : [];
        }
    });
    monaco.languages.registerDocumentSemanticTokensProvider(languageId, {
        getLegend: () => ({ tokenTypes: [...responseTokenTypes], tokenModifiers: [] }),
        provideDocumentSemanticTokens(model) {
            const data: number[] = [];
            let previousLine = 0;
            let previousColumn = 0;
            for (const token of responseTokens(model.getLinesContent(), options.application?.(model))) {
                data.push(token.line - previousLine, token.line === previousLine ? token.column - previousColumn : token.column, token.length, token.type, 0);
                previousLine = token.line;
                previousColumn = token.column;
            }
            return { data: new Uint32Array(data) };
        },
        releaseDocumentSemanticTokens() {},
    });
    monaco.languages.registerInlayHintsProvider(languageId, createInlayHintsProvider(options));
    monaco.languages.registerCodeActionProvider(languageId, createCodeActionProvider());
    monaco.editor.defineTheme(screenplayDarkThemeName, screenplayDark);
    monaco.editor.defineTheme(screenplayLightThemeName, screenplayLight);
    attachDiagnostics(monaco);
}

export {
    causedByProperties,
    clauseKeywords,
    codeBlockTags,
    conceptAttributes,
    constructKeywords,
    contextRoots,
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
export { enclosingChain, fenceMap, firstWord, indentOf, withoutComment } from './document-context';
export {
    fileReferenceKeyword,
    fileReferenceOn,
    fileReferences,
    isAbsoluteFileReferencePath,
} from './file-references';
export type { FileReference } from './file-references';
export { fileImportOn, fileImports, importablePaths, isFileImportLine } from './file-imports';
export type { FileImport } from './file-imports';
export type { CompletionOptions } from './completions';
export { builtInTriggerNames, knownEventNames, knownTriggerNames, knownTypeNames, mergeSymbols, scanDocument } from './symbols';
export type {
    CommandSymbol,
    ConceptSymbol,
    DocumentSymbols,
    EventSymbol,
    ImportSymbol,
    NamedSymbol,
    PolicySymbol,
    PropertySymbol,
    ReadSymbol,
    QuerySymbol,
    TypeSymbol,
} from './symbols';
export { attributeDocs, contextVariableDocs, keywordDocs, specificationKeywordDocs } from './keyword-docs';
export { eventContextMemberAt, eventContextMembers, eventContextMembersAfter, eventContextPaths, namesEventContextMember } from './event-context';
export type { EventContextMember, EventContextPath } from './event-context';
export { contextVariableItems, producesItems, specificationStepItems } from './completion-items';
export type { CompletionEntry } from './completion-items';
export { completionEntriesFor, planCompletions } from './completion-planner';
export type { CompletionPlan } from './completion-planner';
export { responseTokens, responseTokenTypes } from './response-tokens';
export { responseCompletions } from './response-completions';
export { analyzeEventSources, eventSourceAvailability, eventSourceCompletions, eventSourceDetails, eventSourceHover, eventSourceIdentifier, eventSourceReferenceAt } from './event-source-authoring';
export type { EventSourceAnalysis, AuthoredEventSource, AuthoredStream, AuthoredCommandRoute } from './EventSourceAnalysis';
export { analyzeOperations, operationCompletions, operationDetails, operationAvailability, operationHover, operationReferenceAt, phaseState } from './operation-authoring';
export type { OperationAnalysis, OperationDeclaration, OperationInput, OperationPhase, OperationReference, SystemDeclaration } from './OperationAnalysis';
export { responseAvailability, responseAnalysis } from './response-analysis';
export { hoverContent } from './hover-content';
export { validateLines } from './validation';
export { destinationHints, productionDestinations } from './production-destinations';
export type { DestinationHint } from './DestinationHint';
export type { ProductionSymbol } from './ProductionSymbol';
export type { ValidationContext, ValidationIssue, ValidationSeverity } from './validation';
export { createCodeActionProvider } from './code-actions';
export { typeReferenceSymbol, typeReferenceText } from './TypeReferenceSymbol';
export type { TypeReferenceSymbol } from './TypeReferenceSymbol';
export { diagnosticCodes } from './diagnostic-codes';
export type { DiagnosticCode } from './diagnostic-codes';

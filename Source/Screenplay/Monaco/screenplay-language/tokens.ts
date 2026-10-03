// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { languages } from 'monaco-editor';
import {
    clauseKeywords,
    codeBlockTags,
    constructKeywords,
    primitiveTypes,
    sliceTypes,
} from './language';
import { MonarchTokenRules, SubLanguage } from './sub-language-registry';

// Maps a Screenplay inline code tag to the Monaco language id used for embedded highlighting.
// Declaration registration must not globally reserve existing property names.
// Contextual operation/system highlighting is deferred with the richer editor surface.
const contextualConstructs = new Set(['operation', 'system']);

const embeddedLanguages: Record<string, string> = {
    csharp: 'csharp',
    typescript: 'typescript',
    react: 'typescript',
    html: 'html',
    sql: 'sql',
};

function subLanguageState(keyword: string): string {
    return `subLanguage_${keyword}`;
}

// A construct keyword at the start of a line ends any indented sub-language block.
function subLanguageExitRule(subLanguages: SubLanguage[]): MonarchTokenRules[number] {
    const exitKeywords = [...constructKeywords.filter(keyword => !contextualConstructs.has(keyword)), ...subLanguages.map((subLanguage) => subLanguage.keyword)];
    return [
        new RegExp(`^\\s*(?:${exitKeywords.join('|')})\\b`),
        { token: '@rematch', next: '@pop' },
    ];
}

// Shared across the composite `.play` tokenizer and every standalone sub-language tokenizer
// (comments, `$`-prefixed context variables, strings, numbers, operators, delimiters, whitespace).
export const commonTokenRules: MonarchTokenRules = [
    [/\/\/.*$/, 'comment'],
    [/\$(?:context|eventContext|eventSourceId|causedBy)(?:\.\w+)*/, 'variable.predefined'],
    [/\$(?:env|secrets|strings)\.[\w.]+/, 'variable.predefined'],
    [/\$\.[\w.]*/, 'variable.predefined'],
    [/"(?:[^"\\]|\\.)*"/, 'string'],
    [/"(?:[^"\\]|\\.)*$/, 'string.invalid'],
    [/\d+(?:ms|s|m|h|d)\b/, 'number'],
    [/-?\d+\.\d+/, 'number.float'],
    [/-?\d+/, 'number'],
    [/=>|==|!=|>=|<=|[><=:?]/, 'operator'],
    [/[[\](),.]/, 'delimiter'],
    [/\s+/, 'white'],
];

export function createTokensProvider(subLanguages: SubLanguage[]): languages.IMonarchLanguage {
    const tokenizer: Record<string, MonarchTokenRules> = {
        root: [
            [/^(\s*)(handler)(?=\s*(?:\/\/.*)?$)/, ['white', { token: 'keyword', next: '@handlerBody.$1' }]],
            [/^(\s*@?[a-z_]\w*\s+)([\w.]+(?:\[\])?(?:\?|\s+optional)?)(\s+)(generated)(\s+identifier)?(?=\s*(?:\/\/.*)?$)/,
                ['identifier', 'type.identifier', 'white', 'keyword', 'keyword']],
            [/^(\s*)(generated)(\s+)([a-z_]\w*)(\s*=(?!=|>))/, ['white', 'keyword', 'white', 'identifier', 'operator']],
            [/^(\s*)(then)(\s+)(returns)\b/, ['white', 'keyword', 'white', 'keyword']],
            // Only unambiguous response headers: two-token property declarations keep their names.
            [/^(\s*)(returns)(?=\s*(?:\/\/.*)?$|\s+@\w+\s*(?:\/\/.*)?$)/, ['white', 'keyword']],
            // A modifier only after a complete type; names called optional remain ordinary names.
            [/^(\s*(?:by|filter)\s+[a-z_]\w*\s+)([\w.]+(?:\[\])?)(\s+)(optional)\b(?=\s*(?:from\b|\/\/|$))/,
                ['identifier', 'type.identifier', 'white', 'keyword']],
            // Query prefixes cannot fall back to being property names.
            [/^(?!\s*(?:by|filter)\s+)(\s*@?[a-z_]\w*\s+)([\w.]+(?:\[\])?)(\s+)(optional)\b(?=\s*(?:identifier\b|from\b|=(?!=|>)|\/\/|$))/,
                ['identifier', 'type.identifier', 'white', 'keyword']],
            [/^(\s*query\s+[A-Za-z_]\w*\s*=>\s*(?!observable\s+optional\s*(?:\/\/.*)?$)(?:observable\s+)?)([\w.]+(?:\[\])?)(\s+)(optional)\b(?=\s*(?:\/\/.*)?$)/,
                ['keyword', 'type.identifier', 'white', 'keyword']],
            // A specification's clock, trigger, capture and query steps - matched before a sub-language keyword
            // can claim 'capture' and read the rest of the specification as change data capture.
            [/^(\s*)(given|when)(\s+)(clock|capture|trigger|query)\b/, ['white', 'keyword', 'white', 'keyword']],
            [/^(\s*)(then)(\s+)(result|no\s+result)\b/, ['white', 'keyword', 'white', 'keyword']],
            // A quoted import names .play files rather than a qualified name - the path reads as a link.
            [/^(\s*)(import)(\s+)("[^"\\]*")/, ['white', 'keyword', 'white', 'string.link']],
            // Retain the header indent so event metadata stops at the enclosing block boundary.
            [/^(\s*)((?:produces\s+)?event)(\s+)([A-Za-z_]\w*)(\s+)(generation)(\s+)(\d+)(?=\s*(?:\/\/.*)?$)/,
                ['white', 'keyword', 'white', 'type.identifier', 'white', 'keyword', 'white', { token: 'number', next: '@eventBody.$1' }]],
            [/^(\s*)((?:produces\s+)?event)(\s+)([A-Za-z_]\w*)(?=\s*(?:\/\/.*)?$)/,
                ['white', 'keyword', 'white', { token: 'type.identifier', next: '@eventBody.$1' }]],
            // A tagged opening fence carries the embedded language; legacy tag lines still highlight.
            ...codeBlockTags.map(
                (tag): MonarchTokenRules[number] => [
                    new RegExp(`^\\s*\\x60\\x60\\x60${tag}\\s*$`),
                    { token: 'string.quote', next: `@codeBlock.${embeddedLanguages[tag]}`, nextEmbedded: embeddedLanguages[tag] },
                ],
            ),
            // Inline code block tags at end of line open an embedded code block.
            ...codeBlockTags.map(
                (tag): MonarchTokenRules[number] => [
                    new RegExp(`\\b${tag}\\b(?=\\s*$)`),
                    { token: 'keyword.tag', next: `@codeBlockPending.${embeddedLanguages[tag]}` },
                ],
            ),
            // Sub-language constructs switch to the registered sub-language's token rules.
            ...subLanguages.map(
                (subLanguage): MonarchTokenRules[number] => [
                    new RegExp(`\\b${subLanguage.keyword}\\b`),
                    { token: 'keyword', next: `@${subLanguageState(subLanguage.keyword)}` },
                ],
            ),
            // A bare description keyword at end of line opens a fenced plain-text block.
            [/\bdescription\b(?=\s*$)/, { token: 'keyword', next: '@descriptionBlockPending' }],
            [/\brow-click\b/, 'keyword'],
            // A leading @ escapes a name that collides with a directive keyword - it is a name, not an attribute.
            [/^\s*@[a-z_]\w*/, 'identifier'],
            [/@\w+/, 'annotation'],
            [
                /[A-Z]\w*/,
                {
                    cases: {
                        '@sliceTypes': 'keyword.type',
                        '@primitiveTypes': 'type',
                        '@default': 'type.identifier',
                    },
                },
            ],
            [
                /[a-z_]\w*/,
                {
                    cases: {
                        '@keywords': 'keyword',
                        '@default': 'identifier',
                    },
                },
            ],
            { include: '@common' },
        ],

        handlerBody: [
            [/^(?!$S2[ \t]+|\s*$)/, { token: '@rematch', next: '@pop' }],
            [/^(\s*)(implementation)(?=\s*(?:\/\/.*)?$)/, ['white', { token: 'keyword', next: '@implementationBody.$1' }]],
            { include: '@root' },
        ],

        implementationBody: [
            [/^(?!$S2[ \t]+|\s*$)/, { token: '@rematch', next: '@pop' }],
            [/^(\s*)(hint)(?=\s+")/, ['white', 'keyword']],
            { include: '@root' },
        ],

        eventBody: [
            [/^(?!$S2[ \t]+|\s*$)/, { token: '@rematch', next: '@pop' }],
            [/^(\s*)(id)(?=\s+")/, ['white', 'keyword']],
            [/^(\s*)(documentation)(?=\s*(?:\/\/.*)?$)/, ['white', { token: 'keyword', next: '@descriptionBlockPending' }]],
            { include: '@root' },
        ],

        common: commonTokenRules,

        // After a code tag, the only thing allowed before the opening fence is whitespace.
        codeBlockPending: [
            [
                /^\s*```\s*$/,
                { token: 'string.quote', switchTo: '@codeBlock.$S2', nextEmbedded: '$S2' },
            ],
            [/^\s*[^\s`].*$/, { token: '@rematch', next: '@pop' }],
            [/\s+/, 'white'],
        ],

        codeBlock: [
            [/^\s*```\s*$/, { token: 'string.quote', next: '@pop', nextEmbedded: '@pop' }],
            [/[^`]+/, ''],
            [/`/, ''],
        ],

        // After a bare description, the only thing allowed before the opening fence is whitespace.
        descriptionBlockPending: [
            [/\/\/.*$/, 'comment'],
            [/^\s*```(?:text|markdown)?\s*$/, { token: 'string.quote', switchTo: '@descriptionBlock' }],
            [/^\s*[^\s`].*$/, { token: '@rematch', next: '@pop' }],
            [/\s+/, 'white'],
        ],

        descriptionBlock: [
            [/^\s*```\s*$/, { token: 'string.quote', next: '@pop' }],
            [/.+/, 'string'],
        ],
    };

    for (const subLanguage of subLanguages) {
        tokenizer[subLanguageState(subLanguage.keyword)] = [
            subLanguageExitRule(subLanguages),
            ...subLanguage.tokens,
            { include: '@common' },
            [/[A-Z]\w*/, 'type.identifier'],
            [/[a-z_]\w*/, 'identifier'],
        ];
    }

    return {
        defaultToken: '',
        tokenPostfix: '.play',
        ignoreCase: false,
        keywords: [...constructKeywords.filter(keyword => !contextualConstructs.has(keyword)), ...clauseKeywords, ...codeBlockTags],
        sliceTypes,
        primitiveTypes,
        tokenizer,
    } as languages.IMonarchLanguage;
}

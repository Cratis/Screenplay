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
const embeddedLanguages: Record<string, string> = {
    csharp: 'csharp',
    typescript: 'typescript',
    react: 'typescript',
    html: 'html',
};

function subLanguageState(keyword: string): string {
    return `subLanguage_${keyword}`;
}

// A construct keyword at the start of a line ends any indented sub-language block.
function subLanguageExitRule(subLanguages: SubLanguage[]): MonarchTokenRules[number] {
    const exitKeywords = [...constructKeywords, ...subLanguages.map((subLanguage) => subLanguage.keyword)];
    return [
        new RegExp(`^\\s*(?:${exitKeywords.join('|')})\\b`),
        { token: '@rematch', next: '@pop' },
    ];
}

export function createTokensProvider(subLanguages: SubLanguage[]): languages.IMonarchLanguage {
    const tokenizer: Record<string, MonarchTokenRules> = {
        root: [
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

        common: [
            [/\/\/.*$/, 'comment'],
            [/\$(?:context|eventContext)(?:\.\w+)*/, 'variable.predefined'],
            [/\$(?:env|secrets|strings)\.[\w.]+/, 'variable.predefined'],
            [/\$\.[\w.]*/, 'variable.predefined'],
            [/"[^"]*"/, 'string'],
            [/"[^"]*$/, 'string.invalid'],
            [/\d+(?:ms|s|m|h|d)\b/, 'number'],
            [/-?\d+\.\d+/, 'number.float'],
            [/-?\d+/, 'number'],
            [/=>|==|!=|>=|<=|[><=:?]/, 'operator'],
            [/[[\](),.]/, 'delimiter'],
            [/\s+/, 'white'],
        ],

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
            [/^\s*```\s*$/, { token: 'string.quote', switchTo: '@descriptionBlock' }],
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
        keywords: [...constructKeywords, ...clauseKeywords, ...codeBlockTags],
        sliceTypes,
        primitiveTypes,
        tokenizer,
    } as languages.IMonarchLanguage;
}

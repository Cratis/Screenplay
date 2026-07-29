// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { languages } from 'monaco-editor';

export type Monaco = typeof import('monaco-editor');

// Capture Declaration Language (CDL) — Chronicle's DSL for declaring change-data-capture
// rules, registered here as its own standalone top-level Monaco language. This is distinct
// from the lightweight CDL sub-language embedded inside a `.play` document's `capture`
// blocks (../cdl.ts) — this module owns its own language id, tokenizer, and providers.
export const languageId = 'cdl';

// `route`/`poll` are API source properties; `path` is the webhook inbound path; `topic`
// is the message source subject. There is no `auth`/`bearer`/`url` — an API source
// references a named External Service (`api <ServiceName>`) for base URL and auth, and
// a webhook source's authentication is configured on the source builder, not in CDL text.
const KEYWORDS = [
    'capture',
    'source',
    'key',
    'map',
    'append',
    'when',
    'nested',
    'children',
    'identified',
    'by',
    'api',
    'webhook',
    'message',
    'route',
    'poll',
    'path',
    'topic',
    'from',
    'to',
    'or',
    'and',
    'added',
    'removed',
    'translate',
    'split',
] as const;

const BUILTINS = [
    '$context',
    '$previous',
    '$env',
] as const;

const OPERATORS = ['=', '=>', '.'];

export const languageConfiguration: languages.LanguageConfiguration = {
    comments: {
        lineComment: '#',
    },
    brackets: [
        ['{', '}'],
        ['[', ']'],
        ['(', ')'],
    ],
    autoClosingPairs: [
        { open: '{', close: '}' },
        { open: '[', close: ']' },
        { open: '(', close: ')' },
        { open: '"', close: '"' },
        { open: "'", close: "'" },
        { open: '`', close: '`' },
    ],
    surroundingPairs: [
        { open: '{', close: '}' },
        { open: '[', close: ']' },
        { open: '(', close: ')' },
        { open: '"', close: '"' },
        { open: "'", close: "'" },
        { open: '`', close: '`' },
    ],
    indentationRules: {
        increaseIndentPattern: /^.*(\bcapture\b\s+\S+|\bsource\b(?:\s+\w+)?|\bmap\b|\bappend\b\s+\S+|\bnested\b\s+\S+|\bchildren\b\s+\S+.*|\btranslate\b|\bsplit\b.*)\s*$/,
        decreaseIndentPattern: /^.*\}.*$/,
    },
    folding: {
        offSide: true,
        markers: {
            start: /^\s*(capture|source|map|append|nested|children|translate|split)/,
            end: /^\s*$/,
        },
    },
    wordPattern: /[$]?[a-zA-Z_][\w$-]*/,
};

export const monarchLanguage: languages.IMonarchLanguage = {
    defaultToken: '',
    tokenPostfix: '.cdl',

    keywords: KEYWORDS,
    builtins: BUILTINS,
    operators: OPERATORS,

    symbols: /[=><!~?:&|+\-*/^%]+/,
    escapes: /\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,

    tokenizer: {
        root: [
            { include: '@whitespace' },
            [/`/, { token: 'string.template', next: '@templateString' }],
            [/\$\.(?:[a-zA-Z_]\w*(?:\.[a-zA-Z_]\w*)*)?/, 'variable'],
            [/\$previous(?:\.[a-zA-Z_]\w*(?:\.[a-zA-Z_]\w*)*)?/, 'variable.predefined'],
            [/\$context(?:\.(?:occurred|eventSourceId))?/, 'variable.predefined'],
            [/\$env(?:\.[A-Z_][A-Z0-9_]*)?/, 'variable.predefined'],
            [/@[a-zA-Z_$][\w$]*/, 'identifier.escape'],
            [
                /[A-Z][a-zA-Z_$][\w$]*/,
                {
                    cases: {
                        '@keywords': 'invalid',
                        '@default': 'identifier',
                    },
                },
            ],
            [
                /[a-z_$][\w$]*/,
                {
                    cases: {
                        '@keywords': 'keyword',
                        '@builtins': 'variable.predefined',
                        '@default': 'identifier',
                    },
                },
            ],
            [/[{}()[\]]/, '@brackets'],
            [/=>/, 'operator.arrow'],
            [
                /@symbols/,
                {
                    cases: {
                        '@operators': 'operator',
                        '@default': '',
                    },
                },
            ],
            [/\d*\.\d+([eE][-+]?\d+)?/, 'number.float'],
            [/0[xX][0-9a-fA-F]+/, 'number.hex'],
            [/\d+/, 'number'],
            [/[;,.]/, 'delimiter'],
            [/"([^"\\]|\\.)*$/, 'string.invalid'],
            [/"/, { token: 'string.quote', next: '@string' }],
        ],

        whitespace: [
            [/[ \t\r\n]+/, ''],
            [/#.*$/, 'comment'],
        ],

        templateString: [
            [/\$\{/, { token: 'delimiter.bracket', next: '@templateExpression' }],
            [/[^\\`$]+/, 'string.template'],
            [/@escapes/, 'string.template.escape'],
            [/\\./, 'string.template.escape.invalid'],
            [/`/, { token: 'string.template', next: '@pop' }],
        ],

        templateExpression: [
            [/\}/, { token: 'delimiter.bracket', next: '@pop' }],
            { include: '@root' },
        ],

        string: [
            [/[^\\"]+/, 'string'],
            [/@escapes/, 'string.escape'],
            [/\\./, 'string.escape.invalid'],
            [/"/, { token: 'string.quote', next: '@pop' }],
        ],
    },
};

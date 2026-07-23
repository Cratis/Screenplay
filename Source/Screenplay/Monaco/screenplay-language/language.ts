// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { languages } from 'monaco-editor';

export type Monaco = typeof import('monaco-editor');

export const languageId = 'screenplay';

export const languageExtensionPoint: languages.ILanguageExtensionPoint = {
    id: languageId,
    extensions: ['.play'],
    aliases: ['Screenplay', 'screenplay', 'play'],
    mimetypes: ['text/x-screenplay'],
};

export const constructKeywords = [
    'domain',
    'import',
    'concept',
    'policy',
    'persona',
    'authentication',
    'module',
    'layout',
    'feature',
    'slice',
    'event',
    'command',
    'query',
    'reactor',
    'screen',
    'constraint',
    'seed',
];

export const clauseKeywords = [
    'description',
    'template',
    'require',
    'authenticated',
    'role',
    'claim',
    'subject',
    'authorize',
    'validate',
    'produces',
    'handler',
    'when',
    'message',
    'not',
    'empty',
    'matches',
    'all',
    'max',
    'min',
    'length',
    'today',
    'true',
    'false',
    'and',
    'or',
    'by',
    'filter',
    'data',
    'via',
    'action',
    'navigate',
    'to',
    'label',
    'section',
    'table',
    'summary',
    'title',
    'column',
    'field',
    'on',
    'unique',
    'file',
    'concurrency',
    'eventSource',
    'sourceType',
    'streamType',
    'streamId',
    'tag',
    'for',
    'readmodel',
    'provider',
];

export const codeBlockTags = ['csharp', 'typescript', 'react', 'html'];

export const sliceTypes = ['StateChange', 'StateView', 'Automation', 'Translate'];

export const primitiveTypes = ['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime'];

export const conceptAttributes = ['@pii', '@sensitive'];

export const languageConfiguration: languages.LanguageConfiguration = {
    comments: {
        lineComment: '//',
    },
    brackets: [
        ['(', ')'],
        ['[', ']'],
    ],
    autoClosingPairs: [
        { open: '(', close: ')' },
        { open: '[', close: ']' },
        { open: '"', close: '"' },
    ],
    surroundingPairs: [
        { open: '(', close: ')' },
        { open: '[', close: ']' },
        { open: '"', close: '"' },
    ],
    folding: {
        offSide: true,
    },
    indentationRules: {
        increaseIndentPattern:
            /^\s*(module|feature|slice|policy|persona|authentication|provider|event|command|screen|projection|capture|reactor|constraint|layout|template|validate|produces|handler|section|concurrency|seed|for)\b.*$/,
        // Dedents are always explicit in an offside language — never auto-dedent.
        decreaseIndentPattern: /(?!)/,
    },
};

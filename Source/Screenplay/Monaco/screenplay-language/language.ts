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
    'import',
    'concept',
    'policy',
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
];

export const clauseKeywords = [
    'template',
    'require',
    'authenticated',
    'role',
    'claim',
    'subject',
    'authorize',
    'validate',
    'produces',
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
            /^\s*(module|feature|slice|policy|event|command|screen|projection|capture|reactor|constraint|layout|template|validate|produces|section)\b.*$/,
        // Dedents are always explicit in an offside language — never auto-dedent.
        decreaseIndentPattern: /(?!)/,
    },
};

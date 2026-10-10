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

// Every word the parser dispatches on at the top level, inside a module, or inside a slice.
// `for_LanguageService.when_comparing_keywords_against_the_parsers` derives that set from
// ScreenplayParser and SliceParser and fails when this list has drifted from it, so a construct
// added to the compiler cannot silently lose its highlighting and completion.
export const constructKeywords = [
    'domain',
    'import',
    'concept',
    'type',
    'policy',
    'purpose',
    'persona',
    'authentication',
    'module',
    'layout',
    'theme',
    'ui',
    'feature',
    'slice',
    'event',
    'public',
    'system',
    'eventsource',
    'operation',
    'command',
    'query',
    'projection',
    'capture',
    'reaction',
    'reducer',
    'readmodel',
    'trigger',
    'screen',
    'dialog',
    'form',
    'contribute',
    'constraint',
    'specification',
    'case',
    'parameter',
    'example',
    'seed',
    'behavior',
    'exposure',
    'instance',
];

// Type modifiers are contextual, never excluded from property or declaration names.
export const typeModifierKeywords = ['optional'];

export const clauseKeywords = [
    'depends',
    'direction',
    'inbound',
    'outbound',
    'description',
    'documentation',
    'template',
    'profile',
    'require',
    'authenticated',
    'role',
    'claim',
    'subject',
    'authorize',
    'validate',
    'produces',
    'reads', // A clause in command bodies and reaction trigger bodies; @reads remains a value.
    'as',
    'handler',
    'performer',
    'observable',
    'identifier',
    'reason',
    'from',
    'when',
    'given',
    'then',
    'arguments',
    'result',
    'clock', // A specification step - 'given clock' and 'when clock' state an instant.
    'exactly',
    'error',
    'message',
    'not', // Unary policy negation, as well as 'not empty' validation.
    'empty',
    'rule',
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
    'component',
    'context',
    'property',
    'literal',
    'icons',
    'packages',
    'outlet',
    'display',
    'scopes',
    'reexposes',
    'operations',
    'items',
    'toolbar',
    'item',
    'presentation',
    'exposes',
    'mode',
    'oneWay',
    'twoWay',
    'null',
    'propagate',
    'clear',
    'preserve',
    'expected',
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
    'no',
    'when',
    'every',
    'at',
    'where',
    'invokes',
    'contains',
    'starts',
    'with',
    'provider',
    // Interaction: the words a behavior body uses. The built-in interaction kinds ('click', 'submit', and
    // friends) are only reserved inside an `on` clause, so they are clause words rather than constructs -
    // an identifier of the same name elsewhere in a document remains an identifier.
    'uses',
    'parameter',
    'order',
    'open',
    'close',
    'refresh',
    'set',
    'notify',
    'confirm',
    'raise',
    'interval',
    'success',
    'failure',
    'click',
    'double',
    'select',
    'submit',
    'change',
    'load',
    'unload',
    'enter',
    'leave',
    'info',
    'warning',
];

export const codeBlockTags = ['csharp', 'typescript', 'react', 'html', 'sql'];

// The roots a `$context.` path can start with — the members of CommandContext and QueryContext.
export const contextRoots = [
    'command',
    'arguments',
    'tenant',
    'causedBy',
    'causation',
    'occurred',
    'identity',
];

export const causedByProperties = ['subject', 'name', 'userName'];

// The properties a `$context.identity.` path can name — the members of Identity. Everything after
// `claims.` is the name of a claim rather than a member, so it is never checked.
export const identityProperties = ['id', 'name', 'userName', 'isAuthenticated', 'roles', 'claims'];

export const sliceTypes = ['StateChange', 'StateView', 'Automation', 'Translate'];

export const primitiveTypes = ['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime'];

export const conceptAttributes = ['pii', 'personal', 'secret'];

// The lines that open a block with a body of their own. A line that is complete as written - a `domain`, a
// `concept` or `query` with nothing under it, a `produces` of an existing event - is not one.
export const blockHeaderPattern = /^\s*(?:module|feature|slice\s+\w+|type|command|event|readmodel|reducer|projection|capture|reaction|screen|dialog|form|contribute|specification|case|constraint|persona|policy|behavior|layout|theme|ui\s+profile|trigger|authentication|seed|system|eventsource|operation|screen\s+template|dialog\s+template|produces\s+event|validate|handler|on)\b[^=]*$/;

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
    // A header that opens a block puts the cursor one level in on Enter, so what is typed - or suggested - starts at
    // the block's own indentation. IndentAction.Indent is 1.
    onEnterRules: [
        { beforeText: blockHeaderPattern, action: { indentAction: 1 as languages.IndentAction } },
    ],
    indentationRules: {
        increaseIndentPattern:
            /^\s*(module|feature|slice|policy|persona|authentication|provider|event|command|query|type|screen|projection|capture|reaction|trigger|constraint|specification|layout|template|validate|produces|handler|implementation(?=\s*$)|performer|rule|section|action|otherwise(?=\s*$|\s+execute\b)|concurrency|seed|for|when|then|arguments|result|every|at|invokes|on)\b.*$/,
        // Dedents are always explicit in an offside language — never auto-dedent.
        decreaseIndentPattern: /(?!)/,
    },
};

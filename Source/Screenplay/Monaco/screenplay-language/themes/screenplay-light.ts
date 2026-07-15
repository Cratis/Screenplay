// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor } from 'monaco-editor';

export const screenplayLightThemeName = 'screenplay-light';

export const screenplayLight: editor.IStandaloneThemeData = {
    base: 'vs',
    inherit: true,
    rules: [
        { token: 'keyword.play', foreground: 'AF00DB' },
        { token: 'keyword.tag.play', foreground: '0000FF', fontStyle: 'italic' },
        { token: 'keyword.type.play', foreground: '0070C1', fontStyle: 'bold' },
        { token: 'type.play', foreground: '267F99' },
        { token: 'type.identifier.play', foreground: '267F99' },
        { token: 'identifier.play', foreground: '001080' },
        { token: 'annotation.play', foreground: '795E26' },
        { token: 'variable.predefined.play', foreground: 'B45309' },
        { token: 'string.play', foreground: 'A31515' },
        { token: 'string.invalid.play', foreground: 'CD3131' },
        { token: 'string.quote.play', foreground: '008000' },
        { token: 'number.play', foreground: '098658' },
        { token: 'number.float.play', foreground: '098658' },
        { token: 'comment.play', foreground: '008000', fontStyle: 'italic' },
        { token: 'operator.play', foreground: '000000' },
        { token: 'delimiter.play', foreground: '000000' },
    ],
    colors: {
        'editor.background': '#FFFFFF',
    },
};

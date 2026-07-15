// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor } from 'monaco-editor';

export const screenplayDarkThemeName = 'screenplay-dark';

export const screenplayDark: editor.IStandaloneThemeData = {
    base: 'vs-dark',
    inherit: true,
    rules: [
        { token: 'keyword.play', foreground: 'C586C0' },
        { token: 'keyword.tag.play', foreground: '569CD6', fontStyle: 'italic' },
        { token: 'keyword.type.play', foreground: '4FC1FF', fontStyle: 'bold' },
        { token: 'type.play', foreground: '4EC9B0' },
        { token: 'type.identifier.play', foreground: '4EC9B0' },
        { token: 'identifier.play', foreground: '9CDCFE' },
        { token: 'annotation.play', foreground: 'DCDCAA' },
        { token: 'variable.predefined.play', foreground: 'D7BA7D' },
        { token: 'string.play', foreground: 'CE9178' },
        { token: 'string.invalid.play', foreground: 'F44747' },
        { token: 'string.quote.play', foreground: '6A9955' },
        { token: 'number.play', foreground: 'B5CEA8' },
        { token: 'number.float.play', foreground: 'B5CEA8' },
        { token: 'comment.play', foreground: '6A9955', fontStyle: 'italic' },
        { token: 'operator.play', foreground: 'D4D4D4' },
        { token: 'delimiter.play', foreground: 'CCCCCC' },
    ],
    colors: {
        'editor.background': '#1E1E1E',
    },
};

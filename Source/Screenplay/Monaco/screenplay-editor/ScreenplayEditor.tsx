// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Editor } from '@monaco-editor/react';
import { languageId } from '@cratis/screenplay-language';

export interface ScreenplayEditorProps {
    value: string;
    theme: string;
    onChange(value: string): void;
}

export const ScreenplayEditor = ({ value, theme, onChange }: ScreenplayEditorProps) => (
    <Editor
        height='100%'
        language={languageId}
        theme={theme}
        value={value}
        onChange={(changed) => onChange(changed ?? '')}
        options={{
            fontSize: 14,
            tabSize: 2,
            insertSpaces: true,
            minimap: { enabled: true },
            automaticLayout: true,
            wordBasedSuggestions: 'off',
            scrollBeyondLastLine: false,
            fixedOverflowWidgets: true,
        }}
    />
);

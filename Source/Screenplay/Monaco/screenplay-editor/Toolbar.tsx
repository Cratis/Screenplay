// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChangeEvent, useRef } from 'react';

export interface ToolbarProps {
    fileName: string;
    isDark: boolean;
    onNew(): void;
    onOpen(name: string, content: string): void;
    onSave(): void;
    onToggleTheme(): void;
}

export const Toolbar = (props: ToolbarProps) => {
    const fileInput = useRef<HTMLInputElement>(null);

    const filePicked = async (event: ChangeEvent<HTMLInputElement>) => {
        const picked = event.target.files?.[0];
        if (!picked) return;
        props.onOpen(picked.name, await picked.text());
        event.target.value = '';
    };

    return (
        <header className='toolbar'>
            <span className='toolbar__brand'>Screenplay</span>
            <button type='button' onClick={props.onNew}>New</button>
            <button type='button' onClick={() => fileInput.current?.click()}>Open…</button>
            <button type='button' onClick={props.onSave}>Save</button>
            <span className='toolbar__file-name'>{props.fileName}</span>
            <span className='toolbar__spacer' />
            <button type='button' onClick={props.onToggleTheme}>
                {props.isDark ? 'Light theme' : 'Dark theme'}
            </button>
            <input
                ref={fileInput}
                type='file'
                accept='.play'
                hidden
                onChange={filePicked}
            />
        </header>
    );
};

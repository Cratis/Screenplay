// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import {
    screenplayDarkThemeName,
    screenplayLightThemeName,
} from '@cratis/screenplay-language';
import { ScreenplayEditor } from './ScreenplayEditor';
import { Toolbar } from './Toolbar';
import invoicingSample from './samples/invoicing.play?raw';

interface ScreenplayFile {
    name: string;
    content: string;
}

export const App = () => {
    const [file, setFile] = useState<ScreenplayFile>({
        name: 'invoicing.play',
        content: invoicingSample,
    });
    const [isDark, setIsDark] = useState(true);

    const newFile = () => {
        if (file.content.length > 0 && !window.confirm('Discard the current document?')) return;
        setFile({ name: 'untitled.play', content: '' });
    };

    const openFile = (name: string, content: string) => setFile({ name, content });

    const saveFile = () => {
        const blob = new Blob([file.content], { type: 'text/plain' });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = file.name;
        anchor.click();
        URL.revokeObjectURL(url);
    };

    return (
        <div className={`app ${isDark ? 'app--dark' : 'app--light'}`}>
            <Toolbar
                fileName={file.name}
                isDark={isDark}
                onNew={newFile}
                onOpen={openFile}
                onSave={saveFile}
                onToggleTheme={() => setIsDark((current) => !current)}
            />
            <main className='app__editor'>
                <ScreenplayEditor
                    value={file.content}
                    theme={isDark ? screenplayDarkThemeName : screenplayLightThemeName}
                    onChange={(content) => setFile((current) => ({ ...current, content }))}
                />
            </main>
        </div>
    );
};

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import type { editor, languages, Position } from 'monaco-editor';
import { HoverProvider } from '../sub-languages/projection/HoverProvider';
import { eventContextMembers } from '../event-context';

const line = '    type = $eventContext.eventType.id';

function hover(word: string): string {
    const startColumn = line.lastIndexOf(word) + 1;
    const lines = ['projection Statistics => StatisticsReadModel', '  all', line];
    const model = {
        getLineContent: (lineNumber: number) => lines[lineNumber - 1],
        getWordAtPosition: () => ({ word, startColumn, endColumn: startColumn + word.length }),
    } as unknown as editor.ITextModel;
    const result = new HoverProvider().provideHover(model, { lineNumber: 3, column: startColumn } as Position) as languages.Hover;
    return (result.contents[0] as { value: string }).value;
}

describe('when hovering the event context', () => {
    let content: string;

    beforeEach(() => {
        content = hover('$eventContext');
    });

    it('should list every member of the catalog', () => {
        eventContextMembers.every((member) => content.includes(`\`${member.name}\``)).should.be.true;
    });
});

describe('when hovering a member of an event context path that is also a keyword', () => {
    let content: string;

    beforeEach(() => {
        content = hover('id');
    });

    it('should describe the member rather than the keyword', () => {
        content.should.include('$eventContext.eventType.id');
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import type { editor, languages, Position } from 'monaco-editor';
import { CompletionProvider } from '../sub-languages/projection/CompletionProvider';
import { eventContextMembers } from '../event-context';

function complete(line: string): string[] {
    const lines = ['projection Statistics => StatisticsReadModel', '  all', line];
    const model = {
        getLineContent: (lineNumber: number) => lines[lineNumber - 1],
        getWordAtPosition: () => null,
    } as unknown as editor.ITextModel;
    const position = { lineNumber: 3, column: line.length + 1 } as Position;
    const result = new CompletionProvider().provideCompletionItems(model, position) as languages.CompletionList;
    return result.suggestions.map((suggestion) => suggestion.label as string);
}

describe('when completing the first segment of an event context path', () => {
    let labels: string[];

    beforeEach(() => {
        labels = complete('    lastSeen = $eventContext.');
    });

    it('should offer every member of the catalog', () => {
        labels.should.have.members(eventContextMembers.map((member) => member.name));
    });

    it('should offer the event store', () => {
        labels.should.include('eventStore');
    });

    it('should offer the namespace', () => {
        labels.should.include('namespace');
    });

    it('should not offer a causation identifier', () => {
        labels.should.not.include('causationId');
    });
});

describe('when completing below a composite member of an event context path', () => {
    let labels: string[];

    beforeEach(() => {
        labels = complete('    by = $eventContext.causedBy.');
    });

    it('should offer the members of the identity', () => {
        labels.should.have.members(['subject', 'name', 'userName', 'onBehalfOf']);
    });
});

describe('when completing a dynamic dictionary key', () => {
    let labels: string[];

    beforeEach(() => {
        labels = complete('    count eventCountByType.$eventContext.eventType.');
    });

    it('should offer the members of the event type', () => {
        labels.should.have.members(['id', 'generation', 'tombstone']);
    });
});

describe('when completing below a collection of the event context', () => {
    let labels: string[];

    beforeEach(() => {
        labels = complete('    x = $eventContext.causation.');
    });

    it('should offer nothing', () => {
        labels.should.be.empty;
    });
});

describe('when completing the week of when the event occurred', () => {
    let labels: string[];

    beforeEach(() => {
        labels = complete('    week = $eventContext.occurred.');
    });

    it('should offer the derived week', () => {
        labels.should.have.members(['Week']);
    });
});

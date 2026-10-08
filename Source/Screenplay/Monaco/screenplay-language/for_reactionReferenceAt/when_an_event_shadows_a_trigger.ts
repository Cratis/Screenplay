// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { hoverContent } from '../hover-content';
import { reactionReferenceAt } from '../reaction-authoring';
import { mergeSymbols, scanDocument } from '../symbols';

const reaction = 'module M\n  feature F\n    slice Automation Consumer\n      reaction React\n        when Created';
const declared = 'trigger Created\n  triggerValue String';
const event = 'module M\n  feature F\n    slice StateChange Owner\n      event Created\n        eventValue Uuid';
const context = { ...mergeSymbols(), authoringPath: 'reaction.play', authoringDocuments: [{ path: 'trigger.play', source: declared }, { path: 'event.play', source: event }] };

describe('when resolving a reaction occurrence in both editors', () => {
    it('should navigate to the event rather than the declared trigger across documents', () => {
        const reference = reactionReferenceAt(reaction.split('\n'), 4, 14, 21, context);
        expect(reference?.target?.location).toEqual({ path: 'event.play', line: 4, column: 13 });
        expect(reference?.content).toContain('eventValue Uuid');
        expect(reference?.content).not.toContain('triggerValue');
    });
    it('should hover the same event through the shared hover provider', () => {
        expect(hoverContent(reaction.split('\n'), 4, 'Created', 14, 21, context)).toContain('eventValue Uuid');
    });
    it('should not navigate to the trigger when an imported event has no physical declaration', () => {
        const imported = { ...context, authoringDocuments: [{ path: 'trigger.play', source: declared }, { path: 'imports.play', source: 'import Outside.Created' }] };
        const reference = reactionReferenceAt(reaction.split('\n'), 4, 14, 21, imported);
        expect(reference?.target).toBeNull();
        expect(hoverContent(reaction.split('\n'), 4, 'Created', 14, 21, imported)).toContain('import Outside.Created');
    });
    it('should retain declared trigger navigation when no event or import shadows it', () => {
        const reference = reactionReferenceAt(reaction.split('\n'), 4, 14, 21, { ...context, authoringDocuments: [{ path: 'trigger.play', source: declared }] });
        expect(reference?.target?.location).toEqual({ path: 'trigger.play', line: 1, column: 9 });
        expect(reference?.content).toContain('triggerValue String');
    });
    it('should replace stale current-document events with the unsaved buffer', () => {
        const current = declared + '\n' + reaction;
        const reference = reactionReferenceAt(current.split('\n'), 6, 14, 21, { ...context, authoringPath: 'event.play', authoringDocuments: [{ path: 'event.play', source: event }] });
        expect(reference?.content).toContain('triggerValue String');
        expect(reference?.target?.location.path).toBe('event.play');
    });
    it('should not bind a local trigger when a host supplies only remote event symbols', () => {
        const lines = (declared + '\n' + reaction).split('\n');
        const symbols = scanDocument(event.split('\n'));
        const reference = reactionReferenceAt(lines, 6, 14, 21, symbols);
        expect(reference?.target).toBeNull();
        expect(reference?.content).toContain('eventValue Uuid');
        expect(reference?.content).not.toContain('triggerValue');
    });
    it('should not guess between duplicate events', () => {
        expect(reactionReferenceAt(reaction.split('\n'), 4, 14, 21, { ...context, authoringDocuments: [...context.authoringDocuments, { path: 'other.play', source: event }] })?.target).toBeNull();
    });
    it('should ignore specifications, comments and native fences', () => {
        expect(reactionReferenceAt(['specification S', '  when Created'], 1, 8, 15, context)).toBeNull();
        expect(reactionReferenceAt((reaction + ' // Created').split('\n'), 4, 25, 32, context)).toBeNull();
        expect(reactionReferenceAt(['reaction R', '  ```csharp', '  when Created', '  ```'], 2, 8, 15, context)).toBeNull();
    });
});

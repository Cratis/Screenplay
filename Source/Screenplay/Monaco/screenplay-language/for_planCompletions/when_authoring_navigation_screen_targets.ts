// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { completionEntriesFor } from '../completion-planner';
import { keywordDocs } from '../keyword-docs';

const selected = (stack: string[], headers: string[] = []) => completionEntriesFor(stack, { headers, document: [] }).filter(item => item.label.includes('(with screen)'));

describe('when authoring navigation screen targets', () => {
    it('should offer screen selection inside a navigation block', () => expect(selected(['navigation', 'layout'])[0].insertText).toContain('target ${3:Module.Feature} screen ${4:Screen}'));
    it('should offer a selected default in a declared outlet', () => expect(selected(['outlet', 'layout'], ['outlet sidebar of navigation'])[0].insertText).toContain('default ${1:id}'));
    it('should offer a selected item in an outlet change', () => expect(selected(['outlet', 'module'])[0].insertText).toContain('screen ${4:Screen}'));
    it('should explain fallback and the renderer requirement', () => {
        expect(keywordDocs.item).toContain('consumer default');
        expect(keywordDocs.item).toContain('Rendering requires consumer support');
    });
});

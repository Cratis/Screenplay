// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { validateItems } from '../completion-items';
import { hoverContent } from '../hover-content';

const lines = ['command RegisterName', '  name String', '  validate', '    name not empty'];

describe('when describing text validation', () => {
    it('should describe non-blank text and collection presence in hover', () => {
        const content = hoverContent(lines, 3, 'empty', 14, 19)!;
        expect(content).toContain('non-whitespace character');
        expect(content).toContain('Collections need at least one element');
        expect(content).toContain('Identity text is unchanged');
    });
    it('should pin the Unicode whitespace set instead of platform trimming', () => {
        const content = hoverContent(lines, 3, 'empty', 14, 19)!;
        for (const range of ['U+0009–U+000D', 'U+0020', 'U+0085', 'U+00A0', 'U+1680', 'U+2000–U+200A', 'U+2028', 'U+2029', 'U+202F', 'U+205F', 'U+3000', 'not U+FEFF']) {
            expect(content).toContain(range);
        }
    });
    it('should teach the same rule in completion', () => {
        const content = validateItems.find(item => item.label === 'not empty')!.documentation;
        expect(content).toContain('non-whitespace character');
        expect(content).toContain('including U+0085 but not U+FEFF');
        expect(content).toContain('not JavaScript trim()');
        expect(content).toContain('every ESM version');
    });
});

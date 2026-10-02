// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, vi } from 'vitest';
import * as context from '../document-context';
import { scanDocument } from '../symbols';
import { validateLines } from '../validation';

function commands(count: number): string[] {
    return ['module Projects', '  feature Registration', ...Array.from({ length: count }, (_, index) => [
        `    slice StateChange Register${index}`,
        `      command Register${index} // command`,
        '        description Uuid identifier // keyword-named property',
        '        details String',
        '        produces Registered',
        '          nested Uuid identifier',
        '  // a comment is not an ancestor',
        '        handler',
        '          ```csharp',
        '          ghost Uuid identifier',
        '          ```',
        '        name String',
    ]).flat()];
}

describe('when scanning many commands', () => {
    it('should visit structural indentation a linear number of times', () => {
        const visits = vi.spyOn(context, 'indentOf');
        try {
            const small = commands(150);
            validateLines(small);
            const smallCount = visits.mock.calls.length;
            visits.mockClear();
            const large = commands(300);
            validateLines(large);
            const largeCount = visits.mock.calls.length;
            const symbols = scanDocument(large);
            smallCount.should.be.greaterThan(0);
            largeCount.should.be.lessThan(large.length * 10);
            largeCount.should.be.lessThan(smallCount * 2.1);
            symbols.commands.length.should.equal(300);
            symbols.commands.every(command => command.properties.map(property => property.name).join(',') === 'description,details,name').should.equal(true);
        } finally {
            visits.mockRestore();
        }
    });
});

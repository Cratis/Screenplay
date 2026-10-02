// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, expect } from 'vitest';
import { WorkspaceApplication } from '../WorkspaceApplication';

const source = 'module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        name String\n        produces event Renamed\n          name String = name\n';

describe('when indexing inline events', () => {
    it('should find their declarations for navigation and other files', () => {
        const application = new WorkspaceApplication();
        application.set('rename.play', source);
        expect(application.eventDeclarations('Renamed')).toEqual([{ path: 'rename.play', line: 6 }]);
        expect(application.symbolsExcept('other.play').events[0].properties[0].name).toBe('name');
    });
    it('should expose the compiler diagnostic for a malformed typed mapping', () => {
        const application = new WorkspaceApplication();
        application.set('rename.play', source.replace('name String = name', 'name = name'));
        expect(application.diagnosticsFor('rename.play').map(diagnostic => diagnostic.code)).toContain('PLAY0044');
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { WorkspaceApplication } from '../WorkspaceApplication';

describe('when looking up a declaration', () => {
    const application = new WorkspaceApplication();
    application.set('application.play', 'domain Demo');
    application.set('shared/concepts.play', 'concept TalkId : Uuid\n\nconcept Track : Enum\n  ai = 0');
    application.set('shared/access.play', 'policy IsSpeaker\n  require authenticated');
    application.set('callforpapers/review/review.play', 'slice StateView ReviewBoard\n  screen ReviewBoard\n    template ReviewDesk');

    it('finds a concept in another file', () => expect(application.declarationsOf('TalkId')).toEqual([{ path: 'shared/concepts.play', line: 0, column: 8, keyword: 'concept' }]));
    it('finds a policy in another file', () => expect(application.declarationsOf('IsSpeaker')).toEqual([{ path: 'shared/access.play', line: 0, column: 7, keyword: 'policy' }]));
    it('finds every construct that bears the name', () => expect(application.declarationsOf('ReviewBoard').map((declaration) => declaration.keyword)).toEqual(['slice StateView', 'screen']));
    it('finds nothing for a name that is only referenced', () => expect(application.declarationsOf('ReviewDesk')).toEqual([]));
});

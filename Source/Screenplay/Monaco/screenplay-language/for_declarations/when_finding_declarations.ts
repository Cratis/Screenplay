// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { declarationHeaders } from '../declarations';

const find = (lines: string[]) => declarationHeaders(lines).map((header) => `${header.keyword} ${header.name}@${header.line}:${header.column}`);

describe('when finding declarations', () => {
    it('finds a concept, a policy and a read model', () =>
        expect(find(['concept TalkId : Uuid', 'policy IsSpeaker', '  readmodel TalkSummary'])).toEqual(['concept TalkId@0:8', 'policy IsSpeaker@1:7', 'readmodel TalkSummary@2:12']));
    it('finds a slice by its name, not its type', () => expect(find(['slice StateChange SubmitTalk'])).toEqual(['slice StateChange SubmitTalk@0:18']));
    it('finds a screen template apart from a screen', () =>
        expect(find(['screen template ReviewDesk', '  screen ReviewBoard'])).toEqual(['screen template ReviewDesk@0:16', 'screen ReviewBoard@1:9']));
    it('finds a dialog template, a form and a behavior', () =>
        expect(find(['dialog template ConfirmDialog', 'form SubmitTalkForm for SubmitTalk', 'behavior ConfirmThenExecute'])).toEqual(['dialog template ConfirmDialog@0:16', 'form SubmitTalkForm@1:5', 'behavior ConfirmThenExecute@2:9']));
    it('does not take a reference for a declaration', () =>
        expect(find(['    template ReviewDesk', '    authorize IsSpeaker', '    navigate to TalkList', '    talkId TalkId identifier'])).toEqual([]));
    it('skips comments and fenced code', () => expect(find(['// concept Hidden : Uuid', '```csharp', 'event Hidden', '```'])).toEqual([]));
});

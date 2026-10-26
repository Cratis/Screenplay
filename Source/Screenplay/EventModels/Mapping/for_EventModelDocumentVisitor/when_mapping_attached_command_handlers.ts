// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

function description(body: string, production = 'produces E'): string {
    const syntax = parse(`module M\n  feature F\n    slice StateChange S\n      event E\n      command C\n        ${production}\n        handler\n          ${body}`);
    return toEventModelDocument(syntax.value, 'Handlers').collections[0].modules[0].features[0].slices[0].command!.logicDescription;
}

describe('when mapping attached command handlers', () => {
    it('should describe admission without claiming execution', () => {
        const text = description('file C.cs');
        text.should.contain('ESM v23 requires unconditional produces');
        text.should.contain('resolved nonempty, language-tagged attached body');
        text.should.contain('Unsupported (ImplementationBody)');
    });
    it('should keep pending intent separate', () => description('implementation').should.contain('Pending intent reports PLAY0695'));
    it('should keep unconstrained bodies unadmitted', () => description('file C.cs', '').should.contain('Unconstrained or unresolved attachments remain unadmitted'));
});

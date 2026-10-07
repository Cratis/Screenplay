// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { FormSyntax } from '../../Syntax/DependencySources';
import { SliceReferenceCollector } from '../SliceReferences';
import { graphOf, producer } from '../for_DependencyGraph/given/a_model';

// C# form role specs call VisitForm directly; forms are module-owned, never collected by In(slice).
describe('when a form is visited directly', () => {
    let collector: SliceReferenceCollector;
    let form: FormSyntax;
    beforeEach(() => {
        form = {
            kind: 'FormSyntax', name: 'Edit', for: 'C', fields: [], onSubmit: null,
            location: { path: 'form.play', line: 2, column: 3 },
            populate: { kind: 'FormPopulateViaQuerySyntax', query: 'Q', by: null, location: { path: 'form.play', line: 3, column: 5 } },
        };
        collector = new SliceReferenceCollector();
        collector.visitForm(form);
    });
    it('should classify the command as asks with the form command role', () => {
        collector.references[0].should.deep.equal({ name: 'C', targetKind: 'Command', kind: 'asks', role: 'formCommand', location: form.location, timeline: false });
    });
    it('should classify the populate query as shows with its source location', () => {
        collector.references[1].should.deep.equal({ name: 'Q', targetKind: 'Query', kind: 'shows', role: 'populate', location: form.populate!.location, timeline: false });
    });
    it('should not invent a slice owner for a module form', () => {
        graphOf(`${producer}  form Edit for C\n    populate via query Q`).edges.should.have.lengthOf(0);
    });
    it('should not infer dependencies from an unsupported slice form', () => {
        graphOf(`${producer}  feature B\n    slice StateView Consumer\n      form Edit for C\n        populate via query Q`).edges.should.have.lengthOf(0);
    });
});

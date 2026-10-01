// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { AuthorizeSyntax, PolicyRequirementSyntax } from '../Syntax/Authorization';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

// The requirement written out the way it reads, with its grouping made explicit.
const read = (requirement: PolicyRequirementSyntax): string => requirement.kind === 'PolicyReferenceSyntax'
    ? requirement.name
    : `(${read(requirement.left)} ${requirement.operator.toLowerCase()} ${read(requirement.right)})`;
const readAuthorize = (authorize: AuthorizeSyntax | null): string => authorize === null ? '' : read(authorize.requirement);

describe('when parsing personas and authorization', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = a_parsed_document(
            'policy IsAccountant',
            '  require role "Accountant"',
            'persona Accountant',
            '  description "Keeps the books"',
            '  policy IsAccountant',
            '  policy CanManageInvoice',
            'persona Guest',
            'module Billing',
            '  authorize IsAuthenticated',
            '  feature Invoicing',
            '    authorize IsAccountant or IsClerk and CanManageInvoice',
            '    slice StateChange Register',
            '      command Register',
            '        authorize (IsAccountant or IsClerk) CanManageInvoice',
            '        authorize IsAuditor',
            '        invoiceId Guid',
            '    slice StateView Invoices',
            '      query AllInvoices => Invoice[]',
            '        authorize IsAccountant',
            '          or IsAuditor');
    });

    it('should read without diagnostics', () => result.diagnostics.should.deep.equal([]));
    it('should read every persona', () => result.value.personas.map(persona => persona.name).should.deep.equal(['Accountant', 'Guest']));
    it('should read a persona description', () => result.value.personas[0].description!.should.equal('Keeps the books'));
    it('should read the policies a persona holds', () => result.value.personas[0].policies.should.deep.equal(['IsAccountant', 'CanManageInvoice']));
    it('should read a persona without policies', () => result.value.personas[1].policies.should.deep.equal([]));
    it('should read who may use a module', () => readAuthorize(result.value.modules[0].authorize).should.equal('IsAuthenticated'));
    it('should bind and tighter than or', () => readAuthorize(result.value.modules[0].features[0].authorize).should.equal('(IsAccountant or (IsClerk and CanManageInvoice))'));
    it('should group with parentheses and join operands written side by side with and, and each authorize with and', () =>
        readAuthorize(result.value.modules[0].features[0].slices[0].commands[0].authorize).should.equal('(((IsAccountant or IsClerk) and CanManageInvoice) and IsAuditor)'));
    it('should still read the command properties after its authorize', () =>
        result.value.modules[0].features[0].slices[0].commands[0].properties.map(property => property.name).should.deep.equal(['invoiceId']));
    it('should continue a requirement on the lines below', () =>
        readAuthorize(result.value.modules[0].features[0].slices[1].queries[0].authorize).should.equal('(IsAccountant or IsAuditor)'));
    it('should leave a construct without authorize open', () => (a_parsed_document('module Open').value.modules[0].authorize === null).should.be.true);
});

describe('when parsing invalid personas and authorization', () => {
    const codes = (...lines: string[]): string[] => a_parsed_document(...lines).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`);

    it('should report an invalid persona declaration', () => codes('persona 1Bad').should.deep.equal(['PLAY0030@1']));
    it('should report an invalid persona policy', () => codes('persona Clerk', '  policy is-bad').should.deep.equal(['PLAY0031@2']));
    it('should report an unknown persona directive', () => codes('persona Clerk', '  role Admin').should.deep.equal(['PLAY0032@2']));
    it('should report an authorize without a policy', () => codes('module A', '  authorize').should.deep.equal(['PLAY0122@2']));
    it('should report a policy that is not PascalCase', () => codes('module A', '  authorize isAdmin').should.deep.equal(['PLAY0123@2']));
    it('should report a dangling operator', () => codes('module A', '  authorize IsAdmin or').should.deep.equal(['PLAY0122@2']));
    it('should report a dangling and', () => codes('module A', '  authorize IsAdmin and').should.deep.equal(['PLAY0122@2']));
    it('should report an unexpected token', () => codes('module A', '  authorize IsAdmin )').should.deep.equal(['PLAY0184@2']));
    it('should report an unclosed group', () => codes('module A', '  authorize (IsAdmin or IsClerk').should.deep.equal(['PLAY0185@2']));
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const prefix = 'module M\n  feature F\n    slice StateChange S\n';
const event = (type: string, declarations = '') => parse(declarations + prefix + `      event Changed\n        customerId ${type} subject`);

describe('when marking event subjects', () => {
    for (const type of ['String', 'Uuid', 'CustomerId']) {
        it(`should accept ${type}`, () => event(type, 'concept CustomerId : Int\n').success.should.be.true);
    }
    for (const type of ['String optional', 'String?', 'String[]', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime', 'Value']) {
        it(`should refuse ${type}`, () => event(type, 'type Value\n  key String\n').diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.InvalidSubjectType).should.be.true);
    }
    for (const primitive of ['Decimal', 'Bool', 'Date', 'DateTime', 'Enum']) {
        it(`should refuse ${primitive} concepts`, () => event('Value', `concept Value : ${primitive}\n`).diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.InvalidSubjectType).should.be.true);
    }
    for (const marker of ['pii', 'personal', 'secret', '@pii', 'sensitive', '@sensitive']) {
        it(`should refuse ${marker} keys`, () => event('CustomerId', `concept CustomerId : String ${marker}\n`).diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.ProtectedSubjectType).should.be.true);
    }
    it('should isolate the subject refusal on an otherwise valid operation', () => {
        const result = parse('system Mailer\n' + prefix + '      operation Send\n        uses Mailer\n        customerId Uuid subject\n        execute');
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([DiagnosticCodes.InvalidSubjectOwner]);
    });
    it('should report each extra mark naming the first', () => {
        const result = parse(prefix + '      event Changed\n        customerId Uuid subject\n        personId Uuid subject\n        employeeId String subject');
        result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.DuplicateEventSubject).map(diagnostic => diagnostic.message.includes('customerId')).should.deep.equal([true, true]);
    });
    it('should keep contextual names and inline metadata in syntax JSON', () => {
        const result = parse(prefix + '      event Changed\n        subject String\n        customerId Uuid subject\n      command Change\n        customerId Uuid\n        produces event Corrected\n          customerId Uuid subject = customerId');
        result.success.should.be.true;
        JSON.stringify(toSyntaxJson(result.value)).should.contain('"isSubject":true');
        result.value.modules[0].features[0].slices[0].commands[0].produces[0].inlineEvent!.properties[0].isSubject!.should.be.true;
    });
});

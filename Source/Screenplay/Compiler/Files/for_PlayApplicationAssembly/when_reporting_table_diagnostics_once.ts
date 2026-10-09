// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { compileApplication } from '../PlayApplicationAssembly';

const documents = new Map([
    ['application.play', 'eventsource Account\n  identifier String\n  stream All\nimport "table.play"'],
    ['table.play', 'module M\n  feature F\n    slice StateView S\n      event E\n      specification Appending\n        parameter unused Int\n        case One unused = 1\n        case Two unused = 2\n        case Three unused = 3\n        when append E\n          stream Account.All\n        then E'],
]);

describe('when reporting table diagnostics once', () => {
    it('should report one unused parameter warning and one authored route error', () => {
        const result = compileApplication(documents, ['application.play']);
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0588').length.should.equal(1);
        const routes = result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0550');
        routes.should.have.lengthOf(1);
        routes[0].location.path!.should.equal('table.play');
    });
});

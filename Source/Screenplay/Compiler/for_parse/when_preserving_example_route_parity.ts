// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SpecificationNoStreamSyntax } from '../Syntax/Specifications';

class route_walker extends ScreenplaySyntaxWalker {
    readonly markers: SpecificationNoStreamSyntax[] = [];
    visitSpecificationNoStream(syntax: SpecificationNoStreamSyntax): void {
        this.markers.push(syntax);
        super.visitSpecificationNoStream(syntax);
    }
}

describe('when preserving example route parity', () => {
    it.each(['concept Shape : String', 'type Shape\n  value String'])('should not apply command and read model route diagnostics to %s', declaration => {
        const result = parse(declaration + '\nexample Fixture : Shape\n  no stream');
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0526')).toEqual([]);
    });
    it('should dispatch example and locator markers through the overridable visitor', () => {
        const result = parse('module M\n  feature F\n    slice Automation S\n      event E\n      reaction Observer\n        when E\n      example Expected : E\n        no stream\n      specification X\n        given E\n        when redelivered E to Observer\n          no stream\n        then no events');
        expect(result.diagnostics).toEqual([]);
        const walker = new route_walker();
        walker.visitApplication(result.value);
        expect(walker.markers).toHaveLength(2);
    });
});

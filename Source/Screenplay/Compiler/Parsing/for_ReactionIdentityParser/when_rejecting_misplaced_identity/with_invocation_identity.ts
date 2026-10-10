// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { should } from 'chai';
import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../../ScreenplayCompiler';

should();

describe('when rejecting misplaced identity in an invocation', () => {
    let result: ReturnType<typeof parse>;

    beforeEach(() => {
        result = parse(`module M
  feature F
    slice Automation S
      event E
      command C
        name String
      reaction R
        when E
          invokes C
            runs as system
              role "Ignored"
            name = "Accepted"`);
    });

    it('should report only the misplaced identity', () => {
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0647']);
    });

    it('should resume parsing the following mapping', () => {
        result.value.modules[0].features[0].slices[0].reactions[0].triggers[0].invokes[0].mappings!.map(mapping => mapping.property).should.deep.equal(['name']);
    });
});

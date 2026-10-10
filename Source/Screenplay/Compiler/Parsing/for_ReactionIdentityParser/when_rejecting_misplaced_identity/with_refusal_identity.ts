// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { should } from 'chai';
import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../../ScreenplayCompiler';

should();

describe('when rejecting misplaced identity in a refusal branch', () => {
    let result: ReturnType<typeof parse>;

    beforeEach(() => {
        result = parse(`module M
  feature F
    slice Automation S
      event E
      command C
      reaction R
        when E
          invokes C
            on refused by validation
              runs as system
                role "Ignored"
            on refused by authorization
              acknowledge`);
    });

    it('should report the misplaced identity without a cascading empty branch error', () => {
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0647']);
    });

    it('should resume parsing the following refusal branch', () => {
        result.value.modules[0].features[0].slices[0].reactions[0].triggers[0].invokes[0].onRefused![1].acknowledge.should.be.true;
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { should } from 'chai';
import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../../ScreenplayCompiler';

should();

describe('when rejecting misplaced identity in a command', () => {
    let result: ReturnType<typeof parse>;

    beforeEach(() => {
        result = parse(`module M
  feature F
    slice Automation S
      command C
        runs as system
          role "Ignored"
        name String`);
    });

    it('should report only the misplaced identity', () => {
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0647']);
    });

    it('should resume parsing the following property', () => {
        result.value.modules[0].features[0].slices[0].commands[0].properties[0].name.should.equal('name');
    });
});

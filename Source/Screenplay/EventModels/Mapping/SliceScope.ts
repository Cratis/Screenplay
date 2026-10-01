// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { guidFor } from '../Document/identity';

// Where an element sits in the model - module, features, slice - which is what its id is derived from. Two
// elements get the same id exactly when they are the same element of the same model.
export class SliceScope {
    private constructor(readonly path: string) {}

    static module(name: string): SliceScope {
        return new SliceScope(name);
    }

    feature(name: string): SliceScope {
        return new SliceScope(`${this.path}/${name}`);
    }

    slice(name: string): SliceScope {
        return new SliceScope(`${this.path}#${name}`);
    }

    get id(): string {
        return guidFor(this.path);
    }

    idOf(kind: string, name = ''): string {
        return guidFor(`${this.path}:${kind}:${name}`);
    }
}

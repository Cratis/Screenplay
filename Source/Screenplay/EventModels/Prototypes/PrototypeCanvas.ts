// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PrototypeAnchoring, PrototypeElementDocument, PrototypeVisibility } from '../Document/EventModelDocument';
import { guidFor } from '../Document/identity';

// Where the elements of one prototype are collected. Each element is given an id derived from where it was
// drawn from, so the same screen yields the same prototype every time it is compiled.
export class PrototypeCanvas {
    readonly elements: PrototypeElementDocument[] = [];

    constructor(private readonly path: string) {}

    place(type: string, name: string, x: number, y: number, width: number, height: number): void {
        this.elements.push({
            id: guidFor(`${this.path}:element:${this.elements.length}`),
            name,
            type,
            width,
            height,
            ZIndex: 1,
            visibility: PrototypeVisibility.visible,
            isEnabled: true,
            opacity: 1,
            anchoring: PrototypeAnchoring.none,
            properties: { canvas: { x, y } },
        });
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';

/** A value an interaction reads from the signal it handles, written `signal.<value>`. */
export interface SignalValueRead {
    readonly value: string;
    readonly location: SourceLocation;
}

/** An `on <name>` interaction of a component and the signal values its actions read. */
export interface ComponentSignalUse {
    readonly trigger: string;
    readonly location: SourceLocation;
    readonly reads: readonly SignalValueRead[];
}

/**
 * The interactions of each placed component, keyed by the component node. Interaction bodies are opaque in the syntax tree,
 * so the extension use-site check reads them from here; the wire projection never carries them.
 */
export const componentSignalUses = new WeakMap<object, readonly ComponentSignalUse[]>();

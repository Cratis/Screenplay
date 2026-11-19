// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { darkTheme, lightTheme } from '@cratis/scene.blueprint.default';
import type { ScreenEditorAppearance } from '@cratis/scene.editor';

// How a board wants the screens on it to look: in the design system's own theme for the surface the board is
// drawn on, so a screen sits among the slices instead of as a light sheet on a dark board. No colour is named
// here - both themes are the ones the default blueprint ships, the same two a screen is designed against.
export function boardScreenAppearance(dark: boolean): ScreenEditorAppearance {
    return { theme: dark ? darkTheme : lightTheme };
}

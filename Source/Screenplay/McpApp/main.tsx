// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The board draws with Pixi, which compiles its shaders with new Function unless this module replaces that.
// The host's sandbox allows no 'unsafe-eval', so this keeps the board working inside it.
import 'pixi.js/unsafe-eval';
import { createRoot } from 'react-dom/client';
import '@cratis/components/tokens';
import '@cratis/components/styles';
import '@cratis/components/theme';
import '@cratis/event-models/theme';
import '@cratis/event-models/styles';
import '@cratis/scene/styles';
import './board.css';
import { applyDarkTheme } from './applyDarkTheme';
import { BoardApp } from './BoardApp';

applyDarkTheme();
createRoot(document.getElementById('root')!).render(<BoardApp />);

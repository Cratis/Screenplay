// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The board draws with Pixi, which compiles its shaders with new Function unless this module replaces that.
// It keeps the page working under a content security policy without 'unsafe-eval'.
import 'pixi.js/unsafe-eval';
import { createRoot } from 'react-dom/client';
import '@cratis/components/tokens';
import '@cratis/components/styles';
import '@cratis/components/theme';
import '@cratis/event-models/theme';
import '@cratis/event-models/styles';
import '@cratis/scene/styles';
import 'primeicons/primeicons.css';
import './board.css';
import { BoardApp } from './BoardApp';
import { applyDarkTheme } from './applyDarkTheme';

applyDarkTheme();
createRoot(document.getElementById('root')!).render(<BoardApp />);

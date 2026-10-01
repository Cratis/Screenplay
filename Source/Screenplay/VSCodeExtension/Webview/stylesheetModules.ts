// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Stylesheets the webview imports for their effect; esbuild bundles them into out/webview.css. This is a
// script rather than a .d.ts so the repository, which ignores generated declarations, keeps it.
declare module '*.css';
declare module '@cratis/components/tokens';
declare module '@cratis/components/styles';
declare module '@cratis/components/theme';
declare module '@cratis/event-models/theme';
declare module '@cratis/event-models/styles';
declare module '@cratis/scene/styles';

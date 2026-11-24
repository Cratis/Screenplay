// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { ScreenActionSyntax, ScreenDataSyntax } from '../Syntax/Screens';

const codes = (source: string): string[] => parse(source).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`);

const change = (actionGate: string): string[] => [
    'audience Plus', 'audience Pro', 'module Fleet', '  feature Quality', '    slice StateChange Register', '      event Registered', '        name String',
    '      command Register', '        available for Plus', '        name String', '        produces Registered', '          name = name',
    '      screen RegisterScreen', '        action Register', '          label "Register"', `          ${actionGate}`
];

const view: string[] = [
    'audience Plus', 'module Fleet', '  feature Quality', '    slice StateView Vessels', '      readmodel Vessel', '        vesselId String key', '      event Registered', '        vesselId String',
    '      projection Vessels => Vessel', '        from Registered key vesselId', '          vesselId = vesselId', '      query AllVessels => Vessel[]',
    '      screen VesselList', '        data Vessel[] via query AllVessels', '          available for Plus'
];

describe('when parsing gates on screen elements', () => {
    it('should record the gates of an action', () => {
        const result = parse(change('available for Plus and not Pro').join('\n'));
        const action = result.value.modules[0].features[0].slices[0].screens[0].directives[0] as ScreenActionSyntax;
        action.available!.length.should.equal(1);
    });

    it('should record the gate of a data directive', () => {
        const result = parse(view.join('\n'));
        const data = result.value.modules[0].features[0].slices[0].screens[0].directives[0] as ScreenDataSyntax;
        data.available!.length.should.equal(1);
    });

    it('should warn when the gate of an action is broader than its command', () => {
        codes(change('available for Plus or Pro').join('\n')).should.include('PLAY1172@14');
    });

    it('should stay silent when the gate of an action equals its command', () => {
        codes(change('available for Plus').join('\n')).should.not.include('PLAY1172@14');
    });

    it('should warn about an audience nothing declares', () => {
        codes(change('available for Ghost').join('\n')).should.include('PLAY1135@16');
    });
});

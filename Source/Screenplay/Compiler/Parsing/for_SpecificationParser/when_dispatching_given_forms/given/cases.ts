// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The Given characterization matrix, shared with the native specification of the same name. Every case ends in a
// sibling event ('given Marker') so the reader position after each form is pinned. Each expectation is the
// diagnostics (severity, code, line:column, text) in order, then the Given state that was built.
export interface GivenCase {
    readonly name: string;
    readonly exact: boolean;
    readonly body: string;
    readonly expected: string;
}

const none = 'readModels=[] captures=[] generated=[] clock=none timeZone=none caller=none persona=none failures=[]';
const clockPrompt = `Invalid 'given clock' - expected 'given clock "<ISO 8601 instant>"', such as 'given clock "2026-10-05T08:00:00Z"'`;
const zoneRule = `A specification states its time zone at most once, as 'given time zone "<id>"'.`;
const callerRule = `A specification has at most one 'given caller' block.`;
const operationRule = `Expected 'given operation <Name> fails', 'then operation <Name>' or 'then compensated <Name>'.`;
const eventRule = (text: string): string => `error PLAY0100 ${text}`;

export const givenCases: readonly GivenCase[] = [
    { name: 'every valid form', exact: false, body: 'given clock "2026-10-05T08:00:00Z"\ngiven time zone "Europe/Oslo"\ngiven generated CreateShipment.shipmentId = "11111111-1111-4111-8111-111111111111"\ngiven capture token\n  id = "a"\ngiven caller\n  authenticated\ngiven readmodel Row\n  id = "one"\ngiven Opened\n  amount = 1',
        expected: 'events=[Opened,Marker] readModels=[Row] captures=[token] generated=[CreateShipment.shipmentId] clock=2026-10-05T08:00:00Z timeZone=Europe/Oslo caller=true//0 persona=none failures=[]' },
    { name: 'duplicate clock with a body', exact: false, body: 'given clock "2026-10-05T08:00:00Z"\ngiven clock "2026-10-06T08:00:00Z"\n  nested = 1',
        expected: `error PLAY0461 3:3 A specification states its clock at most once.\nevents=[Marker] ${none.replace('clock=none', 'clock=2026-10-05T08:00:00Z')}` },
    { name: 'invalid first clock then a valid clock', exact: false, body: 'given clock nonsense\n  nested = 1\ngiven clock "2026-10-06T08:00:00Z"',
        expected: `error PLAY0461 2:3 ${clockPrompt}\nevents=[Marker] ${none.replace('clock=none', 'clock=2026-10-06T08:00:00Z')}` },
    { name: 'duplicate time zone with a body', exact: false, body: 'given time zone "Europe/Oslo"\ngiven time zone "Asia/Tokyo"\n  nested = 1',
        expected: `error PLAY0814 3:3 ${zoneRule}\nevents=[Marker] ${none.replace('timeZone=none', 'timeZone=Europe/Oslo')}` },
    { name: 'invalid first time zone then a valid zone', exact: false, body: 'given time zone nonsense\n  nested = 1\ngiven time zone "Asia/Tokyo"',
        expected: `error PLAY0814 2:3 ${zoneRule}\nevents=[Marker] ${none.replace('timeZone=none', 'timeZone=Asia/Tokyo')}` },
    { name: 'malformed recognized prefix with a body then a valid sibling', exact: false, body: 'given capture 1bad\n  id = "a"\ngiven readmodel\n  id = "x"\ngiven Opened',
        expected: `error PLAY0463 2:3 Invalid 'given capture' declaration 'given capture 1bad' - expected 'given capture <Capture>'\nerror PLAY0099 4:3 Invalid 'given readmodel' declaration 'given readmodel' - expected 'given readmodel <ReadModelType>'\nevents=[Opened,Marker] ${none}` },
    { name: 'generated repetitions and an invalid one', exact: false, body: 'given generated A.x = "1"\ngiven generated A.y = "2"\ngiven generated nonsense\n  nested = 1\ngiven generated B.z = "3"',
        expected: `error PLAY0490 4:3 Expected 'given generated <Command>.<property> = <value>' with exactly one concrete value.\nevents=[Marker] ${none.replace('generated=[]', 'generated=[A.x,A.y,B.z]')}` },
    { name: 'capture and read model repetitions', exact: false, body: 'given capture one\n  id = "a"\ngiven capture two\n  id = "b"\ngiven readmodel R1\n  id = "x"\ngiven readmodel R2\n  id = "y"\ngiven E1\ngiven E2',
        expected: 'events=[E1,E2,Marker] readModels=[R1,R2] captures=[one,two] generated=[] clock=none timeZone=none caller=none persona=none failures=[]' },
    { name: 'explicit caller then a second caller', exact: false, body: 'given caller\n  authenticated\ngiven caller\n  role "Admin"',
        expected: `error PLAY0387 4:3 ${callerRule}\nevents=[Marker] ${none.replace('caller=none', 'caller=true//0')}` },
    { name: 'explicit caller then a persona', exact: false, body: 'given caller\n  authenticated\ngiven caller as Accountant',
        expected: `error PLAY0387 4:3 ${callerRule}\nevents=[Marker] ${none.replace('caller=none', 'caller=true//0')}` },
    { name: 'persona then an explicit caller', exact: false, body: 'given caller as Accountant\ngiven caller\n  authenticated',
        expected: `error PLAY0387 3:3 ${callerRule}\nevents=[Marker] ${none.replace('persona=none', 'persona=Accountant')}` },
    { name: 'persona twice', exact: false, body: 'given caller as Accountant\ngiven caller as Auditor',
        expected: `error PLAY0387 3:3 ${callerRule}\nevents=[Marker] ${none.replace('persona=none', 'persona=Accountant')}` },
    { name: 'persona with a body', exact: false, body: 'given caller as Accountant\n  role "Other"',
        expected: `error PLAY0572 3:5 'given caller as <Persona>' has no body; use an explicit 'given caller' for refinements.\nevents=[Marker] ${none.replace('persona=none', 'persona=Accountant')}` },
    { name: 'malformed persona with a body', exact: false, body: 'given caller as\n  role "Other"',
        expected: `error PLAY0572 2:3 Expected 'given caller as <Persona>'.\nerror PLAY0572 3:5 'given caller as <Persona>' has no body; use an explicit 'given caller' for refinements.\nevents=[Marker] ${none}` },
    { name: 'malformed caller header and broad caller prefix', exact: false, body: 'given caller extra\n  authenticated\ngiven callers',
        expected: `error PLAY0386 2:3 Expected exactly 'given caller'.\nerror PLAY0386 4:3 Expected exactly 'given caller'.\nevents=[Marker] ${none}` },
    { name: 'duplicate caller skipping a fenced body', exact: false, body: 'given caller\n  authenticated\ngiven caller\n  ```text\nnumbers exact\n  ```\n  role "Admin"',
        expected: `error PLAY0387 4:3 ${callerRule}\nerror PLAY0095 6:3 Unexpected 'numbers' in specification body\nevents=[Marker] ${none.replace('caller=none', 'caller=true//0')}` },
    // The compilers differ here: the TypeScript duplicate-caller branch skips with skipBlock, which never steps over a fence whole, even in exact mode.
    { name: 'duplicate caller skipping a fenced body in exact mode', exact: true, body: 'given caller\n  authenticated\ngiven caller\n  ```text\nnumbers exact\n  ```\n  role "Admin"',
        expected: `error PLAY0387 5:3 ${callerRule}\nerror PLAY0095 7:3 Unexpected 'numbers' in specification body\nevents=[Marker] ${none.replace('caller=none', 'caller=true//0')}` },
    { name: 'time zone with a fenced body in exact mode', exact: true, body: 'given time zone "Europe/Oslo"\n  ```text\nnumbers exact\n  ```\n  numbers exact',
        expected: `error PLAY0508 7:5 Expected 'numbers exact' only as the document preamble, not inside a body.\nevents=[Marker] ${none.replace('timeZone=none', 'timeZone=Europe/Oslo')}` },
    { name: 'operation failures take precedence', exact: false, body: 'given operation Charge fails\ngiven operation\n  nested = 1\ngiven operation Refund',
        expected: `error PLAY0502 3:3 ${operationRule}\nerror PLAY0502 5:3 ${operationRule}\nevents=[Marker] ${none.replace('failures=[]', 'failures=[Charge]')}` },
    { name: 'event fallback and whitespace boundaries', exact: false, body: 'given\ngiven lower\ngiven  Spaced\ngiven clockwise\ngiven time zones\ngiven captured\ngiven readmodels\ngiven Opened\n  amount = 1',
        expected: [
            eventRule(`2:3 Invalid 'given' declaration 'given' - expected 'given <EventType>'`),
            eventRule(`3:3 Invalid 'given' declaration 'given lower' - expected 'given <EventType>'`),
            eventRule(`5:3 Invalid 'given' declaration 'given clockwise' - expected 'given <EventType>'`),
            eventRule(`6:3 Invalid 'given' declaration 'given time zones' - expected 'given <EventType>'`),
            eventRule(`7:3 Invalid 'given' declaration 'given captured' - expected 'given <EventType>'`),
            eventRule(`8:3 Invalid 'given' declaration 'given readmodels' - expected 'given <EventType>'`),
            `events=[Spaced,Opened,Marker] ${none}`
        ].join('\n') },
    { name: 'indented blocks stay with their form', exact: false, body: 'given Opened\n  amount = 1\n    deeper = 2\ngiven readmodel Row\n  id = "one"\n    deeper = 2',
        expected: 'events=[Opened,Marker] readModels=[Row] captures=[] generated=[] clock=none timeZone=none caller=none persona=none failures=[]' }
];

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing.for_SpecificationParser.when_dispatching_given_forms;

/// <summary>
/// The Given characterization matrix, shared with the TypeScript specification of the same name. Every case ends in a
/// sibling event ('given Marker') so the reader position after each form is pinned. Each expectation is the
/// diagnostics (severity, code, line:column, text) in order, then the Given state that was built.
/// </summary>
static class GivenCases
{
    const string None = "readModels=[] captures=[] generated=[] clock=none timeZone=none caller=none persona=none failures=[]";
    const string ClockPrompt = "Invalid 'given clock' - expected 'given clock \"<ISO 8601 instant>\"', such as 'given clock \"2026-10-05T08:00:00Z\"'";
    const string ZoneRule = "A specification states its time zone at most once, as 'given time zone \"<id>\"'.";
    const string CallerRule = "A specification has at most one 'given caller' block.";
    const string OperationRule = "Expected 'given operation <Name> fails', 'then operation <Name>' or 'then compensated <Name>'.";
    const string PersonaBody = "'given caller as <Persona>' has no body; use an explicit 'given caller' for refinements.";
    const string FencedBody = "given caller\n  authenticated\ngiven caller\n  ```text\nnumbers exact\n  ```\n  role \"Admin\"";

    public static IEnumerable<object[]> All =>
    [
        Case("every valid form", false, "given clock \"2026-10-05T08:00:00Z\"\ngiven time zone \"Europe/Oslo\"\ngiven generated CreateShipment.shipmentId = \"11111111-1111-4111-8111-111111111111\"\ngiven capture token\n  id = \"a\"\ngiven caller\n  authenticated\ngiven readmodel Row\n  id = \"one\"\ngiven Opened\n  amount = 1",
            "events=[Opened,Marker] readModels=[Row] captures=[token] generated=[CreateShipment.shipmentId] clock=2026-10-05T08:00:00Z timeZone=Europe/Oslo caller=true//0 persona=none failures=[]"),
        Case("duplicate clock with a body", false, "given clock \"2026-10-05T08:00:00Z\"\ngiven clock \"2026-10-06T08:00:00Z\"\n  nested = 1",
            $"error PLAY0461 3:3 A specification states its clock at most once.\nevents=[Marker] {None.Replace("clock=none", "clock=2026-10-05T08:00:00Z")}"),
        Case("invalid first clock then a valid clock", false, "given clock nonsense\n  nested = 1\ngiven clock \"2026-10-06T08:00:00Z\"",
            $"error PLAY0461 2:3 {ClockPrompt}\nevents=[Marker] {None.Replace("clock=none", "clock=2026-10-06T08:00:00Z")}"),
        Case("duplicate time zone with a body", false, "given time zone \"Europe/Oslo\"\ngiven time zone \"Asia/Tokyo\"\n  nested = 1",
            $"error PLAY0814 3:3 {ZoneRule}\nevents=[Marker] {None.Replace("timeZone=none", "timeZone=Europe/Oslo")}"),
        Case("invalid first time zone then a valid zone", false, "given time zone nonsense\n  nested = 1\ngiven time zone \"Asia/Tokyo\"",
            $"error PLAY0814 2:3 {ZoneRule}\nevents=[Marker] {None.Replace("timeZone=none", "timeZone=Asia/Tokyo")}"),
        Case("malformed recognized prefix with a body then a valid sibling", false, "given capture 1bad\n  id = \"a\"\ngiven readmodel\n  id = \"x\"\ngiven Opened",
            $"error PLAY0463 2:3 Invalid 'given capture' declaration 'given capture 1bad' - expected 'given capture <Capture>'\nerror PLAY0099 4:3 Invalid 'given readmodel' declaration 'given readmodel' - expected 'given readmodel <ReadModelType>'\nevents=[Opened,Marker] {None}"),
        Case("generated repetitions and an invalid one", false, "given generated A.x = \"1\"\ngiven generated A.y = \"2\"\ngiven generated nonsense\n  nested = 1\ngiven generated B.z = \"3\"",
            $"error PLAY0490 4:3 Expected 'given generated <Command>.<property> = <value>' with exactly one concrete value.\nevents=[Marker] {None.Replace("generated=[]", "generated=[A.x,A.y,B.z]")}"),
        Case("capture and read model repetitions", false, "given capture one\n  id = \"a\"\ngiven capture two\n  id = \"b\"\ngiven readmodel R1\n  id = \"x\"\ngiven readmodel R2\n  id = \"y\"\ngiven E1\ngiven E2",
            "events=[E1,E2,Marker] readModels=[R1,R2] captures=[one,two] generated=[] clock=none timeZone=none caller=none persona=none failures=[]"),
        Case("explicit caller then a second caller", false, "given caller\n  authenticated\ngiven caller\n  role \"Admin\"",
            $"error PLAY0387 4:3 {CallerRule}\nevents=[Marker] {None.Replace("caller=none", "caller=true//0")}"),
        Case("explicit caller then a persona", false, "given caller\n  authenticated\ngiven caller as Accountant",
            $"error PLAY0387 4:3 {CallerRule}\nevents=[Marker] {None.Replace("caller=none", "caller=true//0")}"),
        Case("persona then an explicit caller", false, "given caller as Accountant\ngiven caller\n  authenticated",
            $"error PLAY0387 3:3 {CallerRule}\nevents=[Marker] {None.Replace("persona=none", "persona=Accountant")}"),
        Case("persona twice", false, "given caller as Accountant\ngiven caller as Auditor",
            $"error PLAY0387 3:3 {CallerRule}\nevents=[Marker] {None.Replace("persona=none", "persona=Accountant")}"),
        Case("persona with a body", false, "given caller as Accountant\n  role \"Other\"",
            $"error PLAY0572 3:5 {PersonaBody}\nevents=[Marker] {None.Replace("persona=none", "persona=Accountant")}"),
        Case("malformed persona with a body", false, "given caller as\n  role \"Other\"",
            $"error PLAY0572 2:3 Expected 'given caller as <Persona>'.\nerror PLAY0572 3:5 {PersonaBody}\nevents=[Marker] {None}"),
        Case("malformed caller header and broad caller prefix", false, "given caller extra\n  authenticated\ngiven callers",
            $"error PLAY0386 2:3 Expected exactly 'given caller'.\nerror PLAY0386 4:3 Expected exactly 'given caller'.\nevents=[Marker] {None}"),
        Case("duplicate caller skipping a fenced body", false, FencedBody,
            $"error PLAY0387 4:3 {CallerRule}\nerror PLAY0095 6:3 Unexpected 'numbers' in specification body\nevents=[Marker] {None.Replace("caller=none", "caller=true//0")}"),

        // The compilers differ here: the native duplicate-caller branch skips with SkipBody, which steps over a fence whole in exact mode;
        // the TypeScript branch uses skipBlock and so reports the fenced line as a stray directive.
        Case("duplicate caller skipping a fenced body in exact mode", true, FencedBody,
            $"error PLAY0387 5:3 {CallerRule}\nevents=[Marker] {None.Replace("caller=none", "caller=true//0")}"),
        Case("time zone with a fenced body in exact mode", true, "given time zone \"Europe/Oslo\"\n  ```text\nnumbers exact\n  ```\n  numbers exact",
            $"error PLAY0508 7:5 Expected 'numbers exact' only as the document preamble, not inside a body.\nevents=[Marker] {None.Replace("timeZone=none", "timeZone=Europe/Oslo")}"),
        Case("operation failures take precedence", false, "given operation Charge fails\ngiven operation\n  nested = 1\ngiven operation Refund",
            $"error PLAY0502 3:3 {OperationRule}\nerror PLAY0502 5:3 {OperationRule}\nevents=[Marker] {None.Replace("failures=[]", "failures=[Charge]")}"),
        Case("event fallback and whitespace boundaries", false, "given\ngiven lower\ngiven  Spaced\ngiven clockwise\ngiven time zones\ngiven captured\ngiven readmodels\ngiven Opened\n  amount = 1",
            string.Join('\n', EventRule("2:3", "given"), EventRule("3:3", "given lower"), EventRule("5:3", "given clockwise"), EventRule("6:3", "given time zones"), EventRule("7:3", "given captured"), EventRule("8:3", "given readmodels"), $"events=[Spaced,Opened,Marker] {None}")),
        Case("indented blocks stay with their form", false, "given Opened\n  amount = 1\n    deeper = 2\ngiven readmodel Row\n  id = \"one\"\n    deeper = 2",
            "events=[Opened,Marker] readModels=[Row] captures=[] generated=[] clock=none timeZone=none caller=none persona=none failures=[]")
    ];

    static string EventRule(string position, string text) => $"error PLAY0100 {position} Invalid 'given' declaration '{text}' - expected 'given <EventType>'";

    static object[] Case(string name, bool exact, string body, string expected) => [name, exact, body, expected];
}

# TimeTracking

A consultancy's time tracking, written in Screenplay. Engagement managers open client engagements and staff consultants on them. Consultants record their hours each week and submit the timesheet. Managers approve the week or return it, and payroll officers collect the approved hours into payroll runs for an external payroll provider. Absences reported in the HR system come in as time off.

## Monthly lookup keys

`Timesheets/Reporting/CheckingEngagementMonths.play` declares a monthly row keyed by engagement and reporting month. Its single-instance query supplies both parts in a `by` block. `ClosingAnEngagementMonth.play` supplies the same parts in a command reads block. Screens use a collection query because scalar screen lookups cannot supply composite keys.

These files compile as authoring syntax. Composite keys and by-block queries refuse executable binding with `PLAY0268` (#599). Command reads also retain their separate decision-consistency limitation (#129).

## How the files are composed

Every file tells one part of the story. Three kinds of file put them together:

- **One root**: `timetracking.play` declares the domain and imports `"**/*.play"`. That one glob is the whole application.
- **One file per module**: `Timesheets/Timesheets.play` declares the module (description, gate, `import "Screens.play"`) and its features inline. Each feature carries its own description and gate and imports its own folder:

  ```screenplay
  module Timesheets
    authorize HasWorkspaceAccess
    feature Recording
      authorize IsConsultant
      import "Recording/*.play"
  ```

- **One file per story step**: `Timesheets/Recording/RecordingHours.play` holds `slice StateChange RecordTime` with its command, events, screen and specifications. Nothing sits above the slice, because the feature's import already says where it goes. A step file can also contribute on its feature's behalf, as `SeeingMyAssignments.play` and `ReturningAWeek.play` do with `contribute to Navigation`.

The Commerce sample shows the other style, with an explicit composite file at every level.

### Placement: the deepest import wins

The root glob matches every file, including the step files. A file is compiled once however many imports match it, and when several imports place it, the deepest placement wins:

| File | Matched by | Placed in |
| --- | --- | --- |
| `Foundation/*.play` | the root | the application |
| `Timesheets/Timesheets.play` | the root | the application, where it declares `module Timesheets` |
| `Timesheets/Screens.play` | the root and `import "Screens.play"` in the module | `module Timesheets` |
| `Timesheets/Recording/RecordingHours.play` | the root and `import "Recording/*.play"` in `feature Recording` | `Timesheets.Recording` |

The approved-week reaction declares the `PayrollOfficer` and `PayrollAutomation` system roles. The latter is the explicit alternative to the finance department claim, which remains required for ordinary human payroll callers. System claims are unknown in reference execution; this trusted role path selects ESM v10 (claimed, unreleased).

Gates compose along that path. `RecordTime` must pass `HasWorkspaceAccess` (the module) and `IsConsultant` (the feature) without restating either.

```text
Samples/TimeTracking/
  timetracking.play                          the root: domain and import "**/*.play"
  timetracking.en.strings                    English text for every $strings key
  timetracking.nb.strings                    Norwegian (bokmål) text for every $strings key
  Foundation/
    Identities.play                          the event-source identities
    Values.play                              domain values and the rules that travel with them
    PersonalData.play                        PII concepts with their reasons, and the ConsultantContact type
    Enumerations.play                        engagement, billing, timesheet and absence states
    Access.play                              policies, the three personas, authentication
    Shell.play                               theme, the AppShell layout, Desktop and Mobile ui profiles
    Behaviors.play                           ConfirmThenExecute and RefreshOnEnter
    Triggers.play                            the payroll provider's acknowledgement signal
    Seed.play                                a starting state: two engagements, a consultant, a week
  Engagements/
    Engagements.play                         the module, its gate, and features Setup and Portfolio
    Screens.play                             list/details and freeform dashboard templates, forms, navigation
    Setup/OpeningAnEngagement.play            a manager opens an engagement for a client
    Setup/StaffingAConsultant.play           a consultant is staffed on it at an agreed rate
    Setup/ClosingAnEngagement.play           the engagement is closed when the work is done
    Portfolio/OverseeingThePortfolio.play    managers and payroll watch every engagement
    Portfolio/SeeingMyAssignments.play       a consultant sees what they are staffed on
  Timesheets/
    Timesheets.play                          the module, its gate, and features Recording, Approval and Reporting
    Screens.play                             the week view and dialog templates, forms, navigation
    Recording/StartingAWeek.play             a consultant opens the timesheet for a week
    Recording/RecordingHours.play            hours are recorded against an engagement, day by day
    Recording/SubmittingTheWeek.play         the week is handed in for approval
    Recording/FollowingMyWeeks.play          the consultant follows each week to approval
    Approval/ApprovingAWeek.play             a manager approves a submitted week
    Approval/ReturningAWeek.play             or returns it with a reason
    Reporting/TotallingTheWeek.play          a reducer keeps the week's running totals
    Reporting/TrackingWeeksOnTheBoard.play   the approval board, built from projection variants
    Reporting/SummingHoursPerEngagement.play hours add up per engagement
    Reporting/CheckingPublicHolidays.play    the national calendar, read by a query performer
  Payroll/
    Payroll.play                             the module, its claim-based gate, and features Runs, Handover and Absences
    Screens.play                             the ledger and dialog templates, navigation
    Runs/OpeningARun.play                    payroll opens the run for a week
    Runs/QueueingHoursOnARun.play            an approved week goes onto the run
    Runs/ExportingARun.play                  the filled run goes to the payroll provider
    Runs/ReviewingRuns.play                  every run, what is on it, and the current one
    Handover/QueueingApprovedWeeks.play      approved weeks are queued automatically
    Handover/RemindingConsultants.play       Monday morning reminders for weeks still in draft
    Handover/RecordingProviderAcknowledgement.play   the provider's acknowledgement lands on the run
    Absences/ImportingAbsences.play          the HR system's absences are captured as facts
    Absences/BookingTimeOff.play             a reported absence is booked as time off
```

## What it demonstrates

- File imports: one root glob, module files that declare their features inline, and step files that hold a single slice.
- Commands that decide against state: `reads … as … by …`, `require` with `severity` and `$strings` messages, `produces when`, and `for` destinations.
- Queries that are `observable`, `scoped to identity` or `scoped to global`, with parameters filled `from $context.identity.id`.
- Four ways a read model is built: a projection, projection `variant`s, a `reducer`, and a read model nothing builds, served by a query `performer`.
- Module and feature gates, a claim-based policy, and personas whose policies decide whose row a screen is drawn on in the event-model board.
- Templates with `flow` and `freeform` arrangements, forms, `contribute to`, `on enter` and `on click` with `on success` and `on failure`, and named behaviors attached with `uses`. Click navigation opens the engagement, start-week and payroll-run dialogs before their commands run. Row clicks carry `engagementId` to the closure dialog and `timesheetId` to the submission and rejection dialogs. The closure dialog's discovered form collects the reason. Issuing actions and confirmation behaviors live on the dialog screens; action navigation back to a list happens only after success, never to the command's own input dialog.
- An application `trigger`, reactions driven by events, by the clock and by a trigger, a `where` filter, `invokes`, and a change data `capture`.
- Typed specification examples: `BillableDay` in `RecordingHours.play` supplies repeated `RecordTime` inputs. Scenarios override hours, date or billing details while retaining their own caller and outcome; no setup or implicit defaults are introduced.
- Specifications on every slice: `then error`, `then denied`, `given clock`, `when query` with `then result` and `then no result`, `when clock`, `when trigger`, and `given capture` with `when capture`.

## Verify it

From the repository root, compile the application from its root file, or the folder with every file as a root. Both compile the same 40 files with no errors and no warnings:

```bash
screenplay Samples/TimeTracking/timetracking.play
screenplay Samples/TimeTracking
```

Without the tool installed, use `dotnet run --project Source/DotNET/Tool -- Samples/TimeTracking/timetracking.play`.

The samples specification checks that every sample compiles cleanly, that every state change and state view has a screen, that every slice has a specification and that every `$strings` key is defined in each locale:

```bash
dotnet test Source/DotNET/Screenplay/Screenplay.csproj --filter "FullyQualifiedName~for_Samples"
```

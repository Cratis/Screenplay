# Approvals

An approvals application in one file, built from Scene's **Approvals blueprint** (`Cratis.Blueprint.Approvals`,
`@cratis/scene.blueprint.approvals`). Like [Crm](../Crm) and [Ticketing](../Ticketing) it is an application whose screens come from a *domain blueprint*:
every screen fills one of the blueprint's templates, the navigation is declared in the layout with the same regions the designer shows, and the demo data
is the blueprint's own sample-data pack.

```text
Approvals/
  approvals.play   concepts, one policy and persona, the application shell with its navigation, a ui profile, and one module with four features
```

| Persona | Policy | Sees the screens of |
| --- | --- | --- |
| `Employee` | `IsEmployee` | every screen |

## What to read it for

- **A decision from the inbox.** `DecideRequest` has two screens: `Decide` fills the blueprint's `DecisionDialog` and `WaitingForMe` fills `ApprovalInbox`,
  whose cards carry Approve, Reject and Request changes as buttons; the inbox's `toolbar` slot is given the command a decision issues.
- **A wizard that issues one command.** `NewRequest` fills `NewRequestWizard` (request type, details, attachments, review) and its `actions` slot is given
  `SubmitRequest`, the command the last step commits.
- **A request page.** `Request` fills `RequestPage`, which draws the approval chain, attachments and comments around the request.
- **An administrator's list and editor.** `Types` fills `RequestTypes`; `EditType` fills `RequestTypeEditForm` and `QuickNewType` fills the quick-create dialog,
  both issuing `DefineRequestType`.
- **The data the preview shows.** The `ui profile` activates the blueprint's English pack with `active blueprint approvals-general`; the blueprint also ships a
  Norwegian one (`approvals-nordic`).

## Verify

```bash
screenplay --warnaserror Samples/Approvals
screenplay test Samples/Approvals
```

The blueprint itself is documented in
[Scene's blueprint documentation](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/approvals-blueprint.md).

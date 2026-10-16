# Ticketing

A support team's help desk in one file, built from Scene's **Ticketing blueprint** (`Cratis.Blueprint.Ticketing`,
`@cratis/scene.blueprint.ticketing`). Like [Crm](../Crm), it is an application whose screens come from a *domain blueprint*: every screen fills one of
the blueprint's templates, the navigation is declared in the layout with the same regions the designer shows, and the demo data is the blueprint's own
sample-data pack.

```text
Ticketing/
  ticketing.play   concepts, one policy and persona, the application shell with its navigation, a ui profile, and one module with five features
```

| Persona | Policy | Sees the screens of |
| --- | --- | --- |
| `Agent` | `IsAgent` | every screen |

## What to read it for

- **A dialog for a command.** `AssignTicket`, `ChangeTicketStatus`, `EscalateTicket` and `MergeTickets` are the four quick actions of the ticket page. Each is a
  State Change slice with a screen that fills the blueprint's *dialog* template (`template blueprint Cratis.Blueprint.Ticketing AssignTicket`) and gives
  its `actions` slot the command, so the dialog the page opens issues that command.
- **A board that issues a command.** The board's `toolbar` slot is given `ChangeTicketStatus`, the command a card's move issues.
- **A public form.** `Portal` fills `TicketPortalForm`, which the blueprint draws in the full-page layout without application chrome.
- **One command, three screens.** `OpenTicket` has the agent's form (`NewTicket`), the quick-create dialog (`QuickNewTicket`) and the portal (`Portal`).
- **Nested navigation.** Under Queue the sidebar lists List and Board; `Board` is a feature inside `Queue`.
- **The data the preview shows.** The `ui profile` activates the blueprint's English pack with `active blueprint ticketing-helpdesk`; the blueprint also
  ships a German one (`ticketing-service-desk-de`).

## Verify

```bash
screenplay --warnaserror Samples/Ticketing
screenplay test Samples/Ticketing
```

The blueprint itself is documented in
[Scene's blueprint documentation](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/ticketing-blueprint.md).

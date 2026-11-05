# Organization

People belong to groups, groups nest inside groups, and a person can hold several roles in one group. The
organization is modeled as a **property graph**: nobody writes to the graph, every node and edge is what the
events of the organization leave behind.

```text
Organization/
  organization.play   one policy and persona, eight command slices, one graph slice with its projection
```

| Persona | Policy | Sees the screens of |
| --- | --- | --- |
| `Administrator` | `IsAdministrator` | every dialog: register or remove a person, create or nest a group, add or remove a member, assign or revoke a role |

## What to read it for

- **A graph declaration.** `graph Organization` declares the node labels `Person` and `Group`, each with exactly
  one `key` property, and the edge types `MEMBER_OF`, `PART_OF` and `HAS_ROLE` between them.
- **A graph projection.** `projection OrganizationChart => graph Organization` says which events create, change
  and remove which nodes and edges. An edge handler names its two endpoints with `source` and `target`; a node
  removal says `detach` to remove the edges attached to the node with it.
- **Edges that are nested groups.** `PART_OF` joins a `Group` to a `Group`, so a group inside a group is one
  more edge, not a nested object.
- **Several roles in one group.** `HAS_ROLE` is declared `identified by assignmentId`, so one person can hold
  two roles in the same group as two edges.
- **Specifications about a graph.** `then graph Organization` asserts `has node`, `has edge`, `no node` and
  `no edge`; `given graph` seeds a graph before the action.

## Verify

```bash
screenplay --warnaserror Samples/Organization
# or, from a clone of this repository
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Organization
```

See [Graphs](../../Documentation/screenplay/graphs.md) and
[Graph projections](../../Documentation/screenplay/projections/graphs.md).

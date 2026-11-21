// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Extensions;

namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v54 golden: every v53 construct plus graph queries that collect nested rows.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v54 golden: the v53 golden plus a staff graph read as views.
    /// </summary>
    public const string GraphCollectSource = AvailabilitySource + """


        type TeamPlacement
          teamId Uuid
          title  String
          since  DateTime optional

        type ManagerSummary
          employeeId Uuid
          name       String

        module Staffing
          feature Chart
            slice StateView StaffViews
              graph Staff
                node Employee
                  employeeId Uuid key
                  name       String
                node Team
                  teamId Uuid key
                  title  String
                edge WORKS_IN source Employee target Team
                  since DateTime optional
                edge REPORTS_TO source Employee target Employee

              readmodel EmployeeView
                employeeId Uuid key
                name       String
                teams      TeamPlacement[]
                manager    ManagerSummary optional

              query EmployeesInTeam => EmployeeView[] graph Staff
                filter teamId Uuid
                match employee:Employee
                where team.teamId = $teamId
                return
                  employeeId = employee.employeeId
                  name       = employee.name
                  teams      = collect employee -placement:WORKS_IN-> team:Team
                    teamId = team.teamId
                    title  = team.title
                    since  = placement.since
                  manager    = collect employee -REPORTS_TO-> manager:Employee
                    employeeId = manager.employeeId
                    name       = manager.name
        """;

    /// <summary>
    /// Gets the canonical bytes of the v54 golden.
    /// </summary>
    public static byte[] EsmV54Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v54.json");

    /// <summary>
    /// Creates the v54 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV54()
    {
        using var fixture = ExtensionFixture.Locked("acme/wallet", "1", ExtensionComponentContract);
        var previous = ExtensionContractResolver.Current;
        ExtensionContractResolver.Current = fixture.Resolver();
        try
        {
            var result = property_order_binding.AdmitScreens(GraphCollectSource);
            if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
            return result.Compilation!.Model;
        }
        finally
        {
            ExtensionContractResolver.Current = previous;
        }
    }
}
#endif

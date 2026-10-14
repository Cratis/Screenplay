// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_checking_reducer_identity.given;

public class a_reducer_identity_model : Specification
{
    protected const string Model = """
        concept ResourceId : String
        concept Month : String
        concept SelectionId : String
        concept AllocationId : String
        module Work
          feature Reporting
            slice StateView Totals
              event Recorded
                resource ResourceId
              readmodel Totals
                month Month key
              reducer Fold => Totals
                on Recorded
              query ByMonth => Totals
                by month Month
            slice StateChange Record
              command Record
                resource ResourceId identifier
                produces Recorded
                  for resource
                  resource = resource
        """;

    protected const string SecondCommand = """

            slice StateChange Select
              command Select
                selection SelectionId identifier
                produces Recorded
                  for selection
        """;

    protected static Diagnostic[] Warnings(string source) => [.. new ScreenplayCompiler().Compile(source).Diagnostics
        .Where(diagnostic => diagnostic.Code == DiagnosticCodes.ReducerIdentityMismatch)];
}

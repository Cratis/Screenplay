// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    // Every ESM v37 member: a configurable application zone, a declared context member, the three calendar forms,
    // today and now with day, hour and minute offsets, date of on both sides, a clock validation rule and given time zone.
    internal const string ClockSource = """
        time zone configurable default "Europe/Oslo"
        context
          billingZone time zone
        module M
          feature F
            slice StateChange S
              event Done
              command Schedule
                id Uuid identifier
                dueDate Date
                occurred DateTime
                calendar "America/St_Johns"
                validate
                  dueDate >= today
                  require dueDate >= today - 7 days
                produces when date of occurred <= date of now + 2 hours
                  Done
                    for id
              command Remind
                id Uuid identifier
                occurred DateTime
                calendar from $context.timeZone
                validate
                  require occurred > now - 30 minutes
              command Bill
                id Uuid identifier
                dueDate Date
                calendar from $context.billingZone
                validate
                  require dueDate == today
                produces Done
                  for id
              specification InOslo
                given time zone "Europe/Oslo"
                given clock "2025-03-02T10:00:00Z"
                when Bill
                  id = "6f1c2b0e-8d4f-4a3b-9c5e-1a2b3c4d5e6f"
                  dueDate = "2025-03-02"
                then Done
        """;

    public static byte[] EsmV37Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v37.json");

    public static ExecutableSemanticModel CreateSemanticModelV37()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", ClockSource);
        var result = new SemanticModelBinder(2026).Bind("Billing", new ScreenplayCompiler().Parse(ClockSource).Value!, SemanticDocumentSet.Create([document], catalog));
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value!.Model;
    }
}
#endif

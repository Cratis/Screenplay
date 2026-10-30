// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler.given;

public class security_documents : documents
{
    protected const string Policy = "policy Access\n  require claim \"department\" matches unknown\n";
    protected const string Authorized = "module M\n  feature F\n    slice StateChange Write\n      command Save\n        authorize Access\n    slice StateView Lookup\n      readmodel View\n        id String key\n      query Get => View optional\n        by id String\n        authorize Access\n    slice StateChange Kept\n";
    protected const string Protected = "command reads protected\nmodule M\n  feature F\n    slice StateChange Write\n      event Recorded\n        value String\n      command Update\n        id String identifier\n        value String\n        reads View by id\n        produces Recorded\n          for id\n          value = value\n    slice StateView Lookup\n      readmodel View\n        id String key\n        value String\n      projection Build => View\n        from Recorded\n          id = $eventSourceId\n          value = value\n      query Get => View optional\n        by id String\n    slice StateChange Inspect\n      command Inspect\n        id String identifier\n        reads View by id\n    slice StateChange Kept\n";

    protected static string UnnamedVariantProjection => Protected.Replace("      projection Build => View\n        from Recorded\n          id = $eventSourceId\n          value = value", "      projection Collections\n        variant View\n          enters on Recorded\n          from Recorded\n            id = $eventSourceId\n            value = value", StringComparison.Ordinal);

    protected static string CompositeProtected => Protected
        .Replace("reads View by id", "reads View\n          by\n            id = id\n            value = id", StringComparison.Ordinal)
        .Replace("        value String\n      projection", "        value String key\n      projection", StringComparison.Ordinal)
        .Replace("          id = $eventSourceId\n          value = value", "          key\n            id = $eventSourceId\n            value = value", StringComparison.Ordinal)
        .Replace("query Get => View optional\n        by id String", "query Get => View[]", StringComparison.Ordinal);

    protected static string AdmittedProtected => Protected
        .Replace("      command Update\n", "      event Removed\n      command Update\n", StringComparison.Ordinal)
        .Replace("      query Get =>", "        remove with Removed\n      query Get =>", StringComparison.Ordinal);
}

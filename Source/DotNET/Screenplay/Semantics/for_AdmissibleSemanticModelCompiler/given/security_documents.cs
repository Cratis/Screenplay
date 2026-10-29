// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler.given;

public class security_documents : documents
{
    protected const string Policy = "policy Access\n  require claim \"department\" matches unknown\n";
    protected const string Authorized = "module M\n  feature F\n    slice StateChange Write\n      command Save\n        authorize Access\n    slice StateView Lookup\n      readmodel View\n        id String key\n      query Get => View optional\n        by id String\n        authorize Access\n    slice StateChange Kept\n";
    protected const string Protected = "command reads protected\nmodule M\n  feature F\n    slice StateChange Write\n      event Recorded\n        value String\n      command Update\n        id String identifier\n        value String\n        reads View by id\n        produces Recorded\n          for id\n          value = value\n    slice StateView Lookup\n      readmodel View\n        id String key\n        value String\n      projection Build => View\n        from Recorded\n          id = $eventSourceId\n          value = value\n      query Get => View optional\n        by id String\n    slice StateChange Inspect\n      command Inspect\n        id String identifier\n        reads View by id\n    slice StateChange Kept\n";
}

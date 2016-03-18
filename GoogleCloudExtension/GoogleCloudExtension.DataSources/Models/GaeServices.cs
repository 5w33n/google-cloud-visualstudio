﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    internal class GaeServices
    {
        [JsonProperty("nextPageToken")]
        public string NextPageToken { get; internal set; }

        [JsonProperty("services")]
        public IEnumerable<GaeService> Services { get; internal set; }
    }
}

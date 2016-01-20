﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal static class GcsDataSource
    {
        private const string ListBucketsUrl = "http://www.googleapis.com/storage/v1/b";

        internal static async Task<IList<Bucket>> GetBucketListAsync(string projectId)
        {
            var oauthToken = await GCloudWrapper.Instance.GetAccessTokenAsync();

            try
            {
                var client = new WebClient();
                var url = $"https://www.googleapis.com/storage/v1/b?project={projectId}&access_token={oauthToken}";
                var content = await client.DownloadStringTaskAsync(url);

                var buckets = JsonConvert.DeserializeObject<Buckets>(content);
                return buckets.Items;
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Failed to download data: {ex.Message}");
            }
            return null;

            //var client = new HttpClient();
            //var url = new Uri(ListBucketsUrl, dontEscape: true);
            //var request = new HttpRequestMessage(HttpMethod.Get, url);
            //request.Headers.Add("Authorization", $"Bearer {oauthToken}");

            //var response = await client.SendAsync(request);
            //var content = await response.Content.ReadAsStringAsync();
        }
    }
}
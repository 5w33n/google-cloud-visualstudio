﻿using GoogleCloudExtension.GCloud;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceDataSource
    {
        internal static async Task<IList<GceInstance>> GetInstanceListAsync(string projectId)
        {
            var accessToken = await GCloudWrapper.Instance.GetAccessTokenAsync();
            try
            {
                var client = new WebClient();
                var zones = await GetZoneListAsync(client, projectId, accessToken);

                var result = new List<GceInstance>();
                foreach (var zone in zones)
                {
                    var url = $"https://www.googleapis.com/compute/v1/projects/{projectId}/zones/{zone.Name}/instances?access_token={accessToken}";
                    var content = await client.DownloadStringTaskAsync(url);
                    var instances = JsonConvert.DeserializeObject<GceInstances>(content);
                    if (instances.Items != null)
                    {
                        foreach (var instance in instances.Items)
                        {
                            instance.ZoneName = zone.Name;
                            result.Add(instance);
                        }
                    }
                }
                return result;
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Failed to download data: {ex.Message}");
            }
            return null;
        }

        private static async Task<IList<Zone>> GetZoneListAsync(WebClient client, string projectId, string accessToken)
        {
            try
            {
                var url = $"https://www.googleapis.com/compute/v1/projects/{projectId}/zones?access_token={accessToken}";
                var content = await client.DownloadStringTaskAsync(url);

                var zones = JsonConvert.DeserializeObject<Zones>(content);
                return zones.Items;
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Failed to download list of zone: {ex.Message}");
            }
            return null;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GceVMsLoadedEvent
    {
        private const string GceVMsLoadedEventName = "gceVMsLoad";

        public static void Report()
        {
            EventsReporterWrapper.ReportEvent(GceVMsLoadedEventName);
        }
    }
}

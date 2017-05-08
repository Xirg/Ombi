﻿using Microsoft.Extensions.Logging;

namespace Ombi.Helpers
{
    public class LoggingEvents
    {
        public static EventId ApiException => new EventId(1000);
        public static EventId CacherException => new EventId(2000);
    }
}
﻿using Asp.Versioning;

namespace WebApplicationDemo.Configuration
{
    public class ApiVersions
    {
        public static readonly ApiVersion V1 = new(1, 0);
        public static readonly ApiVersion V2 = new(2, 0);

        public static readonly ApiVersion[] All = new ApiVersion[]
        {
            V1,
            V2
        };
    }
}

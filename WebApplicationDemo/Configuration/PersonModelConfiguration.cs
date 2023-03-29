﻿using Asp.Versioning.OData;
using Asp.Versioning;
using Microsoft.OData.ModelBuilder;
using WebApplicationDemo.Dto;

namespace WebApplicationDemo.Configuration
{
    public class PersonModelConfiguration : IModelConfiguration
    {
        public void Apply(ODataModelBuilder builder, ApiVersion apiVersion, string? routePrefix)
        {
            var person = builder.EntitySet<Person>("People").EntityType;

            person.HasKey(p => p.Id);
            person.Select().OrderBy("firstName", "lastName").Filter();

            if(apiVersion < ApiVersions.V2)
            {
                person.Ignore(p => p.Phone);
            }
        }
    }
}

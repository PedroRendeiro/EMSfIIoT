using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EMSfIIoT_API.Filters
{
    public class AuthenticationRequirementOperationsFilter : IOperationFilter
    {
        private readonly bool _includeUnauthorizedAndForbiddenResponses;

        public AuthenticationRequirementOperationsFilter(bool includeUnauthorizedAndForbiddenResponses = true)
        {
            _includeUnauthorizedAndForbiddenResponses = includeUnauthorizedAndForbiddenResponses;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            List<string> scopes = new List<string>();
            var requiredScopes = context.MethodInfo.GetCustomAttribute<AuthorizeAttribute>();

            if (requiredScopes != null)
            {
                scopes = requiredScopes.Policy.Split(' ').ToList();
            }

            var authorizeAttributes = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<AuthorizeAttribute>();

            if (authorizeAttributes.Count() > 0)
            {
                foreach (var authorizeAttribute in authorizeAttributes)
                {
                    if (authorizeAttribute.AuthenticationSchemes != null)
                    {
                        var scheme = new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = authorizeAttribute.AuthenticationSchemes } };
                        operation.Security.Add(new OpenApiSecurityRequirement
                        {
                            [scheme] = scopes
                        });

                        if (_includeUnauthorizedAndForbiddenResponses)
                        {
                            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
                            operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
                        }
                    }
                }
            }
        }
    }
}

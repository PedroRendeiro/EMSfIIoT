using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMSfIIoT_API.Filters
{
    public class DefaultResponseOperationsFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation openApiOperation, OperationFilterContext operationFilterContext)
        {
            openApiOperation.Responses.ToList().ForEach(operation => {
                if (operation.Value.Description.Equals("Success"))
                {
                    openApiOperation.Responses.Remove(operation.Key);
                }
            });
        }
    }
}

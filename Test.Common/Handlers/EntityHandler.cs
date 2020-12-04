using Microsoft.Xrm.Sdk;

namespace Test.Common.Handlers
{
    public abstract class EntityHandler
    {
        protected IOrganizationService Service { get; }
        protected ITracingService TracingService { get; }

        protected EntityHandler(IOrganizationService service, ITracingService tracingService)
        {
            Service = service;
            TracingService = tracingService;
        }
    }
}

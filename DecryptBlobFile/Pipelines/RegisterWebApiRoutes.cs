using System.Web.Mvc;
using System.Web.Routing;
using Sitecore.Pipelines;

namespace DecryptBlobFile.Pipelines
{
    public class RegisterWebApiRoutes
    {
        public void Process(PipelineArgs args)
        {
            RouteTable.Routes.MapRoute("DecryptBlobFile.Controllers", "api/decryptblobfile/{action}/{filename}", new
            {
                controller = "Decrypt"
            });
        }
    }
}
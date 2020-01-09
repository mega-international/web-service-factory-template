using log4net.Config;
using System.Web;
using System.Web.Http;

namespace Mega.WebServiceTemplate1
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            XmlConfigurator.Configure();
        }
    }
}
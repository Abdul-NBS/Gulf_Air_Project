using Autofac;
using Autofac.Integration.Mvc;
using eBriefingWebApp.Interfaces;
using eBriefingWebApp.Models;
using System;
using System.Globalization;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace eBriefingWebApp
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var builder = new ContainerBuilder();

            // Register your MVC controllers.
            builder.RegisterControllers(typeof(MvcApplication).Assembly);

            // Register other dependencies
            builder.RegisterType<GetGraphUserService>().As<IGetGraphUserService>();

            // Build and set the dependency resolver.
            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));


            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
            CultureInfo newCulture = new CultureInfo("en-GB");
            System.Threading.Thread.CurrentThread.CurrentCulture = newCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = newCulture;
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();
            Response.Clear();

            HttpException httpException = exception as HttpException;

            if (httpException != null)
            {
                string action;

                switch (httpException.GetHttpCode())
                {
                    case 404:
                        action = "NotFound";
                        break;
                    case 500:
                        action = "ServerError";
                        break;
                    default:
                        action = "General";
                        break;
                }

                Server.ClearError();
                Response.Redirect(String.Format("../Error/{0}", action));
            }
        }

    }
}

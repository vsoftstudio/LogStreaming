using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;

namespace LogStreaming.Server.vNext
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
            app.UseServices(services =>
            {
                services.AddMvc();
                services.AddSignalR();
            });

            app.UseSignalR();
            app.UseFileServer();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    null,
                    "{controller}/{action}",
                    new { controller = "Home", action = "Index" });

                routes.MapRoute(
                    null,
                    "api/{controller}/{action}",
                    new { controller = "Home", action = "Index" });

                routes.MapRoute(
                    null,
                    "api/{controller}",
                    new { controller = "Home" });
            });
        }
    }
}

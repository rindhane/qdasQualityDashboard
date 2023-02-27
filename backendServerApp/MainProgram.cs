using Microsoft.AspNetCore.Builder; //for Api:WebApplication provider
using App.Configurations; //container of all configurations for applications
using Microsoft.Extensions.Configuration; //to access the AddJsonfile extension
using BackgroundHostingService; // to run the application as background service;
using App.Routings; // to publish the routes to app


namespace WebService 
{
    class MainWebService {
        public static void Main (string[] args)
        {
            System.Console.WriteLine("Loading the configurations");
            //creating the host configuration options
            var appConfigs=new SystemConfigurations ();
            var builder = WebApplication.CreateBuilder(appConfigs.options);
            //capturing the confinguration for the same
            builder.Host.ConfigureAppConfiguration((hostingContext,config)=>{
                config.AddJsonFile(appConfigs.configFile, 
                                    optional:true,
                                    reloadOnChange:true);
            });
            //optional & critical : 
                // to run the application as background service
                builder.RunAsWindowService();
            //configuring Server Options : 
            /* //Presently not using http2 , this function is kept for future use
            builder.WebHost.ConfigureKestrel((context, serverOptions)=>{
                serverOptions.ListenAnyIP(5001, listenOptions =>{
                    listenOptions.Protocols=HttpProtocols.Http1AndHttp2;
                });
            });
            */
            //getting the builder ready;
                //Adding Services
                //builder.AddServices(appConfigs);
                //builder.Configuration
                var app = builder.Build();
            //
            //Configuring the app
            //Attaching the working port
            app=app.AddListentingPort(); //from the Configuration Namespace
            // Enable to add custome MiddleWare in the processing pipelines 
            //app.AddCustomMiddleware(); 
            //enable CORS 
            //app.UseCors();
            //enabled the serving staticfiles to fetch image/media files 
            app.UseStaticFiles();
            // add URL endpoints 
            app.AddRouting();
            //start the worker service
            app.Run();        
        }
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration; 
using Microsoft.Extensions.DependencyInjection;

namespace App.Configurations
{
    
    public class SystemConfigurations {

        public WebApplicationOptions options{get;}

        public string configFile {get;}
        public string resultFile {get;}
        public string dataFile {get;}

        public string qdasConfig {get;}

        public string jwt_url {get;}


        public SystemConfigurations () 
        {
            options= new WebApplicationOptions 
            {
                WebRootPath = "wwwroot"
            };
            configFile = "ServerConfig.json";
            resultFile = "result.dat";
            dataFile="DataStorage/data.json";
            qdasConfig=System.AppDomain.CurrentDomain.BaseDirectory+"QdasConfig.toml"; // BaseDirectory usage become essential during running webservice as window's background service 
            //read about in the ref here : 
                //https://stackoverflow.com/questions/2714262/relative-path-issue-with-net-windows-service;
                //https://haacked.com/archive/2004/06/29/current-directory-for-windows-service-is-not-what-you-expect.aspx/
            jwt_url=System.Environment.GetEnvironmentVariable("JWT_URL")!;
            //System.Environment.GetEnvironmentVariable("JWT_TEST_URL",System.EnvironmentVariableTarget.User);
            if(jwt_url==null){
                jwt_url="Jwt";
            }
        }
    }

    public static class ExtensionMethods {
        public static WebApplication AddListentingPort(this WebApplication app) {
            
            //get values from the Environment Variable [replaced with reading from IConfigurator]
            //string? httpPort = Environment.GetEnvironmentVariable("httpPort");           
            string? httpPort = app.Services.CreateScope()
                            .ServiceProvider
                            .GetRequiredService<IConfiguration>()["Configs:AppPort-http"];
            app.Urls.Add($"http://0.0.0.0:{httpPort}");
            //string? httpsPort=Environment.GetEnvironmentVariable("httpsPort");
            string? httpsPort = app.Services.CreateScope()
                            .ServiceProvider
                            .GetRequiredService<IConfiguration>()["Configs:AppPort-https"];
            //app.Urls.Add($"https://0.0.0.0:{httpsPort}"); 
            return app;
        }  
    }

    public class runTimeConfiguration{

        IConfigurationRoot Configuration;

        string configFile;
        public string jwt_url {get;}

        public runTimeConfiguration() {
            var systemConfiguration = new SystemConfigurations ();
            configFile = systemConfiguration.configFile;
            Configuration=new ConfigurationBuilder().AddJsonFile(configFile,false,true).Build();
            jwt_url=systemConfiguration.jwt_url;
        }

        public string getTokenHandlerSecret(){

            return Configuration["TokenHandlerSecret"]!;

        }



    }

}
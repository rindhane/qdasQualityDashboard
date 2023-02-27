using Microsoft.Extensions.Hosting; //to get access to extension UseWindowsService
using Microsoft.AspNetCore.Builder; //to get access to class WebApplicationBuilder;
using Microsoft.Extensions.Logging.EventLog; // to get access to EventLogSettings, EventLogLoggerProvider;
using Microsoft.Extensions.Logging.Configuration; // to get access LoggerProviderOptions;
using Microsoft.Extensions.Logging; // to use the AddConfiguration extension to logging;


namespace BackgroundHostingService{

public static class WindowBackgroundExtension{

    public static WebApplicationBuilder RunAsWindowService(this WebApplicationBuilder builder){
        //ref : 
            // https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service
        builder.Host.UseWindowsService(options =>
                                        {
                                            options.ServiceName = "QdasT";
                                        })
                                        .ConfigureServices(services =>
                                         {
                                           LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);

                                        })
                                        .ConfigureLogging((context, logging) =>
                                        {
                                            // See: https://github.com/dotnet/runtime/issues/47303
                                            logging.AddConfiguration(
                                                context.Configuration.GetSection("Logging"));
                                        });
        return builder;
    }
}

}
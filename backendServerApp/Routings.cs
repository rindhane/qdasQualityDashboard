using Microsoft.AspNetCore.Builder; //for classType:WebApplication 
using App.RouteBindings;
using App.Configurations;

namespace App.Routings
{
public static class Routes{
    public static WebApplication AddRouting(this WebApplication app){
        var configs = new SystemConfigurations();
        //add key-link-reRerouting
        app.MapGet("/", RouteMethods.MoveToHomeScreen);
        app.MapGet("/Home",RouteMethods.pageRedirect);
        //
        
        return app;
    }
}  
}
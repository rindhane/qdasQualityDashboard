using Microsoft.AspNetCore.Http; //to Use Results,IResult Type
using App.Configurations; // to access the configurations

namespace App.RouteBindings
{
  public static class RouteMethods{
    public static IResult IndexMethod(){
      return Results.LocalRedirect("~/index.html",false,true);
    }
    public static IResult pageRedirect(HttpRequest request){
      return Results.LocalRedirect($"~{request.Path}/index.html",false,true);
    }
    public static IResult pageRedirectWithParams(HttpRequest request){
      var param="serialNum";
      var val = request.Query[$"{param}"];
      return Results.LocalRedirect($"~{request.Path}/index.html?{param}={val}",false,true);
    }

    public static IResult MoveToHomeScreen(HttpRequest request){
      return Results.LocalRedirect("~/Home",false,true);
    }

  } 
}
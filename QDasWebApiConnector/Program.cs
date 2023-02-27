using ServiceReference;

namespace WebApiConnector
{
    public class QdassWebConnector {
        //pending stuff: instead of hardcoded server name, input through environment variables into System.ServiceModel.EndpointAddress GetEndpointAddress in the serviceReference: 
        static void Main(string[] args)
        {
            System.Console.WriteLine("Hello, World!");
            WebConnectRequest request= new WebConnectRequest(20, 44, "superuser", "superuser", "");
            Qdas_Web_ServiceClient ws = new Qdas_Web_ServiceClient();
            WebConnectResponse response = ws.WebConnectAsync(request).GetAwaiter().GetResult();
            System.Type myType= response.GetType();
            System.Console.WriteLine(ws.Endpoint.Name);
            System.Console.WriteLine(response.Result);
            WebDisconnectResponse res= ws.WebDisconnectAsync(response.Handle).GetAwaiter().GetResult();
            System.Console.WriteLine(res.Result);
            System.Console.ReadLine();
        }
    }
}


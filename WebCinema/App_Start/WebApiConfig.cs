using System.Web.Http;
using Newtonsoft.Json;

namespace WebCinema
{
    public class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // ✅ Enable Attribute Routing
            config.MapHttpAttributeRoutes();

            // ✅ Default Web API route
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // ✅ JSON formatter configuration
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            json.SerializerSettings.Formatting = Formatting.Indented;
            json.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            
            // ✅ IMPORTANT: Add support for application/x-www-form-urlencoded and application/json
            json.SupportedMediaTypes.Add(new System.Net.Http.MediaTypeHeaderValue("text/html"));
            json.SupportedMediaTypes.Add(new System.Net.Http.MediaTypeHeaderValue("application/json"));

            // ✅ Remove XML formatter
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // ✅ Add exception handler
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
        }
    }
}

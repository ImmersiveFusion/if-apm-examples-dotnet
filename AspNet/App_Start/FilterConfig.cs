using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IF.APM.OpenTelemetry.Attributes.AspNet.Mvc;

namespace Examples.AspNet
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new ControllerActionActivityFilter());
        }
    }
}

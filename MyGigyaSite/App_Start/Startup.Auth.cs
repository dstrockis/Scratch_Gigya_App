using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGigyaSite
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
				AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
				LoginPath = new PathString("/Account/SignIn"),
            });
        }
    }
}

using MyGigyaSite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security;
using Gigya.Socialize.SDK;
using System.Configuration;

namespace MyGigyaSite.Controllers
{
    public class AccountController : Controller
    {
        // See http://developers.gigya.com/010_Developer_Guide/10_UM360/030_Social_Login

        private string apiKey = ConfigurationManager.AppSettings["gigya:APIKey"];
        private string secretKey = ConfigurationManager.AppSettings["gigya:SecretKey"];

        [HttpGet]
        public ActionResult SignIn()
        {
            if (!HttpContext.User.Identity.IsAuthenticated) 
            {
                ViewBag.ShowPassword = false;
                return View();
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult SignIn(AppUser user, bool passwordShown)
        {
            if (passwordShown == null)
                return View("Error");

            if (HttpContext.User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            UserDbContext db = new UserDbContext();
            List<AppUser> existingUsers = db.Users.Where(u => u.Email.Equals(user.Email)).ToList();

            if (passwordShown == false)
            {
                if (existingUsers.Count == 0)
                {
                    ViewBag.Email = user.Email;
                    return View("Register");
                }
                else if (existingUsers[0].Password == null)
                {
                    ViewBag.Email = user.Email;
                    ViewBag.Exists = "true";
                    return View("Register");
                }
                else
                {
                    ViewBag.Email = user.Email;
                    ViewBag.ShowPassword = true;
                    return View();
                }
            }

            if (existingUsers.Count == 0)
            {
                ViewBag.Email = user.Email;
                ViewBag.Error = "Email not found.  Click 'Sign In' to Register.";
                ViewBag.ShowPassword = passwordShown;
                return View();
            }
            if (existingUsers[0].Password == null)
            {
                ViewBag.Email = user.Email;
                ViewBag.Error = "An account with this email exists, but does not have a local login.  Click 'Sign In' to Register or Login with a social account.'";
                ViewBag.ShowPassword = passwordShown;
                return View();
            }
            if (existingUsers[0].Password != user.Password)
            {
                ViewBag.Email = user.Email;
                ViewBag.Error = "Invalid Password.";
                ViewBag.ShowPassword = passwordShown;
                return View();
            }

            // Gigya Social Login Synchronize with Gigya Service
            GSRequest request = new GSRequest(apiKey, secretKey, "socialize.notifyLogin", true);
            request.SetParam("siteUID", existingUsers[0].AppUserID);
            GSResponse response = request.Send();
            if (response.GetErrorCode() != 0)
                return View("Error");
            Response.SetCookie(new HttpCookie(response.GetString("cookieName", ""), response.GetString("cookieValue", "")));

            HttpContext.GetOwinContext().Authentication.SignIn(CreateClaimsIdentity(existingUsers[0]));

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult SocialSignIn(AppUser user)
        {
            if (HttpContext.User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            UserDbContext db = new UserDbContext();
            List<AppUser> existingUsers = db.Users.Where(u => u.AppUserID.Equals(user.AppUserID)).ToList();

            // Gigya Social Login Step 4
            if (existingUsers.Count == 0)
            {
                // Gigya Social Login Step 6
                if (user.Email == null) 
                {
                    ViewBag.AppUserID = user.AppUserID;
                    return View("MissingFields");
                }

                // Gigya Social Login Step 7
                existingUsers = db.Users.Where(u => u.Email.Equals(user.Email)).ToList();

                if (existingUsers.Count == 0)
                {
                    // Gigya Social Login Step 9
                    AppUser newUser = new AppUser
                    {
                        AppUserID = Guid.NewGuid().ToString(),
                        Email = user.Email,
                    };
                    existingUsers.Add(newUser);
                    db.Users.Add(newUser);
                    db.SaveChanges();

                    // Gigya Social Login Step 10
                    GSRequest request = new GSRequest(apiKey, secretKey, "socialize.notifyRegistration", true);
                    request.SetParam("UID", user.AppUserID);
                    request.SetParam("siteUID", newUser.AppUserID);
                    GSResponse response = request.Send();
                    if (response.GetErrorCode() != 0)
                        return View("Error");
                }
                // Gigya Social Login Step 8.a
                else 
                {
                    // Gigya Social Login Step 9.a
                    // Gigya Social Login Step 10
                    GSRequest request = new GSRequest(apiKey, secretKey, "socialize.notifyRegistration", true);
                    request.SetParam("UID", user.AppUserID);
                    request.SetParam("siteUID", existingUsers[0].AppUserID);
                    GSResponse response = request.Send();
                    if (response.GetErrorCode() != 0)
                        return View("Error");                
                }
            }

            // Gigya Social Login Step 5
            HttpContext.GetOwinContext().Authentication.SignIn(CreateClaimsIdentity(existingUsers[0]));

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public ActionResult SignOut()
        {
            if (User.Identity.IsAuthenticated) 
            {   
                // Gigya Social Login Logging Out
                GSRequest request = new GSRequest(apiKey, secretKey, "socialize.logout", true);
                request.SetParam("UID", ClaimsPrincipal.Current.FindFirst(ClaimTypes.Sid).Value);
                GSResponse response = request.Send();
                if (response.GetErrorCode() != 0)
                    return View("Error");

                HttpContext.GetOwinContext().Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult Register(AppUser user)
        {
            UserDbContext db = new UserDbContext();
            List<AppUser> existingUsers = db.Users.Where(u => u.Email.Equals(user.Email)).ToList();

            // TODO: We would HAVE to perform immediate email verification

            if (existingUsers.Count > 0 && existingUsers[0].Password != null)
            {
                ViewBag.Email = user.Email;
                ViewBag.Error = "Local account already exists, please enter a different email.";
                return View("Register");
            }
            else if (existingUsers.Count > 0)
            {
                existingUsers[0].Password = user.Password;
                db.SaveChanges();
            }
            else
            {
                AppUser newUser = new AppUser
                {
                    AppUserID = Guid.NewGuid().ToString(),
                    Email = user.Email,
                    Password = user.Password
                };
            
                db.Users.Add(newUser);
                db.SaveChanges();
                existingUsers.Add(newUser);
            }

            // Gigya Social Login Synchronize with Gigya Service
            GSRequest request = new GSRequest(apiKey, secretKey, "socialize.notifyLogin", true);
            request.SetParam("siteUID", existingUsers[0].AppUserID);
            request.SetParam("newUser", true);
            GSResponse response = request.Send();
            if (response.GetErrorCode() != 0)
                return View("Error");
            Response.SetCookie(new HttpCookie(response.GetString("cookieName", ""), response.GetString("cookieValue", "")));

            HttpContext.GetOwinContext().Authentication.SignIn(CreateClaimsIdentity(existingUsers[0]));

            return RedirectToAction("Index", "Home");
        }

        private ClaimsIdentity CreateClaimsIdentity(AppUser user)
        {
            ClaimsIdentity id = new ClaimsIdentity(new List<Claim>(), CookieAuthenticationDefaults.AuthenticationType);
            id.AddClaim(new Claim(ClaimTypes.Email, user.Email, ClaimValueTypes.String, "MyGigyaSite"));
            id.AddClaim(new Claim(ClaimTypes.Sid, user.AppUserID, ClaimValueTypes.String, "MyGigyaSite"));
            return id;
        }
    }
}
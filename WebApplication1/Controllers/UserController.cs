using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace WebApplication1.Controllers
{
    public class UserController : Controller
    {
        //Regisration Action
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }

        //Registration POST Action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(Models.UserLogin login,string returnUrl) {
            string message = "";
            using (Models.Database1Entities1 dc = new Models.Database1Entities1())
            {
                var v = dc.Users.Where(a => a.EmailId == login.EmailId).FirstOrDefault();
                if (v != null)
                {
                    if (string.Compare(Hashing.hash(login.Password), v.Password) == 0)
                    {
                        int timeout = login.rememberMe ? 525600 : 20;
                        var ticket = new FormsAuthenticationTicket(login.EmailId,login.rememberMe,timeout);
                        string encrypted = FormsAuthentication.Encrypt(ticket);
                        var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted);
                        cookie.Expires = DateTime.Now.AddMinutes(timeout);
                        cookie.HttpOnly = true;
                        Response.Cookies.Add(cookie);
                        if (Url.IsLocalUrl(returnUrl)) {
                            return Redirect(returnUrl);
                        } else {
                            return RedirectToAction("Index","Home");
                        }

                    }
                    else {

                    }
                }
                else {
                    message = "Login or password is not correct";
                }
            }
                ViewBag.Message = message;
            return View();
        }

        //Logout
        [Authorize]
        [HttpPost]
        public ActionResult Logout() {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login","User");
        }

        public ActionResult Registration([Bind(Exclude ="IsEmailVerified,ActivationCode")] Models.Users user)
        {

            bool Status = false;
            string Message="";

            //Model Validation
            if (ModelState.IsValid)
            {


                #region //Email is already Exists
                    var isExist = IsEmailExist(user.EmailId);
                if (isExist) {
                    ModelState.AddModelError("EmailExist", "Email already exist");
                    return View(user);
                }
                #endregion

                #region //Generate Actiovation Code
                user.ActivationCode = Guid.NewGuid();
                #endregion

                #region //Password Hashing
                user.Password = Hashing.hash(user.Password);
                user.ConfirmPassword = Hashing.hash(user.ConfirmPassword);
                #endregion
                user.IsEmailVerified = false;

                #region  //Save data to DataBase
                using (Models.Database1Entities1 dc = new Models.Database1Entities1())
                {
                    dc.Users.Add(user);
                    dc.Configuration.ValidateOnSaveEnabled = false;
                    try
                    {
                        dc.SaveChanges();
                    }
                    catch (DbEntityValidationException e)
                    {
                        foreach (var eve in e.EntityValidationErrors)
                        {
                            Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                                eve.Entry.Entity.GetType().Name, eve.Entry.State);
                            foreach (var ve in eve.ValidationErrors)
                            {
                                Console.WriteLine("- Property: \"{0}\", Value: \"{1}\", Error: \"{2}\"",
                                    ve.PropertyName,
                                    eve.Entry.CurrentValues.GetValue<object>(ve.PropertyName),
                                    ve.ErrorMessage);
                            }
                        }
                        throw;
                    }
                        //Send Email to User
                        SendVerificationLinkEmail(user.EmailId, user.ActivationCode.ToString());
                    Message = "Registration successfully done!";
                    Status = true;
                }

                #endregion
            }

            else
            {
                Message = "Invalid request";
            }
            ViewBag.Message = Message;
            ViewBag.Status = Status;
            return View(user);
        }
        //Verifier email
        [HttpGet]
        public ActionResult VerifyAccount(string id)
        {
            bool Status = false;
            using (Models.Database1Entities1 dc = new Models.Database1Entities1())
            {
                dc.Configuration.ValidateOnSaveEnabled = false;
                var v = dc.Users.Where(a => a.ActivationCode == new Guid(id)).FirstOrDefault();
                if (v != null)
                {
                    v.IsEmailVerified = true;
                    dc.SaveChanges();
                    Status = true;
                }
                else
                {
                    ViewBag.Message = "Invalid Request";
                }

            }
            ViewBag.Status = Status;
            return View();
        }


        //Login
        [HttpGet]
        public ActionResult Login() {
            return View();
        }


        //Login POST
        [HttpPost]




        //Logout
        [NonAction]
        public bool IsEmailExist(String emailId)
        {
            using (Models.Database1Entities1 dc = new Models.Database1Entities1())
            {
                var v = dc.Users.Where(a => a.EmailId == emailId).FirstOrDefault();
                return v == null ? false : true;
            }
        }

        [NonAction]
        public void SendVerificationLinkEmail(String emailId,string activationCode) {

            var verifyUrl = "/User/VerifyAccount/" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

            var fromEmail = new MailAddress("denisglotov98@gmail.com","Sir Glotov");
            var toEmail = new MailAddress(emailId);
            var fromEmailPassword = "strongdweeb2307";
            string subject = "Your account is  created!";

            string body = "<br/><br/>Dear user. Please click on link below to verify you account<br/>" + 
                "<a href='" + link + "'>Click Here!</a>";
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
            };

            using (var message = new MailMessage(fromEmail, toEmail) {
                Subject = subject,
                Body = body,
                IsBodyHtml = true

            })

            smtp.Send(message);

            /*
            var schema = Request.Url.Scheme;
            var host = Request.Url.Host;
            var port = Request.Url.Port;

            string url = schema + "://" + host + ":" + port; 
            */
        }
    }
    
}
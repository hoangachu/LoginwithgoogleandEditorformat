﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LanguageNetwork.Controllers;
using LanguageNetwork.Models.Question;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MulLan.Models.User;
using Newtonsoft.Json;

namespace RoundTheCode.GoogleAuthentication.Controllers
{
    [AllowAnonymous, Route("account")]
    public class AccountController : Controller
    {

        public AccountController()
        {
            //context = Context;
        }
        [Route("google-login")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [Route("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            List<Claim> claims = new List<Claim>();

            var claim = result.Principal.Identities.FirstOrDefault()
                .Claims.Select(claim => new
                {
                    claim.Issuer,
                    claim.OriginalIssuer,
                    claim.Type,
                    claim.Value
                }).ToList();
            List<Claim> claimss = new List<Claim>();
            claim.ForEach(x => claimss.Add(new Claim(x.Type, x.Value)));
            ClaimsIdentity identity = new ClaimsIdentity(claims, "cookie");
            //create principle
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

            //sign-in
            await HttpContext.SignInAsync(
               scheme: "SecurityScheme",
               principal: claimsPrincipal,
               properties: new AuthenticationProperties()
               {
                   IsPersistent = true,
                   ExpiresUtc = DateTime.UtcNow.AddMinutes(100)

               }

           );

            return Redirect("/Account/ReponseMainPage");
        }
        [HttpPost]
        [Route("LoginUser")]
        public IActionResult LoginUser(string user, string password)
        {
            User User = new User();
            using (SqlConnection con = new SqlConnection(Startup.connectionString))
            {

                if (ModelState.IsValid)
                {
                    try
                    {
                        User = GetUserByUserName(user);
                        if (user == null)
                        {
                            return NoContent();
                        }
                        //List<Claim> claims = new List<Claim>()
                        //{
                        //    new Claim("Name",User.UserName),
                        //    new Claim("Password",User.Password)
                        //};
                        //// Create identity
                        //ClaimsIdentity identity = new ClaimsIdentity(claims, "cookie");
                        ////create principle
                        //ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

                        ////sign-in
                        //await HttpContext.SignInAsync(
                        //   scheme: "SecurityScheme",
                        //   principal: claimsPrincipal,
                        //   properties: new AuthenticationProperties()
                        //   {
                        //       IsPersistent = true,
                        //       ExpiresUtc = DateTime.UtcNow.AddMinutes(100)

                        //   }
                        SaveUserSession(User);

                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }

                }

                return Ok(new { data = 1, url = "/Account/MainPage" });
            }
        }
        public void SaveUserSession(User User)
        {
            // Lấy ISession
            var session = HttpContext.Session;
            string key_access = "access";

            // Lưu vào  Session thông tin truy cập
            // Định nghĩa cấu trúc dữ liệu lưu trong Session

            // Đọc chuỗi lưu trong Sessin với key = info_access

            string jsonSave = JsonConvert.SerializeObject(User);
            HttpContext.Session.SetString(key_access, jsonSave);

        }
        public User GetUserByUserName(string UserName)
        {
            User User = new User();
            using (SqlConnection con = new SqlConnection(Startup.connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetUserByUserName", con))
                {
                    {
                        try
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            con.Open();
                            cmd.Parameters.Add("@UserName", SqlDbType.VarChar).Value = UserName;

                            SqlDataReader dr = cmd.ExecuteReader();

                            while (dr.Read())
                            {
                                User.UserName = (string)dr["UserName"];
                                User.Password = (string)dr["Password"];
                                User.UserID = (int)dr["UserID"];
                                User.ProfileImg = dr.IsDBNull("ProfileImg") == true ? "" : (string)dr["ProfileImg"];
                                User.UserName = (string)dr["UserName"];
                                User.SeeLastestTime = dr.IsDBNull("SeeLastestTime") == true ? "" : (string)dr["SeeLastestTime"];
                                User.JoinToSystemTime = dr.IsDBNull("JoinToSystemTime") == true ? "" : (string)dr["JoinToSystemTime"];
                                User.Address = dr.IsDBNull("Address") == true ? "" : (string)dr["Address"]; 
                            }



                        }
                        catch (Exception e)
                        {
                            throw e;
                        }

                        con.Close();
                    }


                }
            }
            return User;
        }
        public User GetUserByUserID(int UserID)
        {
            User User = new User();
            using (SqlConnection con = new SqlConnection(Startup.connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetUserByUserID", con))
                {
                    {
                        try
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            con.Open();
                            cmd.Parameters.Add("@UserID", SqlDbType.VarChar).Value = UserID;

                            SqlDataReader dr = cmd.ExecuteReader();
                            while (dr.Read())
                            {
                                User.UserName = dr.IsDBNull("UserName") == true ? "" : (string)dr["UserName"]; 
                                User.Password = dr.IsDBNull("Password") == true ? "" : (string)dr["Password"]; 
                                User.UserID = dr.IsDBNull("UserID") == true ? 0 : (int)dr["UserID"];
                                User.ProfileImg = dr.IsDBNull("ProfileImg") == true ? "" : (string)dr["ProfileImg"]; 
                            }




                        }
                        catch (Exception e)
                        {
                            throw e;
                        }

                        con.Close();
                    }


                }
            }
            return User;
        }
        [Route("MainPage")]
        public ActionResult MainPage()
        {
            List<Question> listQuestion = new List<Question>();
            using (SqlConnection con = new SqlConnection(Startup.connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetListQuestion", con))
                {
                    {
                        try
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            con.Open();
                            SqlDataReader dr = cmd.ExecuteReader();

                            while (dr.Read())
                            {
                                Question question = new Question();
                                question.QuestionID = dr.IsDBNull("QuestionID") == true ? 0 : (int)dr["QuestionID"];
                                question.QuestionTitle = dr.IsDBNull("QuestionTitle") == true ? "" : (string)dr["QuestionTitle"]; 
                                question.QuestionBody = dr.IsDBNull("QuestionBody") == true ? "" : (string)dr["QuestionBody"]; 
                                question.QuestionTime = dr.IsDBNull("QuestionTime") == true ? null : (DateTime?)dr["QuestionTime"];

                                question.Vote = dr.IsDBNull("Vote") == true ? 0 : (int)dr["Vote"]; 
                                question.answer = dr.IsDBNull("Answer") == true ? 0 : (int)dr["Answer"]; 
                                question.view = dr.IsDBNull("Views") == true ? 0 : (int)dr["Views"]; 
                                listQuestion.Add(question);
                            }



                        }
                        catch (Exception e)
                        {
                            throw e;
                        }

                        con.Close();
                    }


                }
                return View(listQuestion);
            }

        }
    }
}


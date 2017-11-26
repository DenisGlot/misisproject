using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;



namespace WebApplication1
{
    public static class Hashing
    {
        public static string hash(string value) {
            return Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
    }
}
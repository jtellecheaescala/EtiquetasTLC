using System;

namespace Tecnologistica
{
    public class Login
    {
        private static Globals.staticValues gvalues = new Globals.staticValues();
        public bool checkUser(string user, string password)
        {
            if (gvalues.ValidUserPassword == null)
            {
                return true;
            }
            string[] credentials = null;
            credentials = gvalues.ValidUserPassword.Split(',');
            foreach (string credential in credentials)
            {
                if (user == credential.Split(';')[0] && password == credential.Split(';')[1])
                {
                    return true;
                }
            }
            return false;
        }

    }
}

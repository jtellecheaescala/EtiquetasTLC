using System;
using System.Net;
using System.Net.Mail;
using Microsoft.Exchange.WebServices.Data;

namespace Tecnologistica
{
    public class SendMailClass
    {
        private static Globals.staticValues gvalues = new Globals.staticValues();
        private static string mailMessage1 = null;
        private static string mailMessage2 = null;
        private static string mailMessage3 = null;
        public void SendMailLogs()
        {
            if (mailMessage1 != null) SendMail("Novedades de ejecucion de " + gvalues.NombreInterfazEnLogs + ":\r\n\r\n" + mailMessage1, gvalues.MailTo1, gvalues.MailCC1, gvalues.MailBCC1);
            if (mailMessage1 != null) SendMail("Novedades de ejecucion de " + gvalues.NombreInterfazEnLogs + ":\r\n\r\n" + mailMessage2, gvalues.MailTo2, gvalues.MailCC2, gvalues.MailBCC2);
            if (mailMessage1 != null) SendMail("Novedades de ejecucion de " + gvalues.NombreInterfazEnLogs + ":\r\n\r\n" + mailMessage3, gvalues.MailTo3, gvalues.MailCC3, gvalues.MailBCC3);
            mailMessage1 = null;
            mailMessage2 = null;
            mailMessage3 = null;
        }
        public void WriteMail(short index, string message)
        {
            switch (index)
            {
                case 1:
                    mailMessage1 += "\r\n" + message;
                    break;
                case 2:
                    mailMessage2 += "\r\n" + message;
                    break;
                case 3:
                    mailMessage3 += "\r\n" + message;
                    break;
            }
        }
        public bool SendMail(string message, string[] MailTo, string[] MailCC, string[] MailBCC)
        {
            if (gvalues.MailDomain == null || gvalues.MailDomain == string.Empty)
            {
                return SendMailSmtp(message, MailTo, MailCC, MailBCC);
            }
            else
            {
                return SendMailExchange(message, MailTo, MailCC, MailBCC);
            }
        }
        public bool SendMailSmtp(string message, string[] MailTo, string[] MailCC, string[] MailBCC)
        {
            try
            {
                MailMessage Email = new MailMessage();
                Email.From = new MailAddress(gvalues.MailFrom);
                Email.Subject = gvalues.MailSubject;
                Email.Body = message;
                foreach (var mail in MailTo)
                {
                    if (mail != "")
                    {
                        Email.To.Add(new MailAddress(mail));
                    }
                }
                foreach (var mail in MailCC)
                {
                    if (mail != "")
                    {
                        Email.CC.Add(new MailAddress(mail));
                    }
                }
                foreach (var mail in MailBCC)
                {
                    if (mail != "")
                    {
                        Email.Bcc.Add(new MailAddress(mail));
                    }
                }
                Email.IsBodyHtml = false;
                Email.Priority = MailPriority.High;

                SmtpClient smtp = new SmtpClient();
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Port = int.Parse(gvalues.MailPort);
                smtp.Host = gvalues.MailServer;
                smtp.UseDefaultCredentials = false;
                if (gvalues.MailUsername != null && gvalues.MailUsername != "")
                {
                    NetworkCredential credencial = new NetworkCredential(gvalues.MailUsername, gvalues.MailPassword);
                    smtp.Credentials = credencial;
                }
                if (gvalues.MailTLS == "true")
                {
                    smtp.EnableSsl = true;
                }
                try
                {
                    if (Email.To.Count > 0)     //MODIFICADO - GABRIEL SANCINETO - #2859
                        smtp.Send(Email);
                }
                catch (Exception)
                {
                    throw new Exception("Servidor de mails inalcanzable. Revise la configuracion en el archivo config");
                }
                return true;
            }
            catch (Exception e)
            {
                throw new Exception("Error no controlado: " + e.Message);
            }
        }
        public bool SendMailExchange(string message, string[] MailTo, string[] MailCC, string[] MailBCC)
        {
            try
            {
                ExchangeService service = new ExchangeService(GetExchVer());
                service.Credentials = new WebCredentials(gvalues.MailUsername, gvalues.MailPassword, gvalues.MailDomain);
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(ValidarCertificado);
                ServicePointManager.Expect100Continue = false;
                service.UseDefaultCredentials = false;
                service.Url = new Uri(gvalues.MailUrl);

                EmailMessage email = new EmailMessage(service);

                foreach (var mail in MailTo)
                {
                    if (mail != "")
                    {
                        email.ToRecipients.Add(mail);
                    }
                }
                foreach (var mail in MailCC)
                {
                    if (mail != "")
                    {
                        email.CcRecipients.Add(mail);
                    }
                }
                foreach (var mail in MailBCC)
                {
                    if (mail != "")
                    {
                        email.BccRecipients.Add(mail);
                    }
                }

                email.Subject = gvalues.MailSubject;
                email.Body = new MessageBody(message);
                try
                {
                    if (email.ToRecipients.Count > 0)
                        email.Send();
                }
                catch (Exception)
                {
                    throw new Exception("Servidor de mails inalcanzable. Revice la configuracion en el archivo config");
                }
                return true;
            }
            catch (Exception e)
            {
                throw new Exception("Error: " + e.Message);
            }
        }
        private bool ValidarCertificado(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        private ExchangeVersion GetExchVer()
        {
            ExchangeVersion actualExchVer = new ExchangeVersion();
            if (gvalues.MailExchVer == "Exchange2007_SP1")
            {
                actualExchVer = ExchangeVersion.Exchange2007_SP1;
            }
            else if (gvalues.MailExchVer == "Exchange2010")
            {
                actualExchVer = ExchangeVersion.Exchange2010;
            }
            else if (gvalues.MailExchVer == "Exchange2010_SP1")
            {
                actualExchVer = ExchangeVersion.Exchange2010_SP1;
            }
            else if (gvalues.MailExchVer == "Exchange2010_SP2")
            {
                actualExchVer = ExchangeVersion.Exchange2010_SP2;
            }
            else if (gvalues.MailExchVer == "Exchange2013")
            {
                actualExchVer = ExchangeVersion.Exchange2013;
            }
            //else if (gvalues.MailExchVer == "Exchange2013_SP1")
            //{
            //    actualExchVer = ExchangeVersion.Exchange2013_SP1;
            //}
            else
            {
                Console.WriteLine("Version de exchange server invalida, consultar lista de versiones validas en archivo de configuración.");
            }

            return actualExchVer;
        }
    }
}

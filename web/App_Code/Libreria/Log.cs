using System;
using System.Data;
using System.Data.SqlClient;

namespace Tecnologistica
{
    public class Log
    {
        private static Globals.staticValues gvalues = new Globals.staticValues();
        private static SendMailClass sendMail = new SendMailClass();
        internal static readonly string timeOutConfig = gvalues.QueryTimeOut;
        internal static int timeOutQueries = 30;

        //INICIO - NUEVO - GABRIEL SANCINETO - #2859
        public void GrabarLogs(SqlConnection connLog, int severidad, string texto, string textoExtra)
        {
            try
            {
                switch (GetModoLog(connLog))
                {
                    case "W":
                        LogWMS(connLog, severidad, texto, textoExtra);
                        break;
                    case "T":
                        LogTMS(connLog, severidad, texto, textoExtra);
                        break;
                    default:
                        Console.WriteLine("ERROR AL GRABAR LOGS: No se definió tipo de tabla. Comuníquese con soporte.");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR AL GRABAR LOGS: " + e.Message + ". Comuníquese con soporte.");
            }
        }
        //FIN - NUEVO - GABRIEL SANCINETO - #2859

        public void LogWMS(SqlConnection connLog, int severidad, string texto, string textoExtra)
        {
            try
            {
                texto = texto.Replace("'", "");
                textoExtra = textoExtra.Replace("'", "");
                Console.WriteLine(DateTime.Now.ToString("hh:mm:ss:fff") + ": " + (textoExtra == "" ? texto : texto + " - " + textoExtra));
                if (gvalues.severidadCheck(gvalues.Severidades, severidad))
                {
                    string log = "INSERT INTO INTERFACESLOG (Cod_Interfaz, FechaHora, Severidad, Texto, TextoExtra, Revisado) " +
                        "VALUES ('" + gvalues.NombreInterfazEnLogs + "', GETDATE(), " + severidad + ", '" +
                        texto.Substring(0, texto.Length > 250 ? 250 : texto.Length).Replace("'", "#") + "', '" + textoExtra.Replace("'", "#") + "', 0)";
                    SqlCommand cmdl = new SqlCommand(log, connLog);
                    //INICIO - MODIFICACION - GABRIEL SANCINETO - #2859
                    int resTimeOut = -1;
                    if (int.TryParse(timeOutConfig, out resTimeOut) && timeOutConfig != string.Empty && resTimeOut != -1)
                        timeOutQueries = resTimeOut;
                    cmdl.CommandTimeout = timeOutQueries;
                    //FIN - MODIFICACION - GABRIEL SANCINETO - #2859
                    cmdl.ExecuteNonQuery();
                }
                try
                {
                    if (gvalues.severidadCheck(gvalues.MailSeveridad1, severidad))
                    {
                        sendMail.WriteMail(1, DateTime.Now.ToString("hh:mm:ss") + ": " + (textoExtra == "" ? texto : texto + " - " + textoExtra));
                    }
                    if (gvalues.severidadCheck(gvalues.MailSeveridad2, severidad))
                    {
                        sendMail.WriteMail(2, DateTime.Now.ToString("hh:mm:ss") + ": " + (textoExtra == "" ? texto : texto + " - " + textoExtra));
                    }
                    if (gvalues.severidadCheck(gvalues.MailSeveridad2, severidad))
                    {
                        sendMail.WriteMail(3, DateTime.Now.ToString("hh:mm:ss") + ": " + (textoExtra == "" ? texto : texto + " - " + textoExtra));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("-Error: Un error ocurrio al intentar enviar un mail");
                    try
                    {
                        string log = "INSERT INTO INTERFACESLOG (Cod_Interfaz, FechaHora, Severidad, Texto, TextoExtra, Revisado) " +
                        "VALUES ('" + gvalues.NombreInterfazEnLogs + "', GETDATE(), " + 3 + ", 'Error envio mails', 'Error: " + ex.Message.Replace("'", "#") + "', 0)";
                        SqlCommand cmdl = new SqlCommand(log, connLog);
                        //INICIO - MODIFICACION - GABRIEL SANCINETO - #2859
                        int resTimeOut = -1;
                        if (int.TryParse(timeOutConfig, out resTimeOut) && timeOutConfig != string.Empty && resTimeOut != -1)
                            timeOutQueries = resTimeOut;
                        cmdl.CommandTimeout = timeOutQueries;
                        //FIN - MODIFICACION - GABRIEL SANCINETO - #2859
                        cmdl.ExecuteNonQuery();
                    }
                    catch (Exception)
                    {
                        throw new Exception();
                    }
                }
                return;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al intentar crear un registro en log de interfaces de WMS.");
            }
        }
        public void LogTMS(SqlConnection connLog, int severidad, string texto, string textoExtra)
        {
            try
            {
                texto = texto.Replace("'", "");
                textoExtra = textoExtra.Replace("'", "");
                Console.WriteLine(DateTime.Now.ToString("hh:mm:ss:fff") + ": " + (textoExtra == "" ? texto : texto + " - " + textoExtra));
                if (gvalues.severidadCheck(gvalues.Severidades, severidad))
                {
                    string log = "INSERT INTO Log_Eventos_Interfaces (IFAZ, FECHA, EVENTO) " +
                        "VALUES ('" + gvalues.NombreInterfazEnLogs + "', GETDATE() , '" +
                        (textoExtra == "" ? texto : texto + " - " + textoExtra) + "')";
                    SqlCommand cmdl = new SqlCommand(log, connLog);
                    //INICIO - MODIFICACION - GABRIEL SANCINETO - #2859
                    int resTimeOut = -1;
                    if (int.TryParse(timeOutConfig, out resTimeOut) && timeOutConfig != string.Empty && resTimeOut != -1)
                        timeOutQueries = resTimeOut;
                    cmdl.CommandTimeout = timeOutQueries;
                    //FIN - MODIFICACION - GABRIEL SANCINETO - #2859
                    cmdl.ExecuteNonQuery();
                }

                try
                {
                    if (gvalues.severidadCheck(gvalues.MailSeveridad1, severidad))
                    {
                        sendMail.WriteMail(1, DateTime.Now.ToString("hh:mm:ss") + ": " + (textoExtra == "" ? texto : texto + " - " + textoExtra));
                    }
                    if (gvalues.severidadCheck(gvalues.MailSeveridad2, severidad))
                    {
                        sendMail.WriteMail(2, DateTime.Now.ToString("hh:mm:ss") + ": " + (textoExtra == "" ? texto : texto + " - " + textoExtra));
                    }
                    if (gvalues.severidadCheck(gvalues.MailSeveridad2, severidad))
                    {
                        sendMail.WriteMail(3, DateTime.Now.ToString("hh:mm:ss") + ": " + (textoExtra == "" ? texto : texto + " - " + textoExtra));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("-Error: Un error ocurrio al intentar enviar un mail");
                    try
                    {
                        string log = "INSERT INTO Log_Eventos_Interfaces (IFAZ, FECHA, EVENTO) " +
                            "VALUES ('" + gvalues.NombreInterfazEnLogs + "', GETDATE() , '" +
                            "Error de mail: " + ex.Message + "')";
                        SqlCommand cmdl = new SqlCommand(log, connLog);
                        //INICIO - MODIFICACION - GABRIEL SANCINETO - #2859
                        int resTimeOut = -1;
                        if (int.TryParse(timeOutConfig, out resTimeOut) && timeOutConfig != string.Empty && resTimeOut != -1)
                            timeOutQueries = resTimeOut;
                        cmdl.CommandTimeout = timeOutQueries;
                        //FIN - MODIFICACION - GABRIEL SANCINETO - #2859
                        cmdl.ExecuteNonQuery();
                    }
                    catch (Exception)
                    {
                        throw new Exception();
                    }
                }

                return;
            }
            catch (Exception)
            {
                throw new Exception("Al intentar crear un registro en log de interfaces de TMS.");
            }
        }
        public void LogSL(SqlConnection connLog, int severidad, string texto, string textoExtra)
        {
            try
            {
                texto = texto.Replace("'", "");
                textoExtra = textoExtra.Replace("'", "");
                if (gvalues.severidadCheck(gvalues.Severidades, severidad))
                {
                    Console.WriteLine(DateTime.Now.ToString("hh:mm:ss:fff") + ": " + (textoExtra == "" ? texto : texto + " - " + textoExtra));
                    string log = "INSERT INTO INTERFACESLOG_SL (Cod_Interfaz, FechaHora, Severidad, Texto, TextoExtra, Revisado) " +
                        "VALUES ('" + gvalues.NombreInterfazEnLogs + "', GETDATE(), " + severidad + ", '" +
                        texto.Substring(0, texto.Length > 250 ? 250 : texto.Length).Replace("'", "#") + "', '" + textoExtra.Replace("'", "#") + "', 0)";
                    SqlCommand cmdl = new SqlCommand(log, connLog);
                    //INICIO - MODIFICACION - GABRIEL SANCINETO - #2859
                    int resTimeOut = -1;
                    if (int.TryParse(timeOutConfig, out resTimeOut) && timeOutConfig != string.Empty && resTimeOut != -1)
                        timeOutQueries = resTimeOut;
                    cmdl.CommandTimeout = timeOutQueries;
                    //FIN - MODIFICACION - GABRIEL SANCINETO - #2859
                    cmdl.ExecuteNonQuery();
                }
                return;
            }
            catch (Exception)
            {
                throw new Exception("Al intentar crear un registro en log de interfaces de SL.");
            }
        }
        /// <summary>
        /// SIRVE PARA SABER SI EL LOG SE GRABA EN WMS O TMS. 
        /// </summary>
        /// <param name="connWMS">Connection al WMS</param>
        /// <param name="connTMS">Connection al TMS</param>
        /// <returns>Devuelve W (WMS), T (TMS) o X (ninguno)</returns>
        //INICIO - NUEVO - GABRIEL SANCINETO - #2859
        internal string GetModoLog(SqlConnection conn)
        {
            string result = "X";

            string queryWMS = "SELECT COUNT(*) AS 'Return' FROM sysobjects WHERE type = 'U' AND name = 'INTERFACESLOG'";
            string queryTMS = "SELECT COUNT(*) AS 'Return' FROM sysobjects WHERE type = 'U' AND name = 'Log_Eventos_Interfaces'";
            int? cantFilas = 0;

            try
            {
                int resTimeOut = -1;
                if (int.TryParse(timeOutConfig, out resTimeOut) && timeOutConfig != string.Empty && resTimeOut != -1)
                    timeOutQueries = resTimeOut;
                using (SqlDataReader readerWMS = new SqlCommand
                {
                    CommandText = queryWMS,
                    CommandType = CommandType.Text,
                    CommandTimeout = timeOutQueries,
                    Connection = conn
                }.ExecuteReader())
                {
                    if (readerWMS.HasRows)
                    {
                        while (readerWMS.Read())
                            cantFilas = readerWMS["Return"] as int?;
                        if (cantFilas > 0)
                            result = "W";
                    }
                }

                if (result != "W")
                {
                    using (SqlDataReader readerWMS = new SqlCommand
                    {
                        CommandText = queryTMS,
                        CommandType = CommandType.Text,
                        CommandTimeout = timeOutQueries,
                        Connection = conn
                    }.ExecuteReader())
                    {
                        if (readerWMS.HasRows)
                        {
                            while (readerWMS.Read())
                                cantFilas = readerWMS["Return"] as int?;
                            if (cantFilas > 0)
                                result = "T";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR EN OBJETO GetModoLog: " + e.Message + ". Contacte a soporte.");
            }
            return result;
        }
        //FIN - NUEVO - GABRIEL SANCINETO - #2859

    }
}
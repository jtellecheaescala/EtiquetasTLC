using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Web.Services;
using Tecnologistica;
using ZXing;
using ZXing.Common;
using Models;
using System.Linq;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using iText.Layout.Borders;
using iText.IO.Image;

[WebService(Namespace = "http://www.tecnologisticaconsultores.com/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class WSEtiquetas : WebService
{
    private const string FORMAT_JPG = "jpg";
    private const string FORMAT_PDF = "pdf";

    private const string PAGE_SIZE_A4 = "A4";
    private const string PAGE_SIZE_ZEBRA = "ZEBRA";
    private PageSize SIZE_ZEBRA = new PageSize(285, 285);
    private const string TEMPLATE_VERTICAL = "BULTOSVERTICAL";
    private const string TEMPLATE_BULTOSXD = "BULTOSXD";
    private const string TEMPLATE_BULTOS_DHL_APPLE = "BULTOS_DHL_APPLE";
    private const string TEMPLATE_VIAJES_DHL_Apple = "VIAJES_DHL_APPLE";
    private List<String> TemplatesHabilitados = new List<string>() { TEMPLATE_VERTICAL, TEMPLATE_BULTOSXD, TEMPLATE_BULTOS_DHL_APPLE, TEMPLATE_VIAJES_DHL_Apple };

    private string MODO_OBTENCION_ARCHIVO;

    private static Globals.staticValues gvalues = new Globals.staticValues();
    private static Globals globals = new Globals();
    private static Globals.staticValues.SeveridadesClass severidades = new Globals.staticValues.SeveridadesClass();
    private static Login login = new Login();
    private static ConnectionSQL connectionSQL = new ConnectionSQL();
    private static SqlConnection connDB = connectionSQL.connect('D');
    private static SqlConnection connLog = connectionSQL.connect('L');
    private static Log log = new Log();
    private static QueriesSQL queriesSQL = new QueriesSQL();
    private static SendMailClass sendMail = new SendMailClass();
    internal static readonly string timeOutConfig = gvalues.QueryTimeOut;
    internal static int timeOutQueries = 30;    //default 30 seg

    [WebMethod]
    public Response EtiquetasWS(string user, string password, string IDRemito, string cliente, string format, string size, int separarPorDocumento, string template, string nroOperacion, string nroViaje)
    {
        Response response = new Response();
        if (connDB.State == ConnectionState.Closed) connDB.Open();
        if (connLog.State == ConnectionState.Closed) connLog.Open();

        log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Iniciado WS Etiquetas");

        try
        {
            #region Validaciones

            MODO_OBTENCION_ARCHIVO = gvalues.ModoObtencionArchivo.ToUpper();
            if (MODO_OBTENCION_ARCHIVO != "BASE64" && MODO_OBTENCION_ARCHIVO != "URL")
            {
                log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ERROR", "El Modo de grabación debe ser URL o BASE64");

                sendMail.SendMailLogs();

                if (connDB.State == ConnectionState.Open) connDB.Close();
                if (connLog.State == ConnectionState.Open) connLog.Close();
                response.message = "El Modo de grabación debe ser URL o BASE64";
                return response;
            }

            //Chequeo que los parametros no esten vacios
            if (user == string.Empty || password == string.Empty || IDRemito == string.Empty || cliente == string.Empty || format == string.Empty || size == string.Empty)
            {
                log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "Los parámetros de entrada no pueden estar vacíos. Revise el llamado al WS.");
                sendMail.SendMailLogs();
                if (connDB.State == ConnectionState.Open) connDB.Close();
                if (connLog.State == ConnectionState.Open) connLog.Close();
                response.message = "Los parámetros de entrada no pueden estar vacíos. Revise el llamado al WS.";
                return response;
            }

            if (gvalues.PathOut == string.Empty)
            {
                log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ERROR", "El path de salida no puede estar vacío. Revise el archivo .config.");
                sendMail.SendMailLogs();
                if (connDB.State == ConnectionState.Open) connDB.Close();
                if (connLog.State == ConnectionState.Open) connLog.Close();
                response.message = "El path de salida no puede estar vacío. Revise el archivo .config.";
                return response;
            }

            if (!login.checkUser(user, password))
            {
                log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ERROR", "Usuario o contraseña invalidos para WSEtiquetas. Revise config o parámetros enviados.");
                sendMail.SendMailLogs();
                if (connDB.State == ConnectionState.Open) connDB.Close();
                if (connLog.State == ConnectionState.Open) connLog.Close();
                response.message = "Usuario o contraseña invalidos";
                return response;
            }

            //Validaciones para el template BultosXD
            if(template.ToUpper() == TEMPLATE_BULTOSXD || template.ToUpper() == TEMPLATE_BULTOS_DHL_APPLE || template.ToUpper() == TEMPLATE_VIAJES_DHL_Apple)
            {
                #region Generacion de mensaje de error
                string msgError = null;

                if(format.ToUpper() != "PDF")
                {
                    msgError += "El template BultosXD solo admite el formato 'PDF'";
                }

                if (size.ToUpper() == "A4")
                {
                    if (msgError == null)
                    {
                        msgError += "El template BultosXD solo admite size 'Zebra'";
                    }
                    else
                    {
                        msgError += "; Solo se admite size 'Zebra'";
                    }
                }
                if (separarPorDocumento != 0 && separarPorDocumento != 1)
                {
                    if (msgError == null)
                    {
                        msgError += "Los valores posibles para SepararPorDocumento son '0' o '1'";
                    }
                    else
                    {
                        msgError += "; Los valores posibles para SepararPorDocumento son '0' o '1'";
                    }
                }
                #endregion

                if(msgError != null)
                {
                    log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ERROR", "El Modo de grabación debe ser URL o BASE64");

                    sendMail.SendMailLogs();

                    if (connDB.State == ConnectionState.Open) connDB.Close();
                    if (connLog.State == ConnectionState.Open) connLog.Close();
                    response.message = msgError;
                    return response;
                }
            }

            #region Conseguir timeout del config

            int resTimeOut = -1;
            if (int.TryParse(timeOutConfig, out resTimeOut) && timeOutConfig != string.Empty && resTimeOut != -1)
                timeOutQueries = resTimeOut;

            #endregion Conseguir timeout del config

            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Se loggeo " + user + ". Versión: " + gvalues.version);

            string chequeoUsuario = string.Empty;
            using (SqlDataReader sqlReader = new SqlCommand
            {
                CommandText = queriesSQL.sIf_Parametros,
                CommandType = CommandType.Text,
                CommandTimeout = timeOutQueries,
                Connection = connDB
            }.ExecuteReader())
            {
                if (sqlReader.HasRows)
                    while (sqlReader.Read())
                        chequeoUsuario = sqlReader["TOMADOR"].ToString().Trim();
                else
                    log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ALERTA", "No se encontro el parametro VALIDO_USUARIO_CLIENTE_WS_ETIQUETAS");
            }

            /** ACA SE CONSULTA EL USUARIO EN CASO DE QUE SI
             * Si los usuarios no coinciden se loguea y return
             **/
            //Se consulta que el usuario tenga asignado el cliente en la tabla Clientes_Usuario
            if (chequeoUsuario.ToUpper() == "S")
            {
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Se validara que el usuario " + user + " tenga asignado el cliente " + cliente);

                using (SqlDataReader sqlReader = new SqlCommand
                {
                    CommandText = queriesSQL.sClientes_Usuario,
                    CommandType = CommandType.Text,
                    CommandTimeout = timeOutQueries,
                    Connection = connDB,
                    Parameters =
                        {
                            new SqlParameter{ ParameterName = "@Usuario",SqlDbType = SqlDbType.VarChar,Value = user},
                            new SqlParameter{ ParameterName = "@Cod_Cliente",SqlDbType = SqlDbType.Int,Value = int.Parse(cliente)}
                        }
                }.ExecuteReader())
                {
                    if (sqlReader.HasRows)
                    {
                        while (sqlReader.Read())
                            if (sqlReader["Cantidad"].ToString().Trim() == "0")
                            {
                                log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ERROR", "El usuario " + user + " no tiene asignado el cliente " + cliente);
                                sendMail.SendMailLogs();
                                if (connDB.State == ConnectionState.Open) connDB.Close();
                                if (connLog.State == ConnectionState.Open) connLog.Close();
                                response.message = "ERROR: El usuario " + user + " no tiene asignado el cliente " + cliente;
                                return response;
                            }
                    }
                    else
                    {
                        log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo comprobar si el usuario " + user + " tiene asignado el cliente " + cliente);
                        sendMail.SendMailLogs();
                        if (connDB.State == ConnectionState.Open) connDB.Close();
                        if (connLog.State == ConnectionState.Open) connLog.Close();
                        response.message = "ERROR: El usuario " + user + " no existe en la tabla Usuarios";
                        return response;
                    }
                }
            }

            #endregion

            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Se consultará la QUERY principal para IDRemito: " + IDRemito + " - Cliente: " + cliente);

            #region Obtencion de datos
            string cantidadTotal = string.Empty;
            int etiquetasCount = 0;
            bool vertical = false;
            List<Remito> remitos = new List<Remito>();
            string[] RemitosIDS = IDRemito.Split('|');
            foreach (string sIDRemito in RemitosIDS)
            {
                if (string.Equals(template.ToUpper(), TEMPLATE_VERTICAL))
                {
                    vertical = true;
                    List<BultosVertical> lBultosVertical = new List<BultosVertical>();
                    using (SqlDataReader readerCantidad = new SqlCommand
                    {
                        Connection = connDB,
                        CommandType = CommandType.Text,
                        CommandTimeout = timeOutQueries,
                        CommandText = queriesSQL.sRemitos_Bultos,
                        Parameters =
                        {
                            new SqlParameter { ParameterName = "@ID_Remito", SqlDbType = SqlDbType.Int, Value = int.Parse(sIDRemito)}
                        }
                    }.ExecuteReader())
                    {
                        if (readerCantidad.HasRows)
                        {
                            while (readerCantidad.Read())
                            {
                                cantidadTotal = readerCantidad["Cantidad"].ToString().Trim();
                            }
                        }
                        else
                        {
                            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "Error buscando cantidad en Remitos_Bultos para IDRemito: " + sIDRemito + " - cliente: " + cliente);
                            if (connDB.State == ConnectionState.Open) connDB.Close();
                            if (connLog.State == ConnectionState.Open) connLog.Close();
                            response.message = "ERROR: Error buscando cantidad en Remitos_Bultos para IDRemito: " + sIDRemito + " - cliente: " + cliente;
                            return response;
                        }
                    }

                    using (SqlDataReader readerData = new SqlCommand
                    {
                        Connection = connDB,
                        CommandType = CommandType.Text,
                        CommandTimeout = timeOutQueries,
                        CommandText = queriesSQL.sRemitosVertical,
                        Parameters =
                        {
                            new SqlParameter {ParameterName = "@Cod_Cliente", SqlDbType = SqlDbType.Int, Value = int.Parse(cliente)},
                            new SqlParameter {ParameterName = "@Id_Remito", SqlDbType = SqlDbType.Int, Value = int.Parse(sIDRemito)}
                        }
                    }.ExecuteReader())
                    {
                        if (readerData.HasRows)
                        {
                            while (readerData.Read())
                            {
                                etiquetasCount++;

                                lBultosVertical.Add(new BultosVertical(
                                    readerData["TrackingNo"].ToString().Trim(),
                                    readerData["Origen"].ToString().Trim(),
                                    readerData["Origen_Ciudad"].ToString().Trim(),
                                    readerData["Origen_Direccion"].ToString().Trim(),
                                    readerData["Destino"].ToString().Trim(),
                                    readerData["Destino_Ciudad"].ToString().Trim(),
                                    readerData["Destino_Direccion"].ToString().Trim(),
                                    readerData["Destino_Contacto"].ToString().Trim(),
                                    readerData["Destino_Telefono"].ToString().Trim(),
                                    readerData["Cuenta"].ToString().Trim(),
                                    readerData["ModEnvio"].ToString().Trim(),
                                    string.Equals(cantidadTotal, "0") ? readerData["Bultos"].ToString().Trim() : cantidadTotal,
                                    string.Equals(cantidadTotal, "0") ? "" : Math.Round((decimal)readerData["Peso"], 2).ToString().Trim(),
                                    readerData["IdRemito"].ToString().Trim(),
                                    readerData["NroDcto"].ToString().Trim(),
                                    readerData["NroPedido"].ToString().Trim(),
                                    readerData["Conocimiento"].ToString().Trim(),
                                    readerData["Comentario"].ToString().Trim(),
                                    cliente
                                    ));
                            }

                            remitos.Add(new Remito(int.Parse(sIDRemito), lBultosVertical));
                        }
                        else
                        {
                            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "No se encontraron remitos disponibles para IDRemito: " + sIDRemito + " - cliente: " + cliente);
                            if (connDB.State == ConnectionState.Open) connDB.Close();
                            if (connLog.State == ConnectionState.Open) connLog.Close();
                            response.message = "Notificacion: No se encontraron remitos disponibles para IDRemito: " + sIDRemito + " - cliente: " + cliente;
                            return response;
                        }
                    }
                }
                else if (string.Equals(template.ToUpper(), TEMPLATE_BULTOSXD))
                {
                    using (SqlDataReader readerCantidad = new SqlCommand
                    {
                        Connection = connDB,
                        CommandType = CommandType.Text,
                        CommandTimeout = timeOutQueries,
                        CommandText = queriesSQL.cant_BultosXD,
                        Parameters =
                        {
                            new SqlParameter { ParameterName = "@IdRemito", SqlDbType = SqlDbType.Int, Value = int.Parse(sIDRemito)}
                        }
                    }.ExecuteReader())
                    {
                        if (readerCantidad.HasRows)
                        {
                            while (readerCantidad.Read())
                            {
                                cantidadTotal = StringHelper.LimpiarCampo(readerCantidad["Cantidad"]);
                            }
                        }
                        else
                        {
                            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "Error buscando cantidad en Remitos para IDRemito: " + sIDRemito + " - cliente: " + cliente);
                            if (connDB.State == ConnectionState.Open) connDB.Close();
                            if (connLog.State == ConnectionState.Open) connLog.Close();
                            response.message = "ERROR: Error buscando cantidad en Remitos para IDRemito: " + sIDRemito + " - cliente: " + cliente;
                            return response;
                        }
                    }

                    using (SqlDataReader readerData = new SqlCommand
                    {
                        Connection = connDB,
                        CommandType = CommandType.Text,
                        CommandTimeout = timeOutQueries,
                        CommandText = queriesSQL.sRemitosBultosXD,
                        Parameters =
                        {
                            new SqlParameter {ParameterName = "@CodCliente", SqlDbType = SqlDbType.Int, Value = int.Parse(cliente)},
                            new SqlParameter {ParameterName = "@IdRemito", SqlDbType = SqlDbType.Int, Value = int.Parse(sIDRemito)}
                        }
                    }.ExecuteReader())
                    {
                        if (readerData.HasRows)
                        {
                            while (readerData.Read())
                            {
                                etiquetasCount++;

                                BultosXD nBultoXD = new BultosXD();

                                nBultoXD.Origen = StringHelper.LimpiarCampo(readerData["Origen"]);
                                nBultoXD.Domicilio = StringHelper.LimpiarCampo(readerData["Domicilio"]);
                                nBultoXD.Localidad = StringHelper.LimpiarCampo(readerData["Localidad"]);
                                nBultoXD.Mail = StringHelper.LimpiarCampo(readerData["Mail"]);
                                nBultoXD.Url = StringHelper.LimpiarCampo(readerData["Url"]);
                                nBultoXD.Nro_seguimiento = StringHelper.LimpiarCampo(readerData["Nro_seguimiento"]);
                                nBultoXD.Fecha = StringHelper.LimpiarCampo(readerData["Fecha"]) != "" ? Convert.ToDateTime(StringHelper.LimpiarCampo(readerData["Fecha"])).ToString("dd/MM/yyyy HH:mm") : "";
                                nBultoXD.Destino_cod = StringHelper.LimpiarCampo(readerData["Destino_cod"]);
                                nBultoXD.Bultos = StringHelper.LimpiarCampo(readerData["Bultos"]);
                                nBultoXD.Destino_razon_soc = StringHelper.LimpiarCampo(readerData["Destino_razon_soc"]);
                                nBultoXD.Tipo_servicio = StringHelper.LimpiarCampo(readerData["Tipo_servicio"]);
                                nBultoXD.Cantidad_etiquetas = Convert.ToInt32(cantidadTotal);

                                remitos.Add(new Remito(int.Parse(sIDRemito), nBultoXD));
                            }
                        }
                        else
                        {
                            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "No se encontraron remitos disponibles para IDRemito: " + sIDRemito + " - cliente: " + cliente);
                            if (connDB.State == ConnectionState.Open) connDB.Close();
                            if (connLog.State == ConnectionState.Open) connLog.Close();
                            response.message = "Notificacion: No se encontraron remitos disponibles para IDRemito: " + sIDRemito + " - cliente: " + cliente;
                            return response;
                        }
                    }
                }
                else if (string.Equals(template.ToUpper(), TEMPLATE_BULTOS_DHL_APPLE))
                {
                    using (SqlDataReader readerCantidad = new SqlCommand
                    {
                        Connection = connDB,
                        CommandType = CommandType.Text,
                        CommandTimeout = timeOutQueries,
                        CommandText = queriesSQL.cant_BultosXD,
                        Parameters =
                        {
                            new SqlParameter { ParameterName = "@IdRemito", SqlDbType = SqlDbType.Int, Value = int.Parse(sIDRemito)}
                        }
                    }.ExecuteReader())
                    {
                        if (readerCantidad.HasRows)
                        {
                            while (readerCantidad.Read())
                            {
                                cantidadTotal = StringHelper.LimpiarCampo(readerCantidad["Cantidad"]);
                            }
                        }
                        else
                        {
                            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "Error buscando cantidad en Remitos para IDRemito: " + sIDRemito + " - cliente: " + cliente);
                            if (connDB.State == ConnectionState.Open) connDB.Close();
                            if (connLog.State == ConnectionState.Open) connLog.Close();
                            response.message = "ERROR: Error buscando cantidad en Remitos para IDRemito: " + sIDRemito + " - cliente: " + cliente;
                            return response;
                        }
                    }

                    using (SqlDataReader readerData = new SqlCommand
                    {
                        Connection = connDB,
                        CommandType = CommandType.Text,
                        CommandTimeout = timeOutQueries,
                        CommandText = queriesSQL.sRemitosBultosXD,
                        Parameters =
                        {
                            new SqlParameter {ParameterName = "@CodCliente", SqlDbType = SqlDbType.Int, Value = int.Parse(cliente)},
                            new SqlParameter {ParameterName = "@IdRemito", SqlDbType = SqlDbType.Int, Value = int.Parse(sIDRemito)}
                        }
                    }.ExecuteReader())
                    {
                        if (readerData.HasRows)
                        {
                            while (readerData.Read())
                            {
                                etiquetasCount++;

                                BultosXD nBultoXD = new BultosXD();

                                nBultoXD.Origen = StringHelper.LimpiarCampo(readerData["Origen"]);
                                nBultoXD.Domicilio = StringHelper.LimpiarCampo(readerData["Domicilio"]);
                                nBultoXD.Localidad = StringHelper.LimpiarCampo(readerData["Localidad"]);
                                nBultoXD.Mail = StringHelper.LimpiarCampo(readerData["Mail"]);
                                nBultoXD.Url = StringHelper.LimpiarCampo(readerData["Url"]);
                                nBultoXD.Nro_seguimiento = StringHelper.LimpiarCampo(readerData["Nro_seguimiento"]);
                                nBultoXD.Fecha = StringHelper.LimpiarCampo(readerData["Fecha"]) != "" ? Convert.ToDateTime(StringHelper.LimpiarCampo(readerData["Fecha"])).ToString("dd/MM/yyyy HH:mm") : "";
                                nBultoXD.Destino_cod = StringHelper.LimpiarCampo(readerData["Destino_cod"]);
                                nBultoXD.Bultos = StringHelper.LimpiarCampo(readerData["Bultos"]);
                                nBultoXD.Destino_razon_soc = StringHelper.LimpiarCampo(readerData["Destino_razon_soc"]);
                                nBultoXD.Tipo_servicio = StringHelper.LimpiarCampo(readerData["Tipo_servicio"]);
                                nBultoXD.Cantidad_etiquetas = Convert.ToInt32(cantidadTotal);

                                remitos.Add(new Remito(int.Parse(sIDRemito), nBultoXD));
                            }
                        }
                        else
                        {
                            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "No se encontraron remitos disponibles para IDRemito: " + sIDRemito + " - cliente: " + cliente);
                            if (connDB.State == ConnectionState.Open) connDB.Close();
                            if (connLog.State == ConnectionState.Open) connLog.Close();
                            response.message = "Notificacion: No se encontraron remitos disponibles para IDRemito: " + sIDRemito + " - cliente: " + cliente;
                            return response;
                        }
                    }
                }
                else if (string.Equals(template.ToUpper(), TEMPLATE_VIAJES_DHL_Apple))
                {
                    using (SqlDataReader readerCantidad = new SqlCommand
                    {
                        Connection = connDB,
                        CommandType = CommandType.Text,
                        CommandTimeout = timeOutQueries,
                        CommandText = queriesSQL.cant_BultosXD,
                        Parameters =
                        {
                            new SqlParameter { ParameterName = "@IdRemito", SqlDbType = SqlDbType.Int, Value = int.Parse(sIDRemito)}
                        }
                    }.ExecuteReader())
                    {
                        if (readerCantidad.HasRows)
                        {
                            while (readerCantidad.Read())
                            {
                                cantidadTotal = StringHelper.LimpiarCampo(readerCantidad["Cantidad"]);
                            }
                        }
                        else
                        {
                            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "Error buscando cantidad en Remitos para IDRemito: " + sIDRemito + " - cliente: " + cliente);
                            if (connDB.State == ConnectionState.Open) connDB.Close();
                            if (connLog.State == ConnectionState.Open) connLog.Close();
                            response.message = "ERROR: Error buscando cantidad en Remitos para IDRemito: " + sIDRemito + " - cliente: " + cliente;
                            return response;
                        }
                    }

                    using (SqlDataReader readerData = new SqlCommand
                    {
                        Connection = connDB,
                        CommandType = CommandType.Text,
                        CommandTimeout = timeOutQueries,
                        CommandText = queriesSQL.sRemitosBultosXD,
                        Parameters =
                        {
                            new SqlParameter {ParameterName = "@CodCliente", SqlDbType = SqlDbType.Int, Value = int.Parse(cliente)},
                            new SqlParameter {ParameterName = "@IdRemito", SqlDbType = SqlDbType.Int, Value = int.Parse(sIDRemito)}
                        }
                    }.ExecuteReader())
                    {
                        if (readerData.HasRows)
                        {
                            while (readerData.Read())
                            {
                                etiquetasCount++;

                                BultosXD nBultoXD = new BultosXD();

                                nBultoXD.Origen = StringHelper.LimpiarCampo(readerData["Origen"]);
                                nBultoXD.Domicilio = StringHelper.LimpiarCampo(readerData["Domicilio"]);
                                nBultoXD.Localidad = StringHelper.LimpiarCampo(readerData["Localidad"]);
                                nBultoXD.Mail = StringHelper.LimpiarCampo(readerData["Mail"]);
                                nBultoXD.Url = StringHelper.LimpiarCampo(readerData["Url"]);
                                nBultoXD.Nro_seguimiento = StringHelper.LimpiarCampo(readerData["Nro_seguimiento"]);
                                nBultoXD.Fecha = StringHelper.LimpiarCampo(readerData["Fecha"]) != "" ? Convert.ToDateTime(StringHelper.LimpiarCampo(readerData["Fecha"])).ToString("dd/MM/yyyy HH:mm") : "";
                                nBultoXD.Destino_cod = StringHelper.LimpiarCampo(readerData["Destino_cod"]);
                                nBultoXD.Bultos = StringHelper.LimpiarCampo(readerData["Bultos"]);
                                nBultoXD.Destino_razon_soc = StringHelper.LimpiarCampo(readerData["Destino_razon_soc"]);
                                nBultoXD.Tipo_servicio = StringHelper.LimpiarCampo(readerData["Tipo_servicio"]);
                                nBultoXD.Cantidad_etiquetas = Convert.ToInt32(cantidadTotal);

                                remitos.Add(new Remito(int.Parse(sIDRemito), nBultoXD));
                            }
                        }
                        else
                        {
                            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "No se encontraron remitos disponibles para IDRemito: " + sIDRemito + " - cliente: " + cliente);
                            if (connDB.State == ConnectionState.Open) connDB.Close();
                            if (connLog.State == ConnectionState.Open) connLog.Close();
                            response.message = "Notificacion: No se encontraron remitos disponibles para IDRemito: " + sIDRemito + " - cliente: " + cliente;
                            return response;
                        }
                    }
                }
                else
                {
                    vertical = false;
                    List<Etiqueta> lEtiquetas = new List<Etiqueta>();
                    using (SqlDataReader sqlReader = new SqlCommand
                    {
                        CommandText = queriesSQL.sRemitos,
                        CommandType = CommandType.Text,
                        CommandTimeout = timeOutQueries,
                        Connection = connDB,
                        Parameters =
                        {
                            new SqlParameter{ ParameterName = "@IDRemito",SqlDbType = SqlDbType.Int,Value = int.Parse(sIDRemito)},
                            new SqlParameter{ ParameterName = "@Cod_Cliente",SqlDbType = SqlDbType.Int,Value = int.Parse(cliente)}
                        }
                    }.ExecuteReader())
                    {
                        if (sqlReader.HasRows)
                        {
                            while (sqlReader.Read())
                            {
                                etiquetasCount++;
                                lEtiquetas.Add(new Etiqueta(
                                    sqlReader["ID_Remito"].ToString().Trim(),
                                    sqlReader["Cod_cliente"].ToString().Trim(),
                                    sqlReader["CE"].ToString().Trim(),
                                    sqlReader["Grupo"].ToString().Trim(),
                                    sqlReader["Nro_Remito"].ToString().Trim(),
                                    sqlReader["Nro_Pedido"].ToString().Trim(),
                                    sqlReader["Domicilio"].ToString().Trim(),
                                    sqlReader["Region"].ToString().Trim(),
                                    sqlReader["Provincia"].ToString().Trim(),
                                    sqlReader["Localidad"].ToString().Trim(),
                                    sqlReader["Zona"].ToString().Trim(),
                                    sqlReader["CP"].ToString().Trim(),
                                    sqlReader["Razon_Soc"].ToString().Trim(),
                                    sqlReader["Bultos"].ToString().Trim(),
                                    sqlReader["Pallets"].ToString().Trim(),
                                    sqlReader["Descripcion"].ToString().Trim(),
                                    sqlReader["Kilos"].ToString().Trim(),
                                    sqlReader["m3"].ToString().Trim(),
                                    sqlReader["Cantidad"].ToString().Trim(),
                                    sqlReader["Fecha_Remito"].ToString().Trim(),
                                    sqlReader["ID_PE_REAL"].ToString().Trim(),
                                    sqlReader["Cod_Cia"].ToString().Trim(),
                                    sqlReader["Nombre_cliente"].ToString().Trim(),
                                    sqlReader["Contacto_Celular"].ToString().Trim()
                                ));
                            }

                            remitos.Add(new Remito(int.Parse(sIDRemito), lEtiquetas));
                        }
                        else
                        {
                            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "No se encontraron remitos disponibles para IDRemito: " + sIDRemito + " - cliente: " + cliente);
                            if (connDB.State == ConnectionState.Open) connDB.Close();
                            if (connLog.State == ConnectionState.Open) connLog.Close();
                            response.message = "Notificacion: No se encontraron remitos disponibles para IDRemito: " + sIDRemito + " - cliente: " + cliente;
                            return response;
                        }
                    }
                }
            }
            System.Drawing.Image etiquetaImg = obtenerTemplate(template, vertical);
            System.Drawing.Image logoImg = obtenerLogo(etiquetaImg, vertical);
            #endregion

            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Hay " + etiquetasCount.ToString() + " etiquetas a procesar del IDRemito: " + IDRemito);

            #region Generacion de etiquetas
            List<Bitmap> hojas = new List<Bitmap>();

            // Impresión de todos los bultos de un mismo remito en una página
            if (format.ToLower() == FORMAT_PDF && separarPorDocumento == 0)
            {
                List<Etiqueta> etiquetasTotales = new List<Etiqueta>();
                List<BultosVertical> etiquetasVerticalTotales = new List<BultosVertical>();
                if (string.Equals(template.ToUpper(), TEMPLATE_VERTICAL))
                {
                    remitos.ForEach(i => i.etiquetasVerticales.ForEach(e => etiquetasVerticalTotales.Add(e)));
                    var archivo = GenerarPDFBultosVertical(etiquetasVerticalTotales, size.ToLower(), format);
                    response.message = "Etiquetas generadas con exito";
                    response.Archivos.Add(archivo);
                    return response;
                }
                else if(string.Equals(template.ToUpper(), TEMPLATE_BULTOSXD))
                {
                    var archivo = GenerarPDFBultosXD(remitos, size.ToLower(), format);
                    response.message = "Etiquetas generadas con exito";
                    response.Archivos.Add(archivo);
                }
                else if (string.Equals(template.ToUpper(), TEMPLATE_BULTOS_DHL_APPLE))
                {
                    var archivo = new HandlersEtiquetasApple(log, connLog, severidades, MODO_OBTENCION_ARCHIVO, gvalues, size, TEMPLATE_BULTOS_DHL_APPLE).GenerarPDFBultosDHLApple(remitos, size.ToLower(), format);
                    response.message = "Etiquetas generadas con exito";
                    response.Archivos.Add(archivo);
                }
                else if (string.Equals(template.ToUpper(), TEMPLATE_VIAJES_DHL_Apple))
                { 
                    var archivo = new HandlersEtiquetasApple(log,connLog,severidades,MODO_OBTENCION_ARCHIVO,gvalues,size, TEMPLATE_VIAJES_DHL_Apple).GenerarPDFBultosDHLViajesApple(remitos, size.ToLower(), format);
                    response.message = "Etiquetas generadas con exito";
                    response.Archivos.Add(archivo);
                }
                else
                {
                    remitos.ForEach(i => i.etiquetas.ForEach(e => etiquetasTotales.Add(e)));
                    hojas = GenerarEtiquetas(etiquetasTotales, null, etiquetaImg, size.ToLower(), format.ToLower(), template);
                }

                if (!TemplatesHabilitados.Contains(template.ToUpper()))
                {
                    if (hojas.Count == 0)
                    {
                        if (remitos.Count > 1)
                        {
                            response.message = "No existen etiquetas para los documentos seleccionados";
                            return response;
                        }
                        response.message = "No existen etiquetas para el documento seleccionado";
                        return response;
                    }

                    response = imprimirHojas(hojas, response, format, size);
                    response.message = "Etiquetas generadas con exito";
                    return response;
                }
            }
            else if (format.ToLower() == FORMAT_PDF && separarPorDocumento == 1)
            {
                foreach (Remito remito in remitos)
                {
                    List<BultosVertical> etiquetasVerticalTotales = new List<BultosVertical>();
                    List<Remito> etiquetaBultosXDSeparada = new List<Remito>();
                    if (string.Equals(template.ToUpper(), TEMPLATE_VERTICAL))
                    {
                        var archivo = GenerarPDFBultosVertical(remito.etiquetasVerticales, size.ToLower(), format.ToLower());

                        if (archivo != null)
                        {
                            response.Archivos.Add(archivo);
                        }
                    }
                    else if (string.Equals(template.ToUpper(), TEMPLATE_BULTOSXD))
                    {
                        etiquetaBultosXDSeparada.Add(remito);
                        var archivo = GenerarPDFBultosXD(etiquetaBultosXDSeparada, size.ToLower(), format.ToLower());

                        if (archivo != null)
                        {
                            response.Archivos.Add(archivo);
                        }
                    }
                    else if (string.Equals(template.ToUpper(), TEMPLATE_BULTOS_DHL_APPLE))
                    {
                        //etiquetaBultosXDSeparada.Add(remito);
                        ////var archivo = GenerarPDFBultosXD(etiquetaBultosXDSeparada, size.ToLower(), format.ToLower());

                        //if (archivo != null)
                        //{
                        //    response.Archivos.Add(archivo);
                        //}
                    }
                    else if (string.Equals(template.ToUpper(), TEMPLATE_VIAJES_DHL_Apple))
                    {
                      //  etiquetaBultosXDSeparada.Add(remito);
                      ////  var archivo = GenerarPDFBultosXD(etiquetaBultosXDSeparada, size.ToLower(), format.ToLower());

                      //  if (archivo != null)
                      //  {
                      //      response.Archivos.Add(archivo);
                      //  }
                    }
                    else
                    {
                        hojas = GenerarEtiquetas(remito.etiquetas, null, etiquetaImg, size.ToLower(), format.ToLower(), template);
                    }

                    // Modificacion Matias -- Aca lo que hago es verificar que nada venga vacio. Le agrego la validación de la respuesta que viene de GenerarPDFBultosVertical.
                    if (hojas.Count == 0 && response.Archivos.Count() == 0)
                    {
                        response.message = "No existen etiquetas para el remito \"" + remito.ID_Remito + "\"";
                        return response;
                    }

                    // Modificacion Matias -- Para que todo funcione solo entra aca si no es bultos vertical asi los demas templates siguen funcionando. Si es template vertical ya tendra la respuesta.
                    if (TemplatesHabilitados.Contains(template.ToUpper()))
                    {
                        response = imprimirHojas(hojas, response, format, size);
                    }

                }
                if (template.ToUpper() != TEMPLATE_BULTOSXD && template.ToUpper() != TEMPLATE_BULTOS_DHL_APPLE && template.ToUpper() != TEMPLATE_VIAJES_DHL_Apple)
                {
                    response.message = "Etiquetas generadas con exito";
                    return response;
                }
            }
            else
            {
                List<Etiqueta> etiquetasTotales = new List<Etiqueta>();
                List<BultosVertical> etiquetasVerticalTotales = new List<BultosVertical>();
                if (string.Equals(template.ToUpper(), TEMPLATE_VERTICAL))
                {
                    remitos.ForEach(i => i.etiquetasVerticales.ForEach(e => etiquetasVerticalTotales.Add(e)));
                    hojas = GenerarEtiquetas(null, etiquetasVerticalTotales, etiquetaImg, size.ToLower(), format.ToLower(), template);
                }
                else if (string.Equals(template.ToUpper(), TEMPLATE_BULTOSXD) || string.Equals(template.ToUpper(), TEMPLATE_BULTOS_DHL_APPLE) || string.Equals(template.ToUpper(), TEMPLATE_VIAJES_DHL_Apple))
                {
                    log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "El template: " + template + " no admite el formato: " + format);
                    response.message = "No se admite el formato ingresado.";
                    return response;
                }
                else
                {
                    remitos.ForEach(i => i.etiquetas.ForEach(e => etiquetasTotales.Add(e)));
                    hojas = GenerarEtiquetas(etiquetasTotales, null, etiquetaImg, size.ToLower(), format.ToLower(), template);
                }

                if ((template.ToUpper() != TEMPLATE_BULTOSXD) && (template.ToUpper() != TEMPLATE_BULTOS_DHL_APPLE) && (template.ToUpper() != TEMPLATE_VIAJES_DHL_Apple))
                {
                    int index = 0;
                    foreach (Bitmap hoja in hojas)
                    {
                        index++;
                        //Se genera path de salida
                        // string pathOut = gvalues.PathOut + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + etq.ID_Remito + "_" + etq.Cod_Cliente + "_" + bulto.ToString() + "de" + bultosTotal.ToString() + "." + format.ToLower();
                        string pathOut = gvalues.PathOut + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + index + "de" + hojas.Count.ToString() + "." + format.ToLower();
                        string fileName = System.IO.Path.GetFileName(pathOut);

                        //Se elige el modo de grabacion segun formato
                        if (format.ToLower() == "pdf")
                        {
                            // createdFile = Globals.ImagesToPdf(hoja, pathOut);
                            Globals.ImagesToPdf(hoja, pathOut);
                        }
                        else if (format.ToLower() == FORMAT_JPG)
                        {
                            hoja.Save(pathOut);
                        }
                        else
                        {
                            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "El formato no puede ser " + format.ToLower());

                            sendMail.SendMailLogs();

                            if (connDB.State == ConnectionState.Open) connDB.Close();
                            if (connLog.State == ConnectionState.Open) connLog.Close();
                            response.message = "El formato no puede ser " + format.ToLower();
                            return response;
                        }

                        //Si el cliente desea generar backup se copia el archivo de salida en la carpeta backup
                        if (gvalues.GeneraBackup.ToUpper() == "S")
                        {
                            if (gvalues.PathBack == string.Empty)
                            {
                                log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ERROR", "El path de backup no puede estar vacío. Revise el archivo .config.");
                            }
                            else
                            {
                                if (!Directory.Exists(gvalues.PathBack))
                                    Directory.CreateDirectory(gvalues.PathBack);

                                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando BACKUP en " + gvalues.PathBack);
                                File.Copy(pathOut, System.IO.Path.Combine(gvalues.PathBack, System.IO.Path.GetFileName(pathOut)));
                            }
                        }

                        //INICIO - MODIFICACION - 30/3/20 - G.SANCINETO - v1.1.0.0
                        if (MODO_OBTENCION_ARCHIVO == "BASE64")
                        {
                            ImageConverter converter = new ImageConverter();
                            byte[] data = File.ReadAllBytes(pathOut);
                            Archivo archivo = new Archivo();
                            archivo.base64 = Convert.ToBase64String(data);
                            archivo.nombre = fileName;
                            response.Archivos.Add(archivo);
                        }
                        else if (MODO_OBTENCION_ARCHIVO == "URL")
                        {
                            string url = System.IO.Path.Combine(gvalues.RaizURL, fileName);
                            Archivo archivo = new Archivo();
                            archivo.url = url;
                            response.Archivos.Add(archivo);
                        }
                    }
                }

            }
            #endregion

            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Etiquetas generadas con exito para IDRemito: " + IDRemito + " - Cliente: " + cliente);

            sendMail.SendMailLogs();

            if (connDB.State == ConnectionState.Open) connDB.Close();
            if (connLog.State == ConnectionState.Open) connLog.Close();
            response.message = "Etiquetas generadas con exito para IDRemito: " + IDRemito + " - Cliente: " + cliente;
            return response;
        }
        catch (Exception ex)
        {
            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "Error no controlado: " + ex.Message);
            sendMail.SendMailLogs();

            if (connDB.State == ConnectionState.Open) connDB.Close();
            if (connLog.State == ConnectionState.Open) connLog.Close();
            response.message = "Error no controlado: " + ex.Message;
            return response;
        }
    }

    private Archivo GenerarPDFBultosVertical(List<BultosVertical> lEtiquetasVertical, string size, string format)
    {
        Archivo archivoPdf = new Archivo();

        PageSize pageSize = null;
        #region Aca seteo el tamaño de pagina si es a4 o zebra 
        if (size.ToUpper().Equals("A4"))
        {
            pageSize = PageSize.A4;
        }
        else
        {
            pageSize = new PageSize(285, 429);
        }
        #endregion


        string ruta = gvalues.PathOut + "\\" + DateTime.Now.ToString("yyyyMMdd-hhmmssffff_")+ lEtiquetasVertical.Count() + ".pdf";
        PdfWriter writer = new PdfWriter(ruta);
        PdfDocument pdf = new PdfDocument(writer);

        Document document = new Document(pdf, pageSize);
        document.SetMargins(0, 1, 0, 0);

        try
        {

            if (!Directory.Exists(gvalues.PathOut))
            {
                Directory.CreateDirectory(gvalues.PathOut);
            }


            ImageData logo = ImageDataFactory.Create(gvalues.PathLogo);


            int countBulto = 0;
            string idRemitoAux = string.Empty;
            int conteadorUltimaPagina = 1;

            foreach (var etq in lEtiquetasVertical)
            {
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando codigo de barra para IDRemito: " + etq.IdRemito);

                System.Drawing.Image barcode1 = null;
                ImageData codigoBarras = null;

                if (etq.TrackingNumber != null)
                {
                    try
                    {
                        BarcodeWriter bcWriter = new BarcodeWriter();
                        EncodingOptions encodingBC = new EncodingOptions() { Width = 150, Height = 50, Margin = 0, PureBarcode = true };
                        bcWriter.Options = encodingBC;
                        bcWriter.Format = BarcodeFormat.CODE_128;
                        barcode1 = new Bitmap(bcWriter.Write(etq.TrackingNumber));
                        var img = new ImageHelper().ImageToByte(barcode1);
                        codigoBarras = ImageDataFactory.Create(img);
                    }
                    catch (Exception e)
                    {
                        log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo de barra para IDRemito: " + etq.IdRemito + ". Detalles: " + e.Message);
                    }
                }

                //FIN - MODIFICACION - LFC - 7/7/20 - v1.2.0.0

                //Variables legibles Esto lo copie de GABI, sinceramente me costo tiempo entender que hacia. Quiza lo reworkearia en un futuro. Por lo que entiendo itera por etiquetas y las etiquetas tienen bultos totales


                double bultosAux = 0;
                double outBultos = -1;


                if (double.TryParse(etq.Bulto, out outBultos))
                    if (outBultos != -1)
                        bultosAux = outBultos;

                int bultosTotal = Convert.ToInt32(bultosAux);

                //if (bultosTotal == 0)
                //{
                //    log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ATENCION", "El remito: " + etq.IdRemito + " - cliente: " + etq.Cod_Cliente + " tiene 0 bultos, no se generaron etiquetas");

                //    sendMail.SendMailLogs();

                //    if (connDB.State == ConnectionState.Open) connDB.Close();
                //    if (connLog.State == ConnectionState.Open) connLog.Close();
                //    continue;
                //}

                double kilos = 0;
                if (etq.Peso != string.Empty)
                {
                    kilos = Math.Round(double.Parse(etq.Peso), 3);
                    bultosTotal = 1;
                }

                if (!string.Equals(idRemitoAux, etq.IdRemito))
                {
                    idRemitoAux = etq.IdRemito;
                    countBulto = 0;
                }

                if (Convert.ToDouble(etq.Bulto) == 0)
                {
                    bultosTotal = 1;
                }

                countBulto++;

                for (int bulto = 1; bulto <= bultosTotal; bulto++)
                {
                    log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando etiqueta para Nro Pedido: " + etq.NroPedido + " - bulto " + bulto.ToString() + " de " + bultosTotal.ToString() + " - Tamaño: " + size + " - Formato: " + format);

                    #region Aca es la tabla del encabezado Tracking number y el logo
                    //Esta tabla es responsiva a todos los logos que se lo pongan siempre va a escalar al tamaño que tiene la celda asignada. 

                    Table tablaEncabezado = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }));
                    tablaEncabezado.AddCell(new Cell().Add(new Paragraph("TRACKING No").SetFontSize(10).SetTextAlignment(TextAlignment.LEFT))
                                                        .Add(new Paragraph(etq.TrackingNumber).SetTextAlignment(TextAlignment.LEFT)).SetBorder(Border.NO_BORDER)
                                            ).SetMinHeight(35).SetMaxHeight(35);
                    tablaEncabezado.AddCell(new Cell().Add(new iText.Layout.Element.Image(logo).SetAutoScale(true).SetPadding(0).SetMargins(0, 0, 0, 0).SetHorizontalAlignment(HorizontalAlignment.RIGHT)).SetBorder(Border.NO_BORDER)).SetMinHeight(35).SetMaxHeight(35);
                    tablaEncabezado.SetWidth(UnitValue.CreatePercentValue(100));
                    tablaEncabezado.SetHeight(UnitValue.CreatePercentValue(100));
                    tablaEncabezado.SetMargins(2, 1, 0, 1);

                    document.Add(tablaEncabezado);

                    #endregion


                    #region Aca creo la tabla del codigo de barra
                    //En la versión v3.1.0.2 se corrige a que solo inserte la celda con imagen si el codigo de barra != null sino se agrega un parrafo vacio.
                    Cell celdaCodigoBarras = null;
                    if (etq.TrackingNumber != string.Empty)
                    {
                        celdaCodigoBarras = (new Cell().Add(new iText.Layout.Element.Image(codigoBarras)));
                    }
                    else
                    {
                        celdaCodigoBarras = (new Cell().Add(new Paragraph()));
                    }



                    Table codigoBarra = new Table(UnitValue.CreatePercentArray(new float[] { 100 }));
                    codigoBarra.AddCell(new Cell().Add(celdaCodigoBarras.SetMargins(2, 0, 0, 2)).SetBorder(Border.NO_BORDER).SetPadding(0).SetHorizontalAlignment(HorizontalAlignment.LEFT)).SetMargins(0, 0, 0, 2);
                    codigoBarra.SetWidth(UnitValue.CreatePercentValue(100));
                    codigoBarra.SetMaxHeight(60).SetMinHeight(60);

                    document.Add(codigoBarra);

                    #endregion

                    #region Tabla de datos

                    //Esto lo hacia en el jpg. Lo que hago aca es setear una celda u otra dependiendo de la etiqueta peso. Lo aplica con la lógica de mas arriba.
                    Cell celdaBultos = null;


                    if (etq.Peso != string.Empty)
                    {
                        celdaBultos = (new Cell().Add(new Paragraph(countBulto + " de " + lEtiquetasVertical.Where(x => x.IdRemito == etq.IdRemito).Count()).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)));
                    }
                    else if(Convert.ToDouble(etq.Bulto) == 0) {
                        celdaBultos = (new Cell().Add(new Paragraph("").SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)));
                    }
                    else
                    {
                        celdaBultos = (new Cell().Add(new Paragraph(bulto + " de " + bultosTotal).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)));
                    }


                    Table tablaDatos = new Table(UnitValue.CreatePercentArray(new float[] { 30, 70 })).SetMargins(0, 3, 0, 3);
                    tablaDatos.AddCell(new Cell().Add(new Paragraph("ORIGEN: ").SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph(etq.Origen).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(12).SetMaxHeight(12))
                                                    .Add(new Paragraph("Ciudad: " + etq.Origen_Ciudad).SetMultipliedLeading(1).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(22).SetMaxHeight(22))
                                                    .Add(new Paragraph("Dirección: " + etq.Origen_Direccion).SetMultipliedLeading(1).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(22).SetMaxHeight(22))
                                        ).SetPaddings(0, 0, 0, 1);
                    tablaDatos.AddCell(new Cell().Add(new Paragraph("DESTINO: ").SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph(etq.Destino).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(12).SetMaxHeight(12))
                                                    .Add(new Paragraph("Ciudad: " + etq.Destino_Ciudad).SetMultipliedLeading(1).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(22).SetMaxHeight(22))
                                                    .Add(new Paragraph("Dirección: " + etq.Destino_Direccion).SetMultipliedLeading(1).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(22).SetMaxHeight(22))
                                                    .Add(new Paragraph("Contacto: " + etq.Destino_Contacto).SetMultipliedLeading(1).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(14).SetMaxHeight(14))
                                                    .Add(new Paragraph("Teléfono: " + etq.Destino_Telefono).SetMultipliedLeading(1).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(14).SetMaxHeight(14))
                                                    .SetWidth(185));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph("CUENTA: ").SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph(etq.Cuenta).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph("MOD ENVIO: ").SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph(etq.ModEnvio).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph("BULTOS: ").SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(celdaBultos.SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph("PESO: ").SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph(etq.Peso).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph("ID REMITO: ").SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph(etq.IdRemito).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph("NRO DCTO: ").SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph(etq.NroDcto).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph("NRO PEDIDO: ").SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph(etq.NroPedido).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph("CONOCIMIENTO: ").SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph(etq.Conocimiento).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(12).SetMaxHeight(12));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph("COMENTARIO: ").SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(42).SetMaxHeight(42));
                    tablaDatos.AddCell(new Cell().Add(new Paragraph(etq.Comentario).SetFontSize(9).SetTextAlignment(TextAlignment.LEFT)).SetMinHeight(42).SetMaxHeight(42));
                    tablaDatos.SetWidth(UnitValue.CreatePercentValue(100));
                    document.Add(tablaDatos);

                    #endregion


                    // Esto es medio loco pero necesito agregar un espacio siempre que el conteador de la ultima etiqueta  sea menor a la cantidad de etiquetas y cuando es la ultima etiqueta necesito que agregue el espacio solo cuando el bulto es menor a los bultos totales porque sino me agregaba un espacio al final de la pagina. En pocas palabras si era a4 me agregaba una página demás. Con el if de abajo logre detectar si es la ultima de las ultimas y no lo agrego

                    if (conteadorUltimaPagina < lEtiquetasVertical.Count() || ((conteadorUltimaPagina == lEtiquetasVertical.Count()) && bulto < bultosTotal))
                    {
                        AreaBreak aB = new AreaBreak();
                        document.Add(aB);
                    };

                }
                conteadorUltimaPagina++;

            }


            document.Close();
            writer.Close();
            pdf.Close();


            string fileName = System.IO.Path.GetFileName(ruta);
            if (MODO_OBTENCION_ARCHIVO == "BASE64")
            {
                Byte[] pdfEnByte = File.ReadAllBytes(ruta);
                archivoPdf.base64 = Convert.ToBase64String(pdfEnByte);
                archivoPdf.nombre = fileName;

                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Etiqueta PDFBultosVertical generada con exito Tamaño: " + size + " - Formato: " + format + ". Modo elegido: BASE64");
            }
            else if (MODO_OBTENCION_ARCHIVO == "URL")
            {
                string url = System.IO.Path.Combine(gvalues.RaizURL, fileName);
                archivoPdf.url = url;
                archivoPdf.nombre = fileName;
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Etiqueta generada con exito - Tamaño: " + size + " - Formato: " + format + ". Modo elegido: URL");
            }

            return archivoPdf;
        }
        catch(Exception e)
        {
            //Esto lo llamo a que si falla en algo la generacion del pdf libero los recursos de este pdf, porque sino queda siempre pegado y el programa nunca lo cierra hasta que se cae.
            writer.Close();
            pdf.Close();
            document.Close();

            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "ERROR", "ERROR CREANDO ARCHIVO PDF" + e.Message.ToString());

            return archivoPdf;
        }
    }
    private Archivo GenerarPDFBultosXD(List<Remito> remitos, string size, string format)
    {
        Archivo archivoPdf = new Archivo();

        PageSize pageSize = null;
        #region Aca seteo el tamaño de pagina si es a4 o zebra 
        if (size.ToUpper().Equals("A4"))
        {
            pageSize = PageSize.A4;
        }
        else
        {
            pageSize = new PageSize(285, 285);
        }
        #endregion

        string ruta = gvalues.PathOut + "\\" + "BultosXD_" + DateTime.Now.ToString("yyyyMMdd-HHmmssffff") + ".pdf";
        PdfWriter writer = new PdfWriter(ruta);
        PdfDocument pdf = new PdfDocument(writer);

        Document document = new Document(pdf, pageSize);
        document.SetMargins(0, 1, 0, 0);

        try
        {
            if (!Directory.Exists(gvalues.PathOut))
            {
                Directory.CreateDirectory(gvalues.PathOut);
            }

            ImageData logo = ImageDataFactory.Create(gvalues.PathLogo);

            foreach (var r in remitos)
            {
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando codigo de barra para IDRemito: " + r.ID_Remito);

                #region Generacion de codigo de barras
                System.Drawing.Image barcode1 = null;
                ImageData codigoBarras = null;

                try
                {
                    BarcodeWriter bcWriter = new BarcodeWriter();
                    EncodingOptions encodingBC = new EncodingOptions() { Width = 400, Height = 100, Margin = 0, PureBarcode = true };
                    bcWriter.Options = encodingBC;
                    bcWriter.Format = BarcodeFormat.CODE_128;
                    barcode1 = new Bitmap(bcWriter.Write(r.bultosXD.Nro_seguimiento));
                    var img = new ImageHelper().ImageToByte(barcode1);
                    codigoBarras = ImageDataFactory.Create(img);
                }
                catch (Exception e)
                {
                    log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo de barra para IDRemito: " + r.ID_Remito + ". Detalles: " + e.Message);
                }
                #endregion

                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando etiquetas para IDRemito: " + r.ID_Remito + " - Tamaño: " + size + " - Formato: " + format);

                for (int bulto = 1; bulto <= r.bultosXD.Cantidad_etiquetas; bulto++)
                {
                    #region Tabla con los datos de cabecera y el logo
                    Table tablaEncabezado = new Table(UnitValue.CreatePercentArray(new float[] { 60, 40 }));

                    tablaEncabezado.AddCell(new Cell().Add(new Paragraph(r.bultosXD.Origen).SetFontSize(11).SetTextAlignment(TextAlignment.LEFT).SetBold().SetMinHeight(15).SetMaxHeight(15).SetMaxWidth(160))
                                                        .Add(new Paragraph(r.bultosXD.Domicilio).SetMultipliedLeading(1).SetFontSize(10).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(12).SetMaxHeight(12).SetMaxWidth(160))
                                                        .Add(new Paragraph(r.bultosXD.Localidad).SetMultipliedLeading(1).SetFontSize(10).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(12).SetMaxHeight(12).SetMaxWidth(160))
                                                        .Add(new Paragraph(r.bultosXD.Mail).SetMultipliedLeading(1).SetFontSize(10).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(12).SetMaxHeight(12).SetMaxWidth(160))
                                                        .Add(new Paragraph(r.bultosXD.Url).SetMultipliedLeading(1).SetFontSize(10).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(12).SetMaxHeight(12).SetMaxWidth(160))
                                                        .SetBorder(Border.NO_BORDER))
                                    .SetMinHeight(35).SetMaxHeight(35);
                    tablaEncabezado.AddCell(new Cell().Add(new iText.Layout.Element.Image(logo).SetAutoScale(true).SetPadding(0).SetMargins(2, 5, 0, 0).SetHorizontalAlignment(HorizontalAlignment.RIGHT)).SetBorder(Border.NO_BORDER)
                        .Add(new Paragraph(r.bultosXD.Tipo_servicio).SetMultipliedLeading(1).SetMarginTop(9).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER).SetMinHeight(20).SetMaxHeight(20).SetMaxWidth(160)))
                        .SetMinHeight(75).SetMaxHeight(75);
                    tablaEncabezado.SetWidth(UnitValue.CreatePercentValue(100));
                    tablaEncabezado.SetHeight(UnitValue.CreatePercentValue(100));
                    tablaEncabezado.SetMargins(6, 1, 0, 6);

                    document.Add(tablaEncabezado);
                    #endregion

                    #region Tabla con el nro de seguimiento
                    Table tbNroSeguimiento = new Table(UnitValue.CreatePercentArray(new float[] { 60, 40 }));
                    tbNroSeguimiento.AddCell(new Cell().Add(new Paragraph("Número de Seguimiento: ").SetFontSize(13).SetTextAlignment(TextAlignment.LEFT).SetBold()).SetBorder(Border.NO_BORDER));
                    tbNroSeguimiento.AddCell(new Cell().Add(new Paragraph(r.bultosXD.Nro_seguimiento).SetFontSize(13).SetTextAlignment(TextAlignment.CENTER).SetBold()).SetBorder(Border.NO_BORDER));
                    tbNroSeguimiento.SetMargins(0, 0, 0, 6);

                    document.Add(tbNroSeguimiento);
                    #endregion

                    #region Aca creo la tabla del codigo de barra

                    Cell celdaCodigoBarras = (new Cell().Add(new iText.Layout.Element.Image(codigoBarras).SetAutoScale(true).SetMargins(4, 0, 0, 0).SetHorizontalAlignment(HorizontalAlignment.CENTER)));

                    Table codigoBarra = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }));
                    codigoBarra.AddCell(new Cell().Add(new Paragraph("Fecha: " + r.bultosXD.Fecha).SetFontSize(11).SetTextAlignment(TextAlignment.LEFT).SetBold().SetMargins(15, 0, 0, 2)).SetBorder(Border.NO_BORDER));
                    codigoBarra.AddCell(celdaCodigoBarras.SetBorder(Border.NO_BORDER));
                    codigoBarra.SetWidth(UnitValue.CreatePercentValue(100));
                    codigoBarra.SetMarginLeft(4);

                    document.Add(codigoBarra);

                    #endregion

                    #region Tabla con los indicadores
                    Table tbtDescIndicadores = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }));
                    tbtDescIndicadores.AddCell(new Cell().Add(new Paragraph("DESTINO").SetFontSize(13).SetTextAlignment(TextAlignment.CENTER).SetBold()).SetBorder(Border.NO_BORDER));
                    tbtDescIndicadores.AddCell(new Cell().Add(new Paragraph("BULTOS").SetFontSize(13).SetTextAlignment(TextAlignment.CENTER).SetBold()).SetBorder(Border.NO_BORDER));
                    tbtDescIndicadores.SetWidth(UnitValue.CreatePercentValue(100));
                    tbtDescIndicadores.SetMargins(4, 0, 0, 0);

                    document.Add(tbtDescIndicadores);

                    int fontSizeBultos = 70;
                    var vAlignBultos = VerticalAlignment.TOP;
                    if(r.bultosXD.Bultos.Length >= 4)
                    {
                        fontSizeBultos = 60;
                        vAlignBultos = VerticalAlignment.MIDDLE;
                    }

                    Table tbIndicadores = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }));
                    tbIndicadores.AddCell(new Cell().Add(new Paragraph(r.bultosXD.Destino_cod).SetFontSize(70).SetTextAlignment(TextAlignment.CENTER).SetBold()).SetMinHeight(100).SetMaxHeight(100).SetMaxWidth(50).SetBorder(Border.NO_BORDER));
                    tbIndicadores.AddCell(new Cell().Add(new Paragraph(r.bultosXD.Bultos).SetVerticalAlignment(vAlignBultos).SetFontSize(fontSizeBultos).SetTextAlignment(TextAlignment.CENTER).SetBold()).SetMinHeight(100).SetMaxWidth(50).SetMaxHeight(100).SetBorder(Border.NO_BORDER));
                    tbIndicadores.SetWidth(UnitValue.CreatePercentValue(100));
                    tbIndicadores.SetMargins(-10, 0, 0, 0);

                    document.Add(tbIndicadores);

                    Table tbContenido = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }));
                    tbContenido.AddCell(new Cell().Add(new Paragraph(r.bultosXD.Destino_razon_soc).SetMultipliedLeading(1).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetFontSize(11).SetTextAlignment(TextAlignment.CENTER).SetBold()).SetBorder(Border.NO_BORDER).SetMaxWidth(50).SetMinHeight(30).SetMaxHeight(30));
                    tbContenido.AddCell(new Cell().Add(new Paragraph("Sin verificar contenido").SetVerticalAlignment(VerticalAlignment.MIDDLE).SetFontSize(11).SetTextAlignment(TextAlignment.CENTER)).SetMinHeight(30).SetMaxWidth(50).SetMaxHeight(30).SetBorder(Border.NO_BORDER));
                    tbContenido.SetWidth(UnitValue.CreatePercentValue(100));
                    tbContenido.SetMargins(-20, 0, 0, 6);

                    document.Add(tbContenido);

                    #endregion

                }
            }

            document.Close();
            writer.Close();
            pdf.Close();

            string fileName = System.IO.Path.GetFileName(ruta);
            if (MODO_OBTENCION_ARCHIVO == "BASE64")
            {
                Byte[] pdfEnByte = File.ReadAllBytes(ruta);
                archivoPdf.base64 = Convert.ToBase64String(pdfEnByte);
                archivoPdf.nombre = fileName;

                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Etiqueta PDFBultosVertical generada con exito Tamaño: " + size + " - Formato: " + format + ". Modo elegido: BASE64");
            }
            else if (MODO_OBTENCION_ARCHIVO == "URL")
            {
                string url = System.IO.Path.Combine(gvalues.RaizURL, fileName);
                archivoPdf.url = url;
                archivoPdf.nombre = fileName;
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Etiqueta generada con exito - Tamaño: " + size + " - Formato: " + format + ". Modo elegido: URL");
            }

            return archivoPdf;
        }
        catch (Exception e)
        {
            //Esto lo llamo a que si falla en algo la generacion del pdf libero los recursos de este pdf, porque sino queda siempre pegado y el programa nunca lo cierra hasta que se cae.
            writer.Close();
            pdf.Close();
            document.Close();

            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "ERROR", "ERROR CREANDO ARCHIVO PDF" + e.Message.ToString());

            return archivoPdf;
        }
    }





    private Response imprimirHojas(List<Bitmap> hojas, Response response, string format, string size)
    {
        byte[] createdFile;
        if (format.ToLower() == FORMAT_PDF)
        {
            string pathOut = gvalues.PathOut + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + hojas.Count.ToString() + "." + format.ToLower();

            createdFile = Globals.ImagesToSinglePdf(hojas, pathOut);

            string fileName = System.IO.Path.GetFileName(pathOut);
            if (MODO_OBTENCION_ARCHIVO == "BASE64")
            {
                ImageConverter converter = new ImageConverter();
                Archivo archivo = new Archivo();

                archivo.base64 = Convert.ToBase64String(createdFile);
                archivo.nombre = fileName;
                response.message = "Etiquetas generadas con exito";
                response.Archivos.Add(archivo);
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Etiqueta generada con exito Tamaño: " + size + " - Formato: " + format + ". Modo elegido: BASE64");
            }
            else if (MODO_OBTENCION_ARCHIVO == "URL")
            {
                string url = System.IO.Path.Combine(gvalues.RaizURL, fileName);
                Archivo archivo = new Archivo();
                archivo.url = url;
                response.message = "Etiquetas generadas con exito";
                response.Archivos.Add(archivo);
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Etiqueta generada con exito - Tamaño: " + size + " - Formato: " + format + ". Modo elegido: URL");
            }
        }

        return response;
    }

    private System.Drawing.Image obtenerLogo(System.Drawing.Image etiquetaImg, bool vertical)
    {
        System.Drawing.Image logoImg = null;
        if (File.Exists(gvalues.PathLogo))
        {
            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Se obtendrá LOGO de empresa en ruta: " + gvalues.PathLogo);
            logoImg = System.Drawing.Image.FromFile(gvalues.PathLogo);
        }
        else
            log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ALERTA", "No se obtuvo LOGO de etiqueta en ruta: " + gvalues.PathLogo + ". La etiqueta saldrá sin logo");

        int widthLogo = 800; int heightLogo = 300; int xLogo = 50; int yLogo = 50;
        int widhtImg = 1772; int heightImg = 1181;
        if (vertical)
        {
            widthLogo = 125; heightLogo = 35; xLogo = 160; yLogo = 0;
            widhtImg = 1181; heightImg = 1772;
        }
        //Se pega el logo al template
        if (etiquetaImg == null)
            etiquetaImg = Globals.ResizeImage(Globals.DrawFilledRectangle(1800, 1200), widhtImg, heightImg);

        if (logoImg != null)
        {
            Graphics g = Graphics.FromImage(etiquetaImg);
            g.DrawImage(Globals.ResizeImage(logoImg, widthLogo, heightLogo), xLogo, yLogo);
        }
        return logoImg;
    }

    private System.Drawing.Image obtenerTemplate(string template, bool vertical)
    {
        //Se obtiene template de etiqueta si existe
        System.Drawing.Image etiquetaImg = null;
        log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Se obtendrá TEMPLATE de etiqueta en ruta: " + gvalues.PathImagen);

        if (template == null || template == "")
        {
            template = "GENERICA";
        }

        if (File.Exists(gvalues.PathImagen + "\\" + template + ".jpg"))
        {
            string imagePath = gvalues.PathImagen + "\\" + template + ".jpg";
            etiquetaImg = System.Drawing.Image.FromFile(imagePath);
            if (!vertical)
                etiquetaImg = Globals.ResizeImage(etiquetaImg, 1772, 1181);
            //else
            //    etiquetaImg = Globals.ResizeImage(etiquetaImg, 283, 425);
        }
        else
        {
            string imagePath = gvalues.PathImagen + "\\GENERICA.jpg";
            etiquetaImg = System.Drawing.Image.FromFile(imagePath);
            etiquetaImg = Globals.ResizeImage(etiquetaImg, 1772, 1181);
        }
        return etiquetaImg;
    }

    private Bitmap CrearHoja(Bitmap etiqueta, Boolean pageSizeIsA4)
    {
        Bitmap hoja = null;
        Graphics gHoja = null;

        //Asigno tamaño a la hoja
        if (pageSizeIsA4)
        {
            hoja = Globals.DrawFilledRectangle(2480, 3508);
            gHoja = Graphics.FromImage(hoja);
            gHoja.DrawImage(etiqueta, 400, 300);
            gHoja.DrawImage(etiqueta, 400, 2000);
        }
        else
        {
            hoja = Globals.DrawFilledRectangle(1006, 640);
            gHoja = Graphics.FromImage(hoja);
            gHoja.DrawImage(Globals.ResizeImage(etiqueta, 1006, 640), 0, 0);
        }
        return hoja;
    }

    private List<Bitmap> GenerarEtiquetas(List<Etiqueta> lEtiquetas, List<BultosVertical> lEtiquetasVertical, System.Drawing.Image etiquetaImg, string size, string format, string template)
    {
        List<Bitmap> hojas = new List<Bitmap>();
        if (size.ToUpper() != PAGE_SIZE_A4 && size.ToUpper() != PAGE_SIZE_ZEBRA)
        {
            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "El tamaño no puede ser " + size.ToLower());
            sendMail.SendMailLogs();
            if (connDB.State == ConnectionState.Open) connDB.Close();
            if (connLog.State == ConnectionState.Open) connLog.Close();
            return hojas;
        }

        Boolean pageSizeIsA4 = false;
        if (size.ToUpper() == PAGE_SIZE_A4)
        {
            pageSizeIsA4 = true;
        }

        // Inicio Etiqueta UNIR/BULTOS
        if (template.ToLower() == "unir" || template.ToLower() == "bultos")
        {
            #region Coordenadas texto

            PointF pFecha = new PointF(1225f, 50f);
            PointF pNroRemito = new PointF(1120f, 145f);
            PointF pNroPedido = new PointF(1120f, 244f);
            PointF pProvincia = new PointF(585f, 415f);
            PointF pLocalidad = new PointF(590f, 494f);
            PointF pRegion = new PointF(1250f, 415f);

            PointF pZona = new PointF(1390f, 542f);
            PointF pCP = new PointF(1155f, 540f);

            PointF pDestinatario = new PointF(640f, 581f);
            PointF pDomicilio = new PointF(590f, 665f);

            PointF pBulto = new PointF(162f, 787f);
            PointF pTipoPedido = new PointF(360f, 882f);

            PointF pKilos = new PointF(180f, 1039f);
            PointF pM3 = new PointF(430f, 1039f);
            PointF pCantidad = new PointF(670f, 1039f);

            PointF pRte = new PointF(170f, 1102f);

            PointF pIDRemito = new PointF(1330f, 1065f);
            PointF pCelular = new PointF(573f, 729f);

            #endregion Coordenadas texto

            foreach (var etq in lEtiquetas)
            {
                //Se buscan datos del remitente
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Se buscarán datos de Remitente para cliente: " + etq.Cod_Cliente);
                string razonSocAux = string.Empty;
                string domicilioAux = string.Empty;
                string localidadAux = string.Empty;
                string provinciaAux = string.Empty;
                string paisAux = string.Empty;

                using (SqlDataReader sqlReader = new SqlCommand
                {
                    CommandText = queriesSQL.sClientes,
                    CommandType = CommandType.Text,
                    CommandTimeout = timeOutQueries,
                    Connection = connDB,
                    Parameters =
                        {
                            new SqlParameter{ ParameterName = "@Cod_Cliente",SqlDbType = SqlDbType.Int,Value = int.Parse(etq.Cod_Cliente)}
                        }
                }.ExecuteReader())
                {
                    if (sqlReader.HasRows)
                    {
                        while (sqlReader.Read())
                        {
                            razonSocAux = sqlReader["Razon_soc"].ToString().Trim();
                            domicilioAux = sqlReader["Domicilio"].ToString().Trim();
                            localidadAux = sqlReader["Localidad"].ToString().Trim();
                            provinciaAux = sqlReader["Provincia"].ToString().Trim();
                            paisAux = sqlReader["Pais"].ToString().Trim();
                        }
                        etq.Remitente = razonSocAux + "-" + domicilioAux + "," + localidadAux + "," + provinciaAux + "," + paisAux;
                    }
                    else
                    {
                        log.GrabarLogs(connLog, severidades.MsgSoporte1, "Error", "No se encontraron datos de Remitente para cliente: " + etq.Cod_Cliente);
                    }
                }
                //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando codigo de barra para IDRemito: " + etq.ID_Remito);

                System.Drawing.Image barcode1 = null;
                System.Drawing.Image barcode2 = null;
                System.Drawing.Image QRcode = null;

                //Se agregan 2 códigos nuevos. Se modifican los codigos de barras para mejorar su resolución y tamaño. Se le sacan los números de debajo para insertarlo como un texto luego
                // Obtengo codigo de barra 1
                try
                {
                    BarcodeWriter bcWriter = new BarcodeWriter();
                    EncodingOptions encodingBC = new EncodingOptions() { Width = 600, Height = 200, Margin = 1, PureBarcode = true };
                    bcWriter.Options = encodingBC;
                    bcWriter.Format = BarcodeFormat.CODE_128;
                    barcode1 = new Bitmap(bcWriter.Write(etq.ID_Remito));
                }
                catch (Exception e)
                {
                    log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo de barra para IDRemito: " + etq.ID_Remito + ". Detalles: " + e.Message);
                }

                //Obtengo codigo de barra 2
                try
                {
                    BarcodeWriter bcWriter = new BarcodeWriter();
                    EncodingOptions encodingBC = new EncodingOptions() { Width = 600, Height = 80, Margin = 1, PureBarcode = true };
                    bcWriter.Options = encodingBC;
                    bcWriter.Format = BarcodeFormat.CODE_128;
                    barcode2 = new Bitmap(bcWriter.Write(etq.Nro_Pedido));
                }
                catch (Exception e)
                {
                    log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo de barra para IDRemito: " + etq.ID_Remito + ". Detalles: " + e.Message);
                }

                //Obtengo codigo QR
                try
                {
                    BarcodeWriter bcWriter = new BarcodeWriter();
                    EncodingOptions encodingQr = new EncodingOptions() { Width = 330, Height = 330, Margin = 1 };
                    bcWriter.Options = encodingQr;
                    bcWriter.Format = BarcodeFormat.QR_CODE;
                    QRcode = new Bitmap(bcWriter.Write(etq.ID_Remito + "|" + etq.Nro_Pedido + "|" + etq.Nro_Remito + "|" + etq.Cod_Cliente + "|" + etq.Nombre_cliente + "|" + etq.Cod_Cia + "|" + etq.Fecha_Remito + "|" + etq.ID_PE_Real + "|" + etq.Domicilio + "|" + etq.Localidad + "|" + etq.Provincia + "|" + etq.Razon_Social + "|" + etq.Bultos + "|" + etq.Kilos + "|" + etq.M3 + "|" + etq.Cantidad));
                }
                catch (Exception e)
                {
                    log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo QR para IDRemito: " + etq.ID_Remito + ". Detalles: " + e.Message);
                }

                double bultosAux = 0;
                double outBultos = -1;
                if (double.TryParse(etq.Bultos, out outBultos))
                    if (outBultos != -1)
                        bultosAux = outBultos;

                int bultosTotal = Convert.ToInt32(bultosAux);

                if (bultosTotal == 0)
                {
                    log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ATENCION", "El remito: " + etq.ID_Remito + " - cliente: " + etq.Cod_Cliente + " tiene 0 bultos, no se generaron etiquetas");
                    sendMail.SendMailLogs();
                    continue;
                }

                double kilos = 0;
                if (etq.Kilos != string.Empty)
                    kilos = Math.Round(double.Parse(etq.Kilos), 3);

                double m3 = 0;
                if (etq.M3 != string.Empty)
                    m3 = Math.Round(double.Parse(etq.M3), 3);

                double cantidad = 0;
                if (etq.Cantidad != string.Empty)
                    cantidad = Math.Round(double.Parse(etq.Cantidad), 3);

                //Me aseguro del length de los campos

                #region Length campos

                for (int i = 0; i < 100; i++)
                {
                    etq.Provincia += " ";
                    etq.Localidad += " ";
                    etq.Region += " ";
                    etq.Zona += " ";
                    etq.Razon_Social += " ";
                    etq.Domicilio += " ";
                    etq.Remitente += " ";
                    etq.Celular += " ";
                    //MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                }

                #endregion Length campos

                //Se itera la cantidad de bultos para generar un label por cada bulto
                for (int bulto = 1; bulto <= bultosTotal; bulto++)
                {
                    log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando etiqueta para Nro Pedido: " + etq.Nro_Pedido + " - bulto " + bulto.ToString() + " de " + bultosTotal.ToString() + " - Tamaño: " + size + " - Formato: " + format);

                    Bitmap etiqueta = null;
                    using (var bitmap = (Bitmap)etiquetaImg.Clone())    //MODIFICADO - 26/3/20 - v1.0.0.1 - G.SANCINETO - Se añadio el clone()
                    {
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            //INICIO - MODIFICACION - LFC - 7/7/20 - v1.2.0.0
                            //Se generan mas secciones de tamaños de fuente de letras. Se agrega teléfono
                            using (Font arialFont = new Font("Arial", 11, FontStyle.Bold))
                            {
                                //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                //Sirve para alinear a la derecha los numeros
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Far;
                                //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                graphics.DrawString(etq.Fecha_Remito, arialFont, Brushes.Black, pFecha);
                                graphics.DrawString(etq.CE + "-" + etq.Grupo + "-" + etq.Nro_Remito, arialFont, Brushes.Black, pNroRemito);
                                graphics.DrawString(etq.Nro_Pedido, arialFont, Brushes.Black, pNroPedido);
                            }
                            using (Font arialFont = new Font("Arial", 9, FontStyle.Bold))
                            {
                                //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                //Sirve para alinear a la derecha los numeros
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Far;
                                //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                graphics.DrawString(etq.Provincia.Substring(0, 19), arialFont, Brushes.Black, pProvincia);
                                graphics.DrawString(etq.Localidad.Substring(0, 28), arialFont, Brushes.Black, pLocalidad);
                                graphics.DrawString(etq.Region.Substring(0, 20), arialFont, Brushes.Black, pRegion);
                                graphics.DrawString(etq.Zona.Substring(0, 28), arialFont, Brushes.Black, pZona);
                                graphics.DrawString(etq.CP, arialFont, Brushes.Black, pCP);
                                graphics.DrawString(etq.Razon_Social.Substring(0, 25), arialFont, Brushes.Black, pDestinatario);
                                graphics.DrawString(etq.Celular.Substring(0, 25), arialFont, Brushes.Black, pCelular);
                            }
                            using (Font arialFont = new Font("Arial", 11, FontStyle.Bold))
                            {
                                //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                //Sirve para alinear a la derecha los numeros
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Far;
                                //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                graphics.DrawString(bulto.ToString() + " de " + bultosTotal.ToString(), arialFont, Brushes.Black, pBulto);
                                graphics.DrawString(etq.Descripcion_Documento, arialFont, Brushes.Black, pTipoPedido);

                                //Se agregaron formatos de alineacion
                                graphics.DrawString(kilos.ToString(), arialFont, Brushes.Black, pKilos, stringFormat);
                                graphics.DrawString(m3.ToString(), arialFont, Brushes.Black, pM3, stringFormat);
                                graphics.DrawString(cantidad.ToString(), arialFont, Brushes.Black, pCantidad, stringFormat);
                                graphics.DrawString(etq.ID_Remito, arialFont, Brushes.Black, pIDRemito, stringFormat);
                            }
                            using (Font arialFont = new Font("Arial", 8, FontStyle.Bold))
                            {
                                graphics.DrawString(etq.Domicilio.Substring(0, 75), arialFont, Brushes.Black, pDomicilio);
                                graphics.DrawString(etq.Remitente.Substring(0, 70), arialFont, Brushes.Black, pRte); //MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                            }
                            //FIN - MODIFICACION - LFC - 7/7/20 - v1.2.0.0
                        }
                        etiqueta = new Bitmap(bitmap);
                    }

                    //Se agrega el dibujado de los 2 codigos nuevos. Se modifica el tamaño y posicionamiento del codigo de barra ya existente
                    if (etiqueta != null && barcode1 != null)
                    {
                        Graphics gBarcode1 = Graphics.FromImage(etiqueta);
                        gBarcode1.DrawImage(Globals.ResizeImage(barcode1, 600, 200), 1100f, 810f);
                    }

                    if (etiqueta != null && barcode2 != null)
                    {
                        Graphics gBarcode2 = Graphics.FromImage(etiqueta);
                        gBarcode2.DrawImage(Globals.ResizeImage(barcode2, 600, 80), 860f, 300f);
                    }

                    if (etiqueta != null && QRcode != null)
                    {
                        Graphics gBarcode3 = Graphics.FromImage(etiqueta);
                        gBarcode3.DrawImage(Globals.ResizeImage(QRcode, 330, 330), 40f, 415f);
                    }

                    // Fin Etiqueta UNIR/BULTOS

                    Bitmap hoja = CrearHoja(etiqueta, pageSizeIsA4);
                    hojas.Add(hoja);

                    // Si el path de salida no existe se crea
                    if (!Directory.Exists(gvalues.PathOut))
                        Directory.CreateDirectory(gvalues.PathOut);
                }
            }
        }
        else if (template.ToLower() == "pallets")
        {
            #region Coordenadas texto

            PointF pFecha = new PointF(1225f, 50f);
            PointF pNroRemito = new PointF(1120f, 145f);
            PointF pNroPedido = new PointF(1120f, 244f);
            PointF pProvincia = new PointF(585f, 415f);
            PointF pLocalidad = new PointF(590f, 494f);
            PointF pRegion = new PointF(1250f, 415f);

            PointF pZona = new PointF(1390f, 542f);
            PointF pCP = new PointF(1155f, 540f);

            PointF pDestinatario = new PointF(640f, 581f);
            PointF pDomicilio = new PointF(590f, 665f);

            PointF pPallet = new PointF(162f, 787f);
            PointF pTipoPedido = new PointF(360f, 882f);

            PointF pKilos = new PointF(180f, 1039f);
            PointF pM3 = new PointF(430f, 1039f);
            PointF pCantidad = new PointF(670f, 1039f);
            PointF pBultos = new PointF(910f, 1039f);

            PointF pRte = new PointF(170f, 1102f);

            PointF pIDRemito = new PointF(1330f, 1065f);
            PointF pCelular = new PointF(573f, 729f);

            #endregion Coordenadas texto

            foreach (var etq in lEtiquetas)
            {
                //Se buscan datos del remitente
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Se buscarán datos de Remitente para cliente: " + etq.Cod_Cliente);
                string razonSocAux = string.Empty;
                string domicilioAux = string.Empty;
                string localidadAux = string.Empty;
                string provinciaAux = string.Empty;
                string paisAux = string.Empty;

                using (SqlDataReader sqlReader = new SqlCommand
                {
                    CommandText = queriesSQL.sClientes,
                    CommandType = CommandType.Text,
                    CommandTimeout = timeOutQueries,
                    Connection = connDB,
                    Parameters =
                        {
                            new SqlParameter{ ParameterName = "@Cod_Cliente",SqlDbType = SqlDbType.Int,Value = int.Parse(etq.Cod_Cliente)}
                        }
                }.ExecuteReader())
                {
                    if (sqlReader.HasRows)
                    {
                        while (sqlReader.Read())
                        {
                            razonSocAux = sqlReader["Razon_soc"].ToString().Trim();
                            domicilioAux = sqlReader["Domicilio"].ToString().Trim();
                            localidadAux = sqlReader["Localidad"].ToString().Trim();
                            provinciaAux = sqlReader["Provincia"].ToString().Trim();
                            paisAux = sqlReader["Pais"].ToString().Trim();
                        }
                        etq.Remitente = razonSocAux + "-" + domicilioAux + "," + localidadAux + "," + provinciaAux + "," + paisAux;
                    }
                    else
                    {
                        log.GrabarLogs(connLog, severidades.MsgSoporte1, "Error", "No se encontraron datos de Remitente para cliente: " + etq.Cod_Cliente);
                    }
                }
                //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando codigo de barra para IDRemito: " + etq.ID_Remito);

                System.Drawing.Image barcode1 = null;
                System.Drawing.Image barcode2 = null;
                System.Drawing.Image QRcode = null;

                //Se agregan 2 códigos nuevos. Se modifican los codigos de barras para mejorar su resolución y tamaño. Se le sacan los números de debajo para insertarlo como un texto luego
                // Obtengo codigo de barra 1
                try
                {
                    BarcodeWriter bcWriter = new BarcodeWriter();
                    EncodingOptions encodingBC = new EncodingOptions() { Width = 600, Height = 200, Margin = 1, PureBarcode = true };
                    bcWriter.Options = encodingBC;
                    bcWriter.Format = BarcodeFormat.CODE_128;
                    barcode1 = new Bitmap(bcWriter.Write(etq.ID_Remito));
                }
                catch (Exception e)
                {
                    log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo de barra para IDRemito: " + etq.ID_Remito + ". Detalles: " + e.Message);
                }

                //Obtengo codigo de barra 2
                try
                {
                    BarcodeWriter bcWriter = new BarcodeWriter();
                    EncodingOptions encodingBC = new EncodingOptions() { Width = 600, Height = 80, Margin = 1, PureBarcode = true };
                    bcWriter.Options = encodingBC;
                    bcWriter.Format = BarcodeFormat.CODE_128;
                    barcode2 = new Bitmap(bcWriter.Write(etq.Nro_Pedido));
                }
                catch (Exception e)
                {
                    log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo de barra para IDRemito: " + etq.ID_Remito + ". Detalles: " + e.Message);
                }

                //Obtengo codigo QR
                try
                {
                    BarcodeWriter bcWriter = new BarcodeWriter();
                    EncodingOptions encodingQr = new EncodingOptions() { Width = 330, Height = 330, Margin = 1 };
                    bcWriter.Options = encodingQr;
                    bcWriter.Format = BarcodeFormat.QR_CODE;
                    QRcode = new Bitmap(bcWriter.Write(etq.ID_Remito + "|" + etq.Nro_Pedido + "|" + etq.Nro_Remito + "|" + etq.Cod_Cliente + "|" + etq.Nombre_cliente + "|" + etq.Cod_Cia + "|" + etq.Fecha_Remito + "|" + etq.ID_PE_Real + "|" + etq.Domicilio + "|" + etq.Localidad + "|" + etq.Provincia + "|" + etq.Razon_Social + "|" + etq.Bultos + "|" + etq.Kilos + "|" + etq.M3 + "|" + etq.Cantidad));
                }
                catch (Exception e)
                {
                    log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo QR para IDRemito: " + etq.ID_Remito + ". Detalles: " + e.Message);
                }

                double palletsAux = 0;
                double outPallets = -1;
                if (double.TryParse(etq.Pallets, out outPallets))
                    if (outPallets != -1)
                        palletsAux = outPallets;

                int palletsTotal = Convert.ToInt32(palletsAux);

                if (palletsTotal == 0)
                {
                    log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ATENCION", "El remito: " + etq.ID_Remito + " - cliente: " + etq.Cod_Cliente + " tiene 0 pallets, no se generaron etiquetas");
                    sendMail.SendMailLogs();
                    continue;
                }

                double kilos = 0;
                if (etq.Kilos != string.Empty)
                    kilos = Math.Round(double.Parse(etq.Kilos), 3);

                double m3 = 0;
                if (etq.M3 != string.Empty)
                    m3 = Math.Round(double.Parse(etq.M3), 3);

                double cantidad = 0;
                if (etq.Cantidad != string.Empty)
                    cantidad = Math.Round(double.Parse(etq.Cantidad), 3);

                double bultos = 0;
                if (etq.Bultos != string.Empty)
                    bultos = Math.Round(double.Parse(etq.Bultos), 0);

                //Me aseguro del length de los campos

                #region Length campos

                for (int i = 0; i < 100; i++)
                {
                    etq.Provincia += " ";
                    etq.Localidad += " ";
                    etq.Region += " ";
                    etq.Zona += " ";
                    etq.Razon_Social += " ";
                    etq.Domicilio += " ";
                    etq.Remitente += " ";
                    etq.Celular += " ";
                    //MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                }

                #endregion Length campos

                //Se itera la cantidad de pallets para generar un label por cada pallet
                for (int pallet = 1; pallet <= palletsTotal; pallet++)
                {
                    log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando etiqueta para Nro Pedido: " + etq.Nro_Pedido + " - pallet " + pallet.ToString() + " de " + palletsTotal.ToString() + " - Tamaño: " + size + " - Formato: " + format);

                    Bitmap etiqueta = null;
                    using (var bitmap = (Bitmap)etiquetaImg.Clone())    //MODIFICADO - 26/3/20 - v1.0.0.1 - G.SANCINETO - Se añadio el clone()
                    {
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            //INICIO - MODIFICACION - LFC - 7/7/20 - v1.2.0.0
                            //Se generan mas secciones de tamaños de fuente de letras. Se agrega teléfono
                            using (Font arialFont = new Font("Arial", 11, FontStyle.Bold))
                            {
                                //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                //Sirve para alinear a la derecha los numeros
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Far;
                                //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                graphics.DrawString(etq.Fecha_Remito, arialFont, Brushes.Black, pFecha);
                                graphics.DrawString(etq.CE + "-" + etq.Grupo + "-" + etq.Nro_Remito, arialFont, Brushes.Black, pNroRemito);
                                graphics.DrawString(etq.Nro_Pedido, arialFont, Brushes.Black, pNroPedido);
                            }
                            using (Font arialFont = new Font("Arial", 9, FontStyle.Bold))
                            {
                                //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                //Sirve para alinear a la derecha los numeros
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Far;
                                //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                graphics.DrawString(etq.Provincia.Substring(0, 19), arialFont, Brushes.Black, pProvincia);
                                graphics.DrawString(etq.Localidad.Substring(0, 28), arialFont, Brushes.Black, pLocalidad);
                                graphics.DrawString(etq.Region.Substring(0, 20), arialFont, Brushes.Black, pRegion);
                                graphics.DrawString(etq.Zona.Substring(0, 28), arialFont, Brushes.Black, pZona);
                                graphics.DrawString(etq.CP, arialFont, Brushes.Black, pCP);
                                graphics.DrawString(etq.Razon_Social.Substring(0, 25), arialFont, Brushes.Black, pDestinatario);
                                graphics.DrawString(etq.Celular.Substring(0, 25), arialFont, Brushes.Black, pCelular);
                            }
                            using (Font arialFont = new Font("Arial", 11, FontStyle.Bold))
                            {
                                //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                //Sirve para alinear a la derecha los numeros
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Far;
                                //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                graphics.DrawString("Pallet " + pallet.ToString() + " de " + palletsTotal.ToString(), arialFont, Brushes.Black, pPallet);
                                graphics.DrawString(etq.Descripcion_Documento, arialFont, Brushes.Black, pTipoPedido);

                                //Se agregaron formatos de alineacion
                                graphics.DrawString(kilos.ToString(), arialFont, Brushes.Black, pKilos, stringFormat);
                                graphics.DrawString(m3.ToString(), arialFont, Brushes.Black, pM3, stringFormat);
                                graphics.DrawString(cantidad.ToString(), arialFont, Brushes.Black, pCantidad, stringFormat);
                                graphics.DrawString(bultos.ToString(), arialFont, Brushes.Black, pBultos, stringFormat);
                                graphics.DrawString(etq.ID_Remito, arialFont, Brushes.Black, pIDRemito, stringFormat);
                            }
                            using (Font arialFont = new Font("Arial", 8, FontStyle.Bold))
                            {
                                graphics.DrawString(etq.Domicilio.Substring(0, 75), arialFont, Brushes.Black, pDomicilio);
                                graphics.DrawString(etq.Remitente.Substring(0, 70), arialFont, Brushes.Black, pRte); //MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                            }
                            //FIN - MODIFICACION - LFC - 7/7/20 - v1.2.0.0
                        }
                        etiqueta = new Bitmap(bitmap);
                    }

                    //Se agrega el dibujado de los 2 codigos nuevos. Se modifica el tamaño y posicionamiento del codigo de barra ya existente
                    if (etiqueta != null && barcode1 != null)
                    {
                        Graphics gBarcode1 = Graphics.FromImage(etiqueta);
                        gBarcode1.DrawImage(Globals.ResizeImage(barcode1, 600, 200), 1100f, 810f);
                    }

                    if (etiqueta != null && barcode2 != null)
                    {
                        Graphics gBarcode2 = Graphics.FromImage(etiqueta);
                        gBarcode2.DrawImage(Globals.ResizeImage(barcode2, 600, 80), 860f, 300f);
                    }

                    if (etiqueta != null && QRcode != null)
                    {
                        Graphics gBarcode3 = Graphics.FromImage(etiqueta);
                        gBarcode3.DrawImage(Globals.ResizeImage(QRcode, 330, 330), 40f, 415f);
                    }

                    // Fin Etiqueta PALLETS

                    Bitmap hoja = CrearHoja(etiqueta, pageSizeIsA4);
                    hojas.Add(hoja);

                    // Si el path de salida no existe se crea
                    if (!Directory.Exists(gvalues.PathOut))
                        Directory.CreateDirectory(gvalues.PathOut);
                }
            }
        }
        else if (template.ToLower() == "bultosypallets")
        {
            #region Coordenadas texto

            PointF pFecha = new PointF(1225f, 50f);
            PointF pNroRemito = new PointF(1120f, 145f);
            PointF pNroPedido = new PointF(1120f, 244f);
            PointF pProvincia = new PointF(585f, 415f);
            PointF pLocalidad = new PointF(590f, 494f);
            PointF pRegion = new PointF(1250f, 415f);

            PointF pZona = new PointF(1390f, 542f);
            PointF pCP = new PointF(1155f, 540f);

            PointF pDestinatario = new PointF(640f, 581f);
            PointF pDomicilio = new PointF(590f, 665f);

            PointF pBulto = new PointF(45f, 787f);
            PointF pPallet = new PointF(162f, 787f);
            PointF pTipoPedido = new PointF(360f, 882f);

            PointF pKilos = new PointF(180f, 1039f);
            PointF pM3 = new PointF(430f, 1039f);
            PointF pCantidad = new PointF(670f, 1039f);

            PointF pRte = new PointF(170f, 1102f);

            PointF pIDRemito = new PointF(1400f, 1065f);
            PointF pCelular = new PointF(573f, 729f);

            #endregion Coordenadas texto

            foreach (var etq in lEtiquetas)
            {
                //Se buscan datos del remitente
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Se buscarán datos de Remitente para cliente: " + etq.Cod_Cliente);
                string razonSocAux = string.Empty;
                string domicilioAux = string.Empty;
                string localidadAux = string.Empty;
                string provinciaAux = string.Empty;
                string paisAux = string.Empty;

                using (SqlDataReader sqlReader = new SqlCommand
                {
                    CommandText = queriesSQL.sClientes,
                    CommandType = CommandType.Text,
                    CommandTimeout = timeOutQueries,
                    Connection = connDB,
                    Parameters =
                        {
                            new SqlParameter{ ParameterName = "@Cod_Cliente",SqlDbType = SqlDbType.Int,Value = int.Parse(etq.Cod_Cliente)}
                        }
                }.ExecuteReader())
                {
                    if (sqlReader.HasRows)
                    {
                        while (sqlReader.Read())
                        {
                            razonSocAux = sqlReader["Razon_soc"].ToString().Trim();
                            domicilioAux = sqlReader["Domicilio"].ToString().Trim();
                            localidadAux = sqlReader["Localidad"].ToString().Trim();
                            provinciaAux = sqlReader["Provincia"].ToString().Trim();
                            paisAux = sqlReader["Pais"].ToString().Trim();
                        }
                        etq.Remitente = razonSocAux + "-" + domicilioAux + "," + localidadAux + "," + provinciaAux + "," + paisAux;
                    }
                    else
                    {
                        log.GrabarLogs(connLog, severidades.MsgSoporte1, "Error", "No se encontraron datos de Remitente para cliente: " + etq.Cod_Cliente);
                    }
                }
                //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando codigo de barra para IDRemito: " + etq.ID_Remito);
                double bultosAux = 0;
                double outBultos = -1;
                if (double.TryParse(etq.Bultos, out outBultos))
                    if (outBultos != -1)
                        bultosAux = outBultos;

                int bultosTotal = Convert.ToInt32(bultosAux);

                double palletsAux = 0;
                double outPallets = -1;
                if (double.TryParse(etq.Pallets, out outPallets))
                    if (outPallets != -1)
                        palletsAux = outPallets;

                int palletsTotal = Convert.ToInt32(palletsAux);



                if (bultosTotal == 0 && palletsTotal == 0)
                {
                    log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ATENCION", "El remito: " + etq.ID_Remito + " - cliente: " + etq.Cod_Cliente + " tiene 0 bultos y 0 pallets, no se generaron etiquetas");
                    sendMail.SendMailLogs();
                    continue;
                }

                #region iteracion Bultos

                if (bultosTotal != 0)
                {

                    for (int bulto = 1; bulto <= bultosTotal; bulto++)
                    {

                        System.Drawing.Image barcode1 = null;
                        System.Drawing.Image barcode2 = null;
                        System.Drawing.Image QRcode = null;

                        try
                        {
                            BarcodeWriter bcWriter = new BarcodeWriter();
                            EncodingOptions encodingBC = new EncodingOptions() { Width = 600, Height = 200, Margin = 1, PureBarcode = true };
                            bcWriter.Options = encodingBC;
                            bcWriter.Format = BarcodeFormat.CODE_128;
                            barcode1 = new Bitmap(bcWriter.Write(etq.ID_Remito + "|BU|" + bulto));
                        }
                        catch (Exception e)
                        {
                            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo de barra para IDRemito: " + etq.ID_Remito + ". Detalles: " + e.Message);
                        }

                        //Obtengo codigo de barra 2
                        try
                        {
                            BarcodeWriter bcWriter = new BarcodeWriter();
                            EncodingOptions encodingBC = new EncodingOptions() { Width = 600, Height = 80, Margin = 1, PureBarcode = true };
                            bcWriter.Options = encodingBC;
                            bcWriter.Format = BarcodeFormat.CODE_128;
                            barcode2 = new Bitmap(bcWriter.Write(etq.Nro_Pedido));
                        }
                        catch (Exception e)
                        {
                            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo de barra para IDRemito: " + etq.ID_Remito + ". Detalles: " + e.Message);
                        }

                        //Obtengo codigo QR
                        try
                        {
                            BarcodeWriter bcWriter = new BarcodeWriter();
                            EncodingOptions encodingQr = new EncodingOptions() { Width = 330, Height = 330, Margin = 0 };
                            bcWriter.Options.Hints.Add(EncodeHintType.ERROR_CORRECTION, ZXing.QrCode.Internal.ErrorCorrectionLevel.M);
                            bcWriter.Options = encodingQr;
                            bcWriter.Format = BarcodeFormat.QR_CODE;
                            QRcode = new Bitmap(bcWriter.Write(etq.ID_Remito + "|BU|" + bulto + "|" + etq.Nro_Pedido + "|" + etq.Nro_Remito + "|" + etq.Cod_Cliente + "|" + etq.Nombre_cliente + "|" + etq.Cod_Cia + "|" + etq.Fecha_Remito + "|" + etq.ID_PE_Real + "|" + etq.Domicilio + "|" + etq.Localidad + "|" + etq.Provincia + "|" + etq.Razon_Social + "|" + etq.Bultos + "|" + etq.Kilos + "|" + etq.M3 + "|" + etq.Cantidad));
                        }

                        catch (Exception e)
                        {
                            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo QR para IDRemito: " + etq.ID_Remito + ". Detalles: " + e.Message);
                        }



                        double kilos = 0;
                        if (etq.Kilos != string.Empty)
                            kilos = Math.Round(double.Parse(etq.Kilos), 3);

                        double m3 = 0;
                        if (etq.M3 != string.Empty)
                            m3 = Math.Round(double.Parse(etq.M3), 3);

                        double cantidad = 0;
                        if (etq.Cantidad != string.Empty)
                            cantidad = Math.Round(double.Parse(etq.Cantidad), 3);

                        //Me aseguro del length de los campos

                        #region Length campos

                        if (etq.Provincia.Length > 19)
                            etq.Provincia = etq.Provincia.Substring(0, 19);

                        if (etq.Localidad.Length > 28)
                            etq.Localidad = etq.Localidad.Substring(0, 28);

                        if (etq.Region.Length > 20)
                            etq.Region = etq.Region.Substring(0, 20);

                        if (etq.Zona.Length > 28)
                            etq.Zona = etq.Zona.Substring(0, 28);

                        if (etq.Razon_Social.Length > 25)
                            etq.Razon_Social = etq.Razon_Social.Substring(0, 25);

                        if (etq.Domicilio.Length > 50)
                            etq.Domicilio = etq.Domicilio.Substring(0, 50);

                        if (etq.Remitente.Length > 75)
                            etq.Remitente = etq.Remitente.Substring(0, 75);

                        if (etq.Celular.Length > 25)
                            etq.Celular = etq.Celular.Substring(0, 25);


                        #endregion Length campos

                        {
                            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando etiqueta para Nro Pedido: " + etq.Nro_Pedido + " - bulto " + bulto.ToString() + " de " + bultosTotal.ToString() + " - Tamaño: " + size + " - Formato: " + format);

                            Bitmap etiqueta = null;
                            using (var bitmap = (Bitmap)etiquetaImg.Clone())    //MODIFICADO - 26/3/20 - v1.0.0.1 - G.SANCINETO - Se añadio el clone()
                            {
                                using (Graphics graphics = Graphics.FromImage(bitmap))
                                {
                                    //INICIO - MODIFICACION - LFC - 7/7/20 - v1.2.0.0
                                    //Se generan mas secciones de tamaños de fuente de letras. Se agrega teléfono
                                    using (Font arialFont = new Font("Arial", 11, FontStyle.Bold))
                                    {
                                        //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                        //Sirve para alinear a la derecha los numeros
                                        StringFormat stringFormat = new StringFormat();
                                        stringFormat.Alignment = StringAlignment.Far;
                                        stringFormat.LineAlignment = StringAlignment.Far;
                                        //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                        graphics.DrawString(etq.Fecha_Remito, arialFont, Brushes.Black, pFecha);
                                        graphics.DrawString(etq.CE + "-" + etq.Grupo + "-" + etq.Nro_Remito, arialFont, Brushes.Black, pNroRemito);
                                        graphics.DrawString(etq.Nro_Pedido, arialFont, Brushes.Black, pNroPedido);
                                    }
                                    using (Font arialFont = new Font("Arial", 9, FontStyle.Bold))
                                    {
                                        //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                        //Sirve para alinear a la derecha los numeros
                                        StringFormat stringFormat = new StringFormat();
                                        stringFormat.Alignment = StringAlignment.Far;
                                        stringFormat.LineAlignment = StringAlignment.Far;
                                        //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                        graphics.DrawString(etq.Provincia, arialFont, Brushes.Black, pProvincia);
                                        graphics.DrawString(etq.Localidad, arialFont, Brushes.Black, pLocalidad);
                                        graphics.DrawString(etq.Region, arialFont, Brushes.Black, pRegion);
                                        graphics.DrawString(etq.Zona, arialFont, Brushes.Black, pZona);
                                        graphics.DrawString(etq.CP, arialFont, Brushes.Black, pCP);
                                        graphics.DrawString(etq.Razon_Social, arialFont, Brushes.Black, pDestinatario);
                                        graphics.DrawString(etq.Celular, arialFont, Brushes.Black, pCelular);
                                    }
                                    using (Font arialFont = new Font("Arial", 11, FontStyle.Bold))
                                    {
                                        //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                        //Sirve para alinear a la derecha los numeros
                                        StringFormat stringFormat = new StringFormat();
                                        stringFormat.Alignment = StringAlignment.Far;
                                        stringFormat.LineAlignment = StringAlignment.Far;
                                        //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                        graphics.DrawString("Bulto " + bulto.ToString() + " de " + bultosTotal.ToString(), arialFont, Brushes.Black, pBulto);
                                        graphics.DrawString(etq.Descripcion_Documento, arialFont, Brushes.Black, pTipoPedido);

                                        //Se agregaron formatos de alineacion
                                        graphics.DrawString(kilos.ToString(), arialFont, Brushes.Black, pKilos, stringFormat);
                                        graphics.DrawString(m3.ToString(), arialFont, Brushes.Black, pM3, stringFormat);
                                        graphics.DrawString(cantidad.ToString(), arialFont, Brushes.Black, pCantidad, stringFormat);

                                    }
                                    using (Font arialFont = new Font("Arial", 11))
                                    {
                                        StringFormat stringFormat = new StringFormat();
                                        stringFormat.Alignment = StringAlignment.Center;
                                        stringFormat.LineAlignment = StringAlignment.Far;


                                        graphics.DrawString(etq.ID_Remito + "|BU|" + bulto, arialFont, Brushes.Black, pIDRemito, stringFormat);
                                    }
                                    using (Font arialFont = new Font("Arial", 8, FontStyle.Bold))
                                    {
                                        graphics.DrawString(etq.Domicilio, arialFont, Brushes.Black, pDomicilio);
                                        graphics.DrawString(etq.Remitente, arialFont, Brushes.Black, pRte); //MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                    }
                                    //FIN - MODIFICACION - LFC - 7/7/20 - v1.2.0.0
                                }
                                etiqueta = new Bitmap(bitmap);
                            }

                            if (etiqueta != null && barcode1 != null)
                            {
                                Graphics gBarcode1 = Graphics.FromImage(etiqueta);
                                gBarcode1.DrawImage(Globals.ResizeImage(barcode1, 600, 200), 1100f, 810f);
                            }

                            if (etiqueta != null && barcode2 != null)
                            {
                                Graphics gBarcode2 = Graphics.FromImage(etiqueta);
                                gBarcode2.DrawImage(Globals.ResizeImage(barcode2, 600, 80), 860f, 300f);
                            }

                            if (etiqueta != null && QRcode != null)
                            {
                                Graphics gBarcode3 = Graphics.FromImage(etiqueta);
                                gBarcode3.DrawImage(Globals.ResizeImage(QRcode, 330, 330), 40f, 415f);
                            }

                            System.Drawing.Image logochicoImg = null;
                            if (File.Exists(gvalues.PathLogoChico))
                            {
                                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Se obtendrá LOGO de empresa en ruta: " + gvalues.PathLogo);
                                logochicoImg = System.Drawing.Image.FromFile(gvalues.PathLogoChico);
                            }
                            else
                                log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ALERTA", "No se obtuvo LOGO chico de etiqueta en ruta: " + gvalues.PathLogoChico + ". La etiqueta saldrá sin logo");


                            if (logochicoImg != null)
                            {
                                Graphics LogoQR = Graphics.FromImage(etiqueta);
                                LogoQR.DrawImage(Globals.ResizeImage(logochicoImg, 100, 100), 155, 530);
                            }


                            Bitmap hoja = CrearHoja(etiqueta, pageSizeIsA4);
                            hojas.Add(hoja);

                            // Si el path de salida no existe se crea
                            if (!Directory.Exists(gvalues.PathOut))
                                Directory.CreateDirectory(gvalues.PathOut);
                        }
                    }
                }
                #endregion

                #region iteracion Pallets
                if (palletsTotal != 0)
                {
                    //Se itera la cantidad de Pallets para generar un label por cada Pallet
                    for (int pallet = 1; pallet <= palletsTotal; pallet++)
                    {

                        System.Drawing.Image barcode1 = null;
                        System.Drawing.Image barcode2 = null;
                        System.Drawing.Image QRcode = null;

                        try
                        {
                            BarcodeWriter bcWriter = new BarcodeWriter();
                            EncodingOptions encodingBC = new EncodingOptions() { Width = 600, Height = 200, Margin = 1, PureBarcode = true };
                            bcWriter.Options = encodingBC;
                            bcWriter.Format = BarcodeFormat.CODE_128;
                            barcode1 = new Bitmap(bcWriter.Write(etq.ID_Remito + "|PA|" + pallet));
                        }
                        catch (Exception e)
                        {
                            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo de barra para IDRemito: " + etq.ID_Remito + ". Detalles: " + e.Message);
                        }

                        //Obtengo codigo de barra 2
                        try
                        {
                            BarcodeWriter bcWriter = new BarcodeWriter();
                            EncodingOptions encodingBC = new EncodingOptions() { Width = 600, Height = 80, Margin = 1, PureBarcode = true };
                            bcWriter.Options = encodingBC;
                            bcWriter.Format = BarcodeFormat.CODE_128;
                            barcode2 = new Bitmap(bcWriter.Write(etq.Nro_Pedido));
                        }
                        catch (Exception e)
                        {
                            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo de barra para IDRemito: " + etq.ID_Remito + ". Detalles: " + e.Message);
                        }

                        //Obtengo codigo QR
                        try
                        {
                            BarcodeWriter bcWriter = new BarcodeWriter();
                            EncodingOptions encodingQr = new EncodingOptions() { Width = 330, Height = 330, Margin = 0 };
                            bcWriter.Options = encodingQr;
                            bcWriter.Format = BarcodeFormat.QR_CODE;
                            bcWriter.Options.Hints.Add(EncodeHintType.ERROR_CORRECTION, ZXing.QrCode.Internal.ErrorCorrectionLevel.M);
                            QRcode = new Bitmap(bcWriter.Write(etq.ID_Remito + "|PA|" + pallet + "|" + etq.Nro_Pedido + "|" + etq.Nro_Remito + "|" + etq.Cod_Cliente + "|" + etq.Nombre_cliente + "|" + etq.Cod_Cia + "|" + etq.Fecha_Remito + "|" + etq.ID_PE_Real + "|" + etq.Domicilio + "|" + etq.Localidad + "|" + etq.Provincia + "|" + etq.Razon_Social + "|" + etq.Bultos + "|" + etq.Kilos + "|" + etq.M3 + "|" + etq.Cantidad));
                        }
                        catch (Exception e)
                        {
                            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo QR para IDRemito: " + etq.ID_Remito + ". Detalles: " + e.Message);
                        }



                        double kilos = 0;
                        if (etq.Kilos != string.Empty)
                            kilos = Math.Round(double.Parse(etq.Kilos), 3);

                        double m3 = 0;
                        if (etq.M3 != string.Empty)
                            m3 = Math.Round(double.Parse(etq.M3), 3);

                        double cantidad = 0;
                        if (etq.Cantidad != string.Empty)
                            cantidad = Math.Round(double.Parse(etq.Cantidad), 3);

                        //Me aseguro del length de los campos

                        #region Length campos

                        if (etq.Provincia.Length > 19)
                            etq.Provincia = etq.Provincia.Substring(0, 19);

                        if (etq.Localidad.Length > 28)
                            etq.Localidad = etq.Localidad.Substring(0, 28);

                        if (etq.Region.Length > 20)
                            etq.Region = etq.Region.Substring(0, 20);

                        if (etq.Zona.Length > 28)
                            etq.Zona = etq.Zona.Substring(0, 28);

                        if (etq.Razon_Social.Length > 25)
                            etq.Razon_Social = etq.Razon_Social.Substring(0, 25);

                        if (etq.Domicilio.Length > 50)
                            etq.Domicilio = etq.Domicilio.Substring(0, 50);

                        if (etq.Remitente.Length > 75)
                            etq.Remitente = etq.Remitente.Substring(0, 75);

                        if (etq.Celular.Length > 25)
                            etq.Celular = etq.Celular.Substring(0, 25);



                        //for (int i = 0; i < 100; i++)
                        //{
                        //    etq.Provincia += " ";
                        //    etq.Localidad += " ";
                        //    etq.Region += " ";
                        //    etq.Zona += " ";
                        //    etq.Razon_Social += " ";
                        //    etq.Domicilio += " ";
                        //    etq.Remitente += " ";
                        //    etq.Celular += " ";
                        //    //MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                        //}

                        #endregion Length campos

                        {
                            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando etiqueta para Nro Pedido: " + etq.Nro_Pedido + " - pallet " + pallet.ToString() + " de " + palletsTotal.ToString() + " - Tamaño: " + size + " - Formato: " + format);

                            Bitmap etiqueta = null;
                            using (var bitmap = (Bitmap)etiquetaImg.Clone())    //MODIFICADO - 26/3/20 - v1.0.0.1 - G.SANCINETO - Se añadio el clone()
                            {
                                using (Graphics graphics = Graphics.FromImage(bitmap))
                                {
                                    //INICIO - MODIFICACION - LFC - 7/7/20 - v1.2.0.0
                                    //Se generan mas secciones de tamaños de fuente de letras. Se agrega teléfono
                                    using (Font arialFont = new Font("Arial", 11, FontStyle.Bold))
                                    {
                                        //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                        //Sirve para alinear a la derecha los numeros
                                        StringFormat stringFormat = new StringFormat();
                                        stringFormat.Alignment = StringAlignment.Far;
                                        stringFormat.LineAlignment = StringAlignment.Far;
                                        //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                        graphics.DrawString(etq.Fecha_Remito, arialFont, Brushes.Black, pFecha);
                                        graphics.DrawString(etq.CE + "-" + etq.Grupo + "-" + etq.Nro_Remito, arialFont, Brushes.Black, pNroRemito);
                                        graphics.DrawString(etq.Nro_Pedido, arialFont, Brushes.Black, pNroPedido);
                                    }
                                    using (Font arialFont = new Font("Arial", 9, FontStyle.Bold))
                                    {
                                        //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                        //Sirve para alinear a la derecha los numeros
                                        StringFormat stringFormat = new StringFormat();
                                        stringFormat.Alignment = StringAlignment.Far;
                                        stringFormat.LineAlignment = StringAlignment.Far;
                                        //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                        graphics.DrawString(etq.Provincia, arialFont, Brushes.Black, pProvincia);
                                        graphics.DrawString(etq.Localidad, arialFont, Brushes.Black, pLocalidad);
                                        graphics.DrawString(etq.Region, arialFont, Brushes.Black, pRegion);
                                        graphics.DrawString(etq.Zona, arialFont, Brushes.Black, pZona);
                                        graphics.DrawString(etq.CP, arialFont, Brushes.Black, pCP);
                                        graphics.DrawString(etq.Razon_Social, arialFont, Brushes.Black, pDestinatario);
                                        graphics.DrawString(etq.Celular, arialFont, Brushes.Black, pCelular);
                                    }
                                    using (Font arialFont = new Font("Arial", 11, FontStyle.Bold))
                                    {
                                        //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                        //Sirve para alinear a la derecha los numeros
                                        StringFormat stringFormat = new StringFormat();
                                        stringFormat.Alignment = StringAlignment.Far;
                                        stringFormat.LineAlignment = StringAlignment.Far;
                                        //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                        graphics.DrawString("Pallet " + pallet.ToString() + " de " + palletsTotal.ToString(), arialFont, Brushes.Black, pBulto);
                                        graphics.DrawString(etq.Descripcion_Documento, arialFont, Brushes.Black, pTipoPedido);

                                        //Se agregaron formatos de alineacion
                                        graphics.DrawString(kilos.ToString(), arialFont, Brushes.Black, pKilos, stringFormat);
                                        graphics.DrawString(m3.ToString(), arialFont, Brushes.Black, pM3, stringFormat);
                                        graphics.DrawString(cantidad.ToString(), arialFont, Brushes.Black, pCantidad, stringFormat);

                                    }
                                    using (Font arialFont = new Font("Arial", 11))
                                    {
                                        StringFormat stringFormat = new StringFormat();
                                        stringFormat.Alignment = StringAlignment.Center;
                                        stringFormat.LineAlignment = StringAlignment.Far;


                                        graphics.DrawString(etq.ID_Remito + "|PA|" + pallet, arialFont, Brushes.Black, pIDRemito, stringFormat);
                                    }
                                    using (Font arialFont = new Font("Arial", 8, FontStyle.Bold))
                                    {
                                        graphics.DrawString(etq.Domicilio, arialFont, Brushes.Black, pDomicilio);
                                        graphics.DrawString(etq.Remitente, arialFont, Brushes.Black, pRte); //MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                    }
                                    //FIN - MODIFICACION - LFC - 7/7/20 - v1.2.0.0
                                }
                                etiqueta = new Bitmap(bitmap);
                            }

                            //Se agrega el dibujado de los 2 codigos nuevos. Se modifica el tamaño y posicionamiento del codigo de barra ya existente
                            if (etiqueta != null && barcode1 != null)
                            {
                                Graphics gBarcode1 = Graphics.FromImage(etiqueta);
                                gBarcode1.DrawImage(Globals.ResizeImage(barcode1, 600, 200), 1100f, 810f);
                            }

                            if (etiqueta != null && barcode2 != null)
                            {
                                Graphics gBarcode2 = Graphics.FromImage(etiqueta);
                                gBarcode2.DrawImage(Globals.ResizeImage(barcode2, 600, 80), 860f, 300f);
                            }

                            if (etiqueta != null && QRcode != null)
                            {
                                Graphics gBarcode3 = Graphics.FromImage(etiqueta);
                                gBarcode3.DrawImage(Globals.ResizeImage(QRcode, 330, 330), 40f, 415f);
                            }

                            System.Drawing.Image logochicoImg = null;
                            if (File.Exists(gvalues.PathLogoChico))
                            {
                                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Se obtendrá LOGO de empresa en ruta: " + gvalues.PathLogo);
                                logochicoImg = System.Drawing.Image.FromFile(gvalues.PathLogoChico);
                            }
                            else
                                log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ALERTA", "No se obtuvo LOGO chico de etiqueta en ruta: " + gvalues.PathLogoChico + ". La etiqueta saldrá sin logo");


                            if (logochicoImg != null)
                            {
                                Graphics LogoQR = Graphics.FromImage(etiqueta);
                                LogoQR.DrawImage(Globals.ResizeImage(logochicoImg, 100, 100), 155, 530);
                            }


                            Bitmap hoja = CrearHoja(etiqueta, pageSizeIsA4);
                            hojas.Add(hoja);

                            // Si el path de salida no existe se crea
                            if (!Directory.Exists(gvalues.PathOut))
                                Directory.CreateDirectory(gvalues.PathOut);
                        }
                    }
                }

            }
        }
        else if (string.Equals(template.ToUpper(), TEMPLATE_VERTICAL))
        {
            #region Coordenadas texto
            //suerte con esto
            float xBultosVertical = 95f;
            float yBultosVertical = 82f;
            float xBultosVertical_cab = 5f;
            float line = 12f;
            //BV
            PointF pBV_TrackingNo_1 = new PointF(xBultosVertical_cab, 0f);
            PointF pBV_TrackingNo_2 = new PointF(xBultosVertical_cab, 12f);

            PointF pBV_Origen = new PointF(xBultosVertical, yBultosVertical);
            PointF pBV_Origen_Ciudad_1 = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Origen_Ciudad_2 = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Origen_Direccion_1 = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Origen_Direccion_2 = new PointF(xBultosVertical, yBultosVertical += line);

            yBultosVertical += 8f;
            PointF pBV_Destino = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Destino_Ciudad_1 = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Destino_Ciudad_2 = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Destino_Direccion_1 = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Destino_Direccion_2 = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Destino_Contacto = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Destino_Telefono = new PointF(xBultosVertical, yBultosVertical += line);

            yBultosVertical += 5f;
            line = 17f;
            PointF pBV_Cuenta = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_ModEnvio = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Bultos = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Peso = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_IdRemito = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_NroDcto = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_NroPedido = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Conocimiento = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Comentario_1 = new PointF(xBultosVertical, yBultosVertical += line);
            PointF pBV_Comentario_2 = new PointF(xBultosVertical, yBultosVertical += line);
            int countBulto = 0;
            string idRemitoAux = string.Empty;
            #endregion

            foreach (var etq in lEtiquetasVertical)
            {
                //Se buscan datos del remitente
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando codigo de barra para IDRemito: " + etq.IdRemito);

                System.Drawing.Image barcode1 = null;

                //Obtengo codigo de barra 1
                try
                {
                    BarcodeWriter bcWriter = new BarcodeWriter();
                    EncodingOptions encodingBC = new EncodingOptions() { Width = 150, Height = 50, Margin = 0, PureBarcode = true };
                    bcWriter.Options = encodingBC;
                    bcWriter.Format = BarcodeFormat.CODE_128;
                    barcode1 = new Bitmap(bcWriter.Write(etq.TrackingNumber));
                }
                catch (Exception e)
                {
                    log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo de barra para IDRemito: " + etq.IdRemito + ". Detalles: " + e.Message);
                }
                //FIN - MODIFICACION - LFC - 7/7/20 - v1.2.0.0

                //Variables legibles

                double bultosAux = 0;
                double outBultos = -1;
                if (double.TryParse(etq.Bulto, out outBultos))
                    if (outBultos != -1)
                        bultosAux = outBultos;

                int bultosTotal = Convert.ToInt32(bultosAux);

                if (bultosTotal == 0)
                {
                    log.GrabarLogs(connLog, severidades.MsgUsuarios1, "ATENCION", "El remito: " + etq.IdRemito + " - cliente: " + etq.Cod_Cliente + " tiene 0 bultos, no se generaron etiquetas");

                    sendMail.SendMailLogs();

                    if (connDB.State == ConnectionState.Open) connDB.Close();
                    if (connLog.State == ConnectionState.Open) connLog.Close();
                    continue;
                }

                //FIN - MODIFICADO - 1/4/20 - G.SANCINETO - v1.1.0.1
                //INI - MODIFICACION - G.SANCINETO - 26/6/20 - v1.1.2.0
                double kilos = 0;
                if (etq.Peso != string.Empty)
                {
                    kilos = Math.Round(double.Parse(etq.Peso), 3);
                    bultosTotal = 1;
                }
                if (!string.Equals(idRemitoAux, etq.IdRemito))
                {
                    idRemitoAux = etq.IdRemito;
                    countBulto = 0;
                }
                countBulto++;
                //Se itera la cantidad de bultos para generar un label por cada bulto


                for (int bulto = 1; bulto <= bultosTotal; bulto++)
                {
                    log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando etiqueta para Nro Pedido: " + etq.NroPedido + " - bulto " + bulto.ToString() + " de " + bultosTotal.ToString() + " - Tamaño: " + size + " - Formato: " + format);
                    Bitmap etiqueta = null;
                    using (var bitmap = (Bitmap)etiquetaImg.Clone())    //MODIFICADO - 26/3/20 - v1.0.0.1 - G.SANCINETO - Se añadio el clone()
                    {
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            //INICIO - MODIFICACION - LFC - 7/7/20 - v1.2.0.0
                            //Se generan mas secciones de tamaños de fuente de letras. Se agrega teléfono
                            using (Font arial = new Font("Arial", 5, FontStyle.Bold))
                            {
                                int lenLine = 35;
                                //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                //Sirve para alinear a la derecha los numeros
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Far;
                                //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                graphics.DrawString("TRACKING No", arial, Brushes.Black, pBV_TrackingNo_1);
                                graphics.DrawString(etq.TrackingNumber, arial, Brushes.Black, pBV_TrackingNo_2);

                                graphics.DrawString(etq.Origen, arial, Brushes.Black, pBV_Origen);
                                graphics.DrawString(StringHelper.SubstringChecked("Ciudad: " + etq.Origen_Ciudad, lenLine, lenLine - 5)[0], arial, Brushes.Black, pBV_Origen_Ciudad_1);
                                graphics.DrawString(StringHelper.SubstringChecked("Ciudad: " + etq.Origen_Ciudad, lenLine, lenLine - 5)[1], arial, Brushes.Black, pBV_Origen_Ciudad_2);
                                graphics.DrawString(StringHelper.SubstringChecked("Dirección: " + etq.Origen_Direccion, lenLine, lenLine - 5)[0], arial, Brushes.Black, pBV_Origen_Direccion_1);
                                graphics.DrawString(StringHelper.SubstringChecked("Dirección: " + etq.Origen_Direccion, lenLine, lenLine - 5)[1], arial, Brushes.Black, pBV_Origen_Direccion_2);

                                graphics.DrawString(etq.Destino, arial, Brushes.Black, pBV_Destino);
                                graphics.DrawString(StringHelper.SubstringChecked("Ciudad: " + etq.Destino_Ciudad, lenLine, lenLine - 5)[0], arial, Brushes.Black, pBV_Destino_Ciudad_1);
                                graphics.DrawString(StringHelper.SubstringChecked("Ciudad: " + etq.Destino_Ciudad, lenLine, lenLine - 5)[1], arial, Brushes.Black, pBV_Destino_Ciudad_2);
                                graphics.DrawString(StringHelper.SubstringChecked("Dirección: " + etq.Destino_Direccion, lenLine, lenLine)[0], arial, Brushes.Black, pBV_Destino_Direccion_1);
                                graphics.DrawString(StringHelper.SubstringChecked("Dirección: " + etq.Destino_Direccion, lenLine, lenLine)[1], arial, Brushes.Black, pBV_Destino_Direccion_2);
                                graphics.DrawString("Contacto: " + etq.Destino_Contacto, arial, Brushes.Black, pBV_Destino_Contacto);
                                graphics.DrawString("Teléfono: " + etq.Destino_Telefono, arial, Brushes.Black, pBV_Destino_Telefono);

                                graphics.DrawString(etq.Cuenta, arial, Brushes.Black, pBV_Cuenta);
                                graphics.DrawString(etq.ModEnvio, arial, Brushes.Black, pBV_ModEnvio);
                                if (etq.Peso != string.Empty)
                                    graphics.DrawString(countBulto + " de " + lEtiquetasVertical.Where(x => x.IdRemito == etq.IdRemito).Count(), arial, Brushes.Black, pBV_Bultos);
                                else
                                    graphics.DrawString(bulto + " de " + bultosTotal, arial, Brushes.Black, pBV_Bultos);
                                graphics.DrawString(etq.Peso, arial, Brushes.Black, pBV_Peso);
                                graphics.DrawString(etq.IdRemito, arial, Brushes.Black, pBV_IdRemito);
                                graphics.DrawString(etq.NroDcto, arial, Brushes.Black, pBV_NroDcto);
                                graphics.DrawString(etq.NroPedido, arial, Brushes.Black, pBV_NroPedido);
                                graphics.DrawString(etq.Conocimiento, arial, Brushes.Black, pBV_Conocimiento);
                                graphics.DrawString(StringHelper.SubstringChecked(etq.Comentario, lenLine, lenLine - 5)[0], arial, Brushes.Black, pBV_Comentario_1);
                                graphics.DrawString(StringHelper.SubstringChecked(etq.Comentario, lenLine, lenLine - 5)[1], arial, Brushes.Black, pBV_Comentario_2);

                            }
                            //FIN - MODIFICACION - LFC - 7/7/20 - v1.2.0.0
                        }
                        etiqueta = new Bitmap(bitmap);
                    }


                    if (etiqueta != null && barcode1 != null)
                    {
                        Graphics gBarcode1 = Graphics.FromImage(etiqueta);
                        gBarcode1.DrawImage(/*Globals.ResizeImage(*/barcode1/*, 600, 200)*/, xBultosVertical_cab, 30f);
                    }

                    Bitmap hoja = null;
                    Graphics gHoja = null;
                    //Asigno tamaño a la hoja

                    int widthHoja = 283;
                    int heightHoja = 425;
                    hoja = Globals.DrawFilledRectangle(widthHoja, heightHoja);
                    gHoja = Graphics.FromImage(hoja);

                    gHoja.DrawImage(/*Globals.ResizeImage(*/etiqueta/*, widthHoja, heightHoja)*/, 0, 0);

                    hojas.Add(hoja);


                    //Si el path de salida no existe se crea
                    if (!Directory.Exists(gvalues.PathOut))
                        Directory.CreateDirectory(gvalues.PathOut);
                }
            }
        }
        else
        {
            //Coordenadas para cada texto

            #region Coordenadas texto

            PointF pFecha = new PointF(1225f, 50f);
            PointF pNroRemito = new PointF(1120f, 145f);
            PointF pNroPedido = new PointF(1120f, 244f);

            PointF pProvincia = new PointF(242f, 405f);
            PointF pLocalidad = new PointF(975f, 403f);
            PointF pRegion = new PointF(202f, 502f);
            PointF pZona = new PointF(893f, 502f);
            PointF pCP = new PointF(330f, 598f);
            PointF pDestinatario = new PointF(1030f, 598f);
            PointF pDomicilio = new PointF(242f, 704f);

            //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
            //Se modificaron alturas de ploteos y se agrego el de remitente
            PointF pBulto = new PointF(162f, 787f);
            PointF pTipoPedido = new PointF(360f, 882f);

            PointF pKilos = new PointF(180f, 1039f);
            PointF pM3 = new PointF(430f, 1039f);
            PointF pCantidad = new PointF(670f, 1039f);

            PointF pRte = new PointF(170f, 1102f);
            //FIN   - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

            #endregion Coordenadas texto

            foreach (var etq in lEtiquetas)
            {
                //Se buscan datos del remitente
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Se buscarán datos de Remitente para cliente: " + etq.Cod_Cliente);
                string razonSocAux = string.Empty;
                string domicilioAux = string.Empty;
                string localidadAux = string.Empty;
                string provinciaAux = string.Empty;
                string paisAux = string.Empty;
                //MODIFICACION - 26/06/20 - G.SANCINETO - v1.1.1.2 - Se modifico la query principal sacandole en concat

                using (SqlDataReader sqlReader = new SqlCommand
                {
                    CommandText = queriesSQL.sClientes,
                    CommandType = CommandType.Text,
                    CommandTimeout = timeOutQueries,
                    Connection = connDB,
                    Parameters =
                        {
                            new SqlParameter{ ParameterName = "@Cod_Cliente",SqlDbType = SqlDbType.Int,Value = int.Parse(etq.Cod_Cliente)}
                        }
                }.ExecuteReader())
                {
                    if (sqlReader.HasRows)
                    {
                        while (sqlReader.Read())
                        {
                            razonSocAux = sqlReader["Razon_soc"].ToString().Trim();
                            domicilioAux = sqlReader["Domicilio"].ToString().Trim();
                            localidadAux = sqlReader["Localidad"].ToString().Trim();
                            provinciaAux = sqlReader["Provincia"].ToString().Trim();
                            paisAux = sqlReader["Pais"].ToString().Trim();
                        }
                        etq.Remitente = razonSocAux + "-" + domicilioAux + "," + localidadAux + "," + provinciaAux + "," + paisAux;
                        //MODIFICACION - 26/06/20 - G.SANCINETO - v1.1.1.2 - Se modifico la query principal sacandole en concat
                    }
                    else
                    {
                        log.GrabarLogs(connLog, severidades.MsgSoporte1, "Error", "No se encontraron datos de Remitente para cliente: " + etq.Cod_Cliente);
                    }
                }
                //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando codigo de barra para IDRemito: " + etq.ID_Remito);

                System.Drawing.Image barcode = null;
                //Obtengo codigo de barra
                try
                {
                    BarcodeWriter bcWriter = new BarcodeWriter();
                    bcWriter.Format = BarcodeFormat.CODE_39;
                    barcode = new Bitmap(bcWriter.Write(etq.Nro_Remito));   //MODIFICACION - G.SANCINETO - 26/6/20 - v1.1.2.0
                }
                catch (Exception e)
                {
                    log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo de barra para IDRemito: " + etq.ID_Remito + ". Detalles: " + e.Message);
                }

                //Variables legibles

                double bultosAux = 0;
                double outBultos = -1;
                if (double.TryParse(etq.Bultos, out outBultos))
                    if (outBultos != -1)
                        bultosAux = outBultos;

                int bultosTotal = Convert.ToInt32(bultosAux);

                double kilos = 0;
                if (etq.Kilos != string.Empty)
                    kilos = Math.Round(double.Parse(etq.Kilos), 3);

                double m3 = 0;
                if (etq.M3 != string.Empty)
                    m3 = Math.Round(double.Parse(etq.M3), 3);

                double cantidad = 0;
                if (etq.Cantidad != string.Empty)
                    cantidad = Math.Round(double.Parse(etq.Cantidad), 3);
                //FIN - MODIFICACION - G.SANCINETO - 26/6/20 - v1.1.2.0

                //Me aseguro del length de los campos

                #region Length campos

                for (int i = 0; i < 100; i++)
                {
                    etq.Provincia += " ";
                    etq.Localidad += " ";
                    etq.Region += " ";
                    etq.Zona += " ";
                    etq.Razon_Social += " ";
                    etq.Domicilio += " ";
                    etq.Remitente += " ";   //MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                }

                #endregion Length campos

                //Se itera la cantidad de bultos para generar un label por cada bulto
                for (int bulto = 1; bulto <= bultosTotal; bulto++)
                {
                    log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando etiqueta para Nro Pedido: " + etq.Nro_Pedido + " - bulto " + bulto.ToString() + " de " + bultosTotal.ToString() + " - Tamaño: " + size + " - Formato: " + format);

                    Bitmap etiqueta = null;
                    using (var bitmap = (Bitmap)etiquetaImg.Clone())    //MODIFICADO - 26/3/20 - v1.0.0.1 - G.SANCINETO - Se añadio el clone()
                    {
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            using (Font arialFont = new Font("Arial", 11, FontStyle.Bold))
                            {
                                //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                //Sirve para alinear a la derecha los numeros
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Far;
                                //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0

                                graphics.DrawString(etq.Fecha_Remito, arialFont, Brushes.Black, pFecha);
                                graphics.DrawString(etq.CE + "-" + etq.Grupo + "-" + etq.Nro_Remito, arialFont, Brushes.Black, pNroRemito);
                                graphics.DrawString(etq.Nro_Pedido, arialFont, Brushes.Black, pNroPedido);

                                graphics.DrawString(etq.Provincia.Substring(0, 19), arialFont, Brushes.Black, pProvincia);
                                graphics.DrawString(etq.Localidad.Substring(0, 28), arialFont, Brushes.Black, pLocalidad);
                                graphics.DrawString(etq.Region.Substring(0, 20), arialFont, Brushes.Black, pRegion);
                                graphics.DrawString(etq.Zona.Substring(0, 28), arialFont, Brushes.Black, pZona);
                                graphics.DrawString(etq.CP, arialFont, Brushes.Black, pCP);
                                graphics.DrawString(etq.Razon_Social.Substring(0, 25), arialFont, Brushes.Black, pDestinatario);

                                graphics.DrawString(bulto.ToString() + " de " + bultosTotal.ToString(), arialFont, Brushes.Black, pBulto);
                                graphics.DrawString(etq.Descripcion_Documento, arialFont, Brushes.Black, pTipoPedido);

                                //INICIO - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                                //Se agregaron formatos de alineacion
                                graphics.DrawString(kilos.ToString(), arialFont, Brushes.Black, pKilos, stringFormat);
                                graphics.DrawString(m3.ToString(), arialFont, Brushes.Black, pM3, stringFormat);
                                graphics.DrawString(cantidad.ToString(), arialFont, Brushes.Black, pCantidad, stringFormat);
                                //FIN    - MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                            }
                            using (Font arialFont = new Font("Arial", 8, FontStyle.Bold))
                            {
                                graphics.DrawString(etq.Domicilio.Substring(0, 75), arialFont, Brushes.Black, pDomicilio);
                                graphics.DrawString(etq.Remitente.Substring(0, 70), arialFont, Brushes.Black, pRte); //MODIFICACION - #3466 - G.SANCINETO - 15/4/20 - v1.1.1.0
                            }
                        }
                        etiqueta = new Bitmap(bitmap);
                    }

                    if (etiqueta != null && barcode != null)
                    {
                        Graphics gBarcode = Graphics.FromImage(etiqueta);
                        gBarcode.DrawImage(Globals.ResizeImage(barcode, 600, 250), 1100f, 810f);
                    }

                    Bitmap hoja = CrearHoja(etiqueta, pageSizeIsA4);
                    hojas.Add(hoja);

                    //Si el path de salida no existe se crea
                    if (!Directory.Exists(gvalues.PathOut))
                        Directory.CreateDirectory(gvalues.PathOut);
                }
            }
        }
        #endregion

        return hojas;
    }


}
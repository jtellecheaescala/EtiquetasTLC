using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Tecnologistica;

/// <summary>
/// Summary description for HandlersEtiquetasAppleDB
/// </summary>
public class HandlersEtiquetasAppleDB
{
    private QueriesSQL queriesSQL = new QueriesSQL();

    public List<Remito> ObtenerRemitosBultosDHLApple(SqlConnection connDB, Log log, SqlConnection connLog, Globals.staticValues.SeveridadesClass severidades, int timeOutQueries, string sIDRemito, string cliente, out string message)
    {
        List<Remito> remitos = new List<Remito>();
        string cantidadTotal = string.Empty;
        message = String.Empty;
        int etiquetasCount = 0;

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
                message = "ERROR: Error buscando cantidad en Remitos para IDRemito: " + sIDRemito + " - cliente: " + cliente;
                return remitos;
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
        }
        return remitos;
    }
}
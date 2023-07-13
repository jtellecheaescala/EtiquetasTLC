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
public class RepositorioEtiquetasApple
{
    private QueriesSQL queriesSQL = new QueriesSQL();

    public List<EtiquetaBultoDHLApple> ObtenerDatosEtiquetasBultosDHLApple(SqlConnection connDB, Log log, SqlConnection connLog, Globals.staticValues.SeveridadesClass severidades, int timeOutQueries, string sIDRemito, string cliente, string nroOperacion, string nroViaje, string idPallet, out string message)
    {
        List<EtiquetaBultoDHLApple> lstEtiquetas = new List<EtiquetaBultoDHLApple>();
        string cantidadTotal = string.Empty;
        message = String.Empty;
        int etiquetasCount = 0;


        using (SqlDataReader readerData = new SqlCommand
        {
            Connection = connDB,
            CommandType = CommandType.Text,
            CommandTimeout = timeOutQueries,
            CommandText = queriesSQL.remitosBultosDHLApple,
            Parameters =
                        {
                            new SqlParameter {ParameterName = "@NroViaje", SqlDbType = SqlDbType.Int, Value = int.Parse(nroViaje)},
                            new SqlParameter {ParameterName = "@NroOperacion", SqlDbType = SqlDbType.Int, Value = int.Parse(nroOperacion)},
                            new SqlParameter {ParameterName = "@IdRemito", SqlDbType = SqlDbType.Int, Value = int.Parse(sIDRemito)},
                            new SqlParameter {ParameterName = "@IdPallet", SqlDbType = SqlDbType.Int, Value =  String.IsNullOrEmpty(idPallet) ? DBNull.Value : (object) idPallet}
                        }
        }.ExecuteReader())
        {
            if (readerData.HasRows)
            {
                while (readerData.Read())
                {
                    etiquetasCount++;

                    EtiquetaBultoDHLApple etiqueta = new EtiquetaBultoDHLApple();

                    etiqueta.Nro_Viaje = StringHelper.LimpiarCampo(readerData["Nro_Viaje"]);
                    etiqueta.Cod_cliente = StringHelper.LimpiarCampo(readerData["Cod_cliente"]);
                    etiqueta.Nro_Operacion = StringHelper.LimpiarCampo(readerData["Nro_Operacion"]);
                    etiqueta.ID_Remito = StringHelper.LimpiarCampo(readerData["ID_Remito"]);
                    etiqueta.Bultos = StringHelper.LimpiarCampo(readerData["Bultos"]);
                    etiqueta.ParcelId = StringHelper.LimpiarCampo(readerData["ParcelId"]);
                    etiqueta.Parada = StringHelper.LimpiarCampo(readerData["Parada"]);
                    etiqueta.CantParadasTotales = StringHelper.LimpiarCampo(readerData["CantParadasTotales"]);
                    etiqueta.FechaViaje = StringHelper.LimpiarCampo(readerData["FechaViaje"]);
                    etiqueta.NroReintento = StringHelper.LimpiarCampo(readerData["NroReintento"]);

                    lstEtiquetas.Add(etiqueta);
                }
            }
            else
            {
                message = String.Format("No se encontraron datos para las etiquetas BultosDHLApple para los datos Viaje: {0}, Nro Operacion {1}, Id Remito {2}, IdPallet: {3}", nroViaje, nroOperacion, sIDRemito, idPallet);
            }
        }
        return lstEtiquetas;
    }

    public List<EtiquetaBultoDHLApple> ObtenerDatosEtiquetasViajesBultosDHLApple(SqlConnection connDB, Log log, SqlConnection connLog, Globals.staticValues.SeveridadesClass severidades, int timeOutQueries, string sIDRemito, string cliente, string nroOperacion, string nroViaje, string idPallet, out string message)
    {
        List<EtiquetaBultoDHLApple> lstEtiquetas = new List<EtiquetaBultoDHLApple>();
        string cantidadTotal = string.Empty;
        message = String.Empty;
        int etiquetasCount = 0;


        using (SqlDataReader readerData = new SqlCommand
        {
            Connection = connDB,
            CommandType = CommandType.Text,
            CommandTimeout = timeOutQueries,
            CommandText = queriesSQL.viajesDHLApple,
            Parameters =
                        {
                            new SqlParameter {ParameterName = "@NroViaje", SqlDbType = SqlDbType.Int, Value = int.Parse(nroViaje)},
                            new SqlParameter {ParameterName = "@NroOperacion", SqlDbType = SqlDbType.Int, Value = int.Parse(nroOperacion)},
                            new SqlParameter {ParameterName = "@IdRemito", SqlDbType = SqlDbType.Int, Value = int.Parse(sIDRemito)},
                            new SqlParameter {ParameterName = "@IdPallet", SqlDbType = SqlDbType.Int, Value =  String.IsNullOrEmpty(idPallet) ? DBNull.Value : (object) int.Parse(idPallet)}
                        }
        }.ExecuteReader())
        {
            if (readerData.HasRows)
            {
                while (readerData.Read())
                {
                    etiquetasCount++;

                    EtiquetaBultoDHLApple etiqueta = new EtiquetaBultoDHLApple();

                    etiqueta.Nro_Viaje = StringHelper.LimpiarCampo(readerData["Nro_Viaje"]);
                    etiqueta.Cod_cliente = StringHelper.LimpiarCampo(readerData["Cod_cliente"]);
                    etiqueta.Nro_Operacion = StringHelper.LimpiarCampo(readerData["Nro_Operacion"]);
                    etiqueta.ID_Remito = StringHelper.LimpiarCampo(readerData["ID_Remito"]);
                    etiqueta.Bultos = StringHelper.LimpiarCampo(readerData["Bultos"]);
                    etiqueta.ParcelId = StringHelper.LimpiarCampo(readerData["ParcelId"]);
                    etiqueta.Parada = StringHelper.LimpiarCampo(readerData["Parada"]);
                    etiqueta.CantParadasTotales = StringHelper.LimpiarCampo(readerData["CantParadasTotales"]);
                    etiqueta.FechaViaje = StringHelper.LimpiarCampo(readerData["FechaViaje"]);
                    etiqueta.NroReintento = StringHelper.LimpiarCampo(readerData["NroReintento"]);

                    lstEtiquetas.Add(etiqueta);
                }
            }
            else
            {
                message = String.Format("No se encontraron bultos para las etiquetas para los datos Viaje: {0}, Nro Operacion {1}, Id Remito {2}, IdPallet: {3}", nroViaje, nroOperacion, sIDRemito, idPallet);
            }
        }
        return lstEtiquetas;
    }
}
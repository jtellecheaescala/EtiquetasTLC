using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tecnologistica
{
    /// <summary>
    /// Summary description for QueriesSQL
    /// </summary>
    internal class QueriesSQL
    {
        internal string sRemitos = @"SELECT
                                    ID_Remito, Remitos.Cod_cliente,
                                    CE,	Grupo,
                                    Nro_Remito,	Nro_Pedido,
                                    Puntos_de_entrega.Domicilio, Region,
                                    Provincias.Provincia, Localidades.Localidad,
                                    Zonas.Zona, Puntos_de_Entrega.CP,
                                    Puntos_de_entrega.Razon_Soc, /*Domicilio,*/
                                    Bultos, Pallets, Tipos_Documentos.Descripcion,
                                    Kilos, m3,
                                    Cantidad, Fecha_Remito,
                                    ID_PE_Real, Cod_cia,
                                    Clientes.Razon_soc as Nombre_cliente,
                                    Contacto_Celular
                                    FROM REMITOS
                                    INNER JOIN Puntos_de_Entrega WITH(NOLOCK) ON ID_PE_Real=ID_PE
                                    INNER JOIN Localidades WITH(NOLOCK) ON Localidades.Cod_Loc=Puntos_de_Entrega.Cod_Loc
                                    INNER JOIN Provincias WITH(NOLOCK) ON Localidades.Cod_Prov=Provincias.Cod_Prov
                                    INNER JOIN Zonas WITH(NOLOCK) ON Puntos_de_Entrega.Cod_zona=zonas.Cod_zona
                                    INNER JOIN Regiones WITH(NOLOCK) ON Provincias.Cod_Region =Regiones.Cod_Region
                                    INNER JOIN Tipos_Documentos WITH(NOLOCK) ON Tipos_Documentos.Cod_Tipo_Doc=Remitos.Cod_Tipo_Doc
                                    INNER JOIN Clientes WITH(NOLOCK) ON Clientes.Cod_cliente=Remitos.Cod_cliente
                                    WHERE ID_Remito = @IDRemito AND Remitos.cod_cliente = @Cod_Cliente
                                    ORDER BY Fecha_Remito DESC";

        internal string sClientes = "" +
                                    "   SELECT 										" +
                                    "   	Razon_soc, 									" +
                                    "   	Domicilio,									" +
                                    "   	Localidad,									" +
                                    "   	Provincia,									" +
                                    "   	paises.Descripcion AS 'Pais'                " +
                                    "   FROM Clientes WITH(NOLOCK)						" +
                                    "   INNER  JOIN Localidades WITH(NOLOCK) ON 		" +
                                    "   	clientes.Cod_Loc = Localidades.Cod_loc		" +
                                    "   INNER JOIN Provincias WITH(NOLOCK) ON 			" +
                                    "   	Localidades.Cod_Prov = Provincias.Cod_Prov	" +
                                    "   INNER JOIN Paises WITH(NOLOCK) ON 				" +
                                    "   	Provincias.Cod_pais = Paises.Cod_Pais		" +
                                    "   	WHERE Cod_cliente=@Cod_Cliente              ";

        //MODIFICACION - 26/06/20 - G.SANCINETO - v1.1.1.2 - Se modifico la query principal sacandole en concat

        internal string sClientes_Usuario = "SELECT COUNT(*) AS 'Cantidad' FROM Clientes_Usuario WITH(NOLOCK) WHERE User_ID = @Usuario AND Cod_Cliente = @Cod_Cliente";

        internal string sIf_Parametros = "SELECT COALESCE(TOMADOR,'N') AS 'TOMADOR' FROM If_Parametros WITH(NOLOCK) WHERE Interfaz = 'VALIDO_USUARIO_CLIENTE_WS_ETIQUETAS'";


        internal readonly string sRemitosVertical = @"
            SELECT
                R.Nro_Guia_Transporte AS TrackingNo, -- Nueva columna de tabla Remitos
	            CASE 
		            WHEN R.Origen IS NOT NULL THEN POR.Razon_Soc
		            WHEN TD.Inverso=1 THEN PE.Razon_Soc 
		            ELSE POP.Razon_Soc 
	            END AS Origen,
	            CASE 
		            WHEN R.Origen IS NOT NULL THEN LOR.Localidad
		            WHEN TD.Inverso=1 THEN LPE.Localidad 
		            ELSE LOP.localidad 
	            END AS Origen_Ciudad,
	            CASE 
		            WHEN R.Origen IS NOT NULL THEN POR.Domicilio
		            WHEN TD.Inverso=1 THEN PE.Domicilio 
		            ELSE POP.Domicilio 
	            END AS Origen_Direccion,
	            CASE 
		            WHEN R.Origen IS NOT NULL THEN PE.Razon_Soc
		            WHEN TD.Inverso=1 THEN POP.Razon_Soc 
		            ELSE PE.Razon_Soc 
	            END AS Destino,
	            CASE 
		            WHEN R.Origen IS NOT NULL THEN LPE.Localidad
		            WHEN TD.Inverso=1 THEN LOP.Localidad 
		            ELSE LPE.Localidad 
	            END AS Destino_Ciudad,
	            CASE 
		            WHEN R.Origen IS NOT NULL THEN PE.Domicilio
		            WHEN TD.Inverso=1 THEN POP.Domicilio 
		            ELSE PE.Domicilio 
	            END AS Destino_Direccion,
	            CASE 
		            WHEN R.Origen IS NOT NULL THEN COALESCE(PE.Contactos,'') 
		            WHEN TD.Inverso=1 THEN COALESCE(POP.Contactos,'') 
		            ELSE COALESCE(PE.Contactos,'') 
	            END AS Destino_Contacto,
	            CASE 
		            WHEN R.Origen IS NOT NULL THEN COALESCE(PE.Contacto_celular,'') 
		            WHEN TD.Inverso=1 THEN COALESCE(POP.Contacto_celular,'') 
		            ELSE COALESCE(PE.Contacto_celular,'') 
	            END AS Destino_Telefono,
	            C.Razon_soc AS Cuenta,
	            '' AS ModEnvio,
                R.Bultos AS Bultos,
	            RB.Kilos AS Peso, --  vacío si toma el total de Remitos, Remitos_Bultos.Kilossi toma de remitos_bultos, as Peso,
	            R.ID_Remito AS IdRemito,
	            R.Cod_Tipo_Doc+'-'+R.Grupo+'-'+RIGHT(CONVERT(varchar(5),10000+R.CE),4)+'-'+RIGHT(CONVERT(varchar(10),100000000+R.Nro_Remito),8) AS NroDcto,
	            COALESCE(R.Nro_Pedido,'') AS NroPedido,
	            COALESCE(R.Nro_Conocimiento_Cliente,'') AS Conocimiento,
	            COALESCE(r.Comentario ,'') AS Comentario -- Antes bultos vertical tenia para lo que entre en dos renglones. Ya no porque el pdf se adapta.
            FROM Remitos R WITH(NOLOCK)
            INNER JOIN Tipos_Documentos TD WITH(NOLOCK) ON R.Cod_Tipo_Doc = TD.Cod_tipo_doc
            INNER JOIN Operaciones O WITH(NOLOCK) ON R.Nro_Operacion_Primaria = O.Nro_operacion
            INNER JOIN Puntos_de_Entrega POP WITH(NOLOCK) ON O.ID_PE = POP.ID_PE
            INNER JOIN Localidades LOP WITH(NOLOCK) ON LOP.Cod_Loc = POP.Cod_Loc
            INNER JOIN Puntos_de_Entrega PE WITH(NOLOCK) ON R.ID_PE_Real = PE.ID_PE
            INNER JOIN Localidades LPE WITH(NOLOCK) ON PE.Cod_Loc = LPE.Cod_loc
            LEFT JOIN Puntos_de_Entrega POR WITH(NOLOCK) ON R.Origen = POR.ID_PE
            LEFT JOIN Localidades LOR WITH(NOLOCK) ON POR.Cod_Loc = LOR.Cod_loc
            INNER JOIN Clientes C WITH(NOLOCK) ON R.Cod_cliente = C.Cod_cliente
            LEFT JOIN Remitos_Bultos RB WITH(NOLOCK) ON R.ID_Remito = RB.ID_Remito
                WHERE R.Cod_cliente=@Cod_Cliente AND R.ID_Remito=@Id_Remito";


        internal readonly string sRemitos_Bultos = @"SELECT COUNT(*) AS Cantidad FROM Remitos_Bultos WITH(NOLOCK) WHERE ID_Remito = @ID_Remito";

        internal readonly string cant_BultosXD = @"select 
                                                    case when coalesce(r.pallets,0) > 0 then 
	                                                    case when coalesce(topeCont.valor,0)=0 or coalesce(r.pallets,0) <= coalesce(topeCont.valor,0) then (ceiling(r.pallets) + coalesce(adic.valor,0)) 
		                                                    else (topeCont.valor + coalesce(adic.valor,0)) end 
                                                    else 
	                                                    case when r.bultos is null or r.bultos = 0 then (1 + coalesce(adic.valor,0)) 
		                                                    when coalesce(tope.valor,0)=0 or coalesce(r.bultos,1) <= coalesce(tope.valor,0) then (ceiling(r.bultos) + coalesce(adic.valor,0)) 
		                                                    else (tope.valor + coalesce(adic.valor,0)) end 
	                                                    end as Cantidad 
                                                    from remitos r with (nolock) 
                                                    left join variables tope with (nolock) on tope.campo='Etiquetas_WS_Tope'
                                                    left join variables topeCont with (nolock) on topeCont.campo='Etiquetas_WS_Tope_Contenedores'
                                                    left join variables adic with (nolock) on adic.campo='Etiquetas_WS_Adicionales' 
                                                    where r.id_remito=@IdRemito";

        internal readonly string sRemitosBultosXD = @"select o.descripcion as Origen, 
	                                                    o.domicilio as Domicilio, 
	                                                    l.localidad as Localidad, 
	                                                    peope.Contacto_Email as Mail, 
	                                                    f.Sitio_Web as Url, 
	                                                    r.id_remito as Nro_seguimiento, 
	                                                    r.fecha_hora_recepcion as Fecha, 
	                                                    pedes.cod_cliente_sap as Destino_cod, 
	                                                    ceiling(r.bultos) as Bultos, 
	                                                    pedes.razon_soc as Destino_razon_soc, 
                                                        ts.Descripcion as Tipo_servicio
                                                    from remitos r with (nolock) 
                                                    inner join operaciones o with (nolock) on r.nro_operacion_primaria=o.nro_operacion 
                                                    inner join filiales f with (nolock) on o.nro_filial=f.nro_filial 
                                                    inner join puntos_de_entrega peope with (nolock) on o.id_pe=peope.id_pe 
                                                    inner join localidades l with (nolock) on peope.cod_loc=l.cod_loc 
                                                    inner join puntos_de_entrega pedes with (nolock) on r.id_pe_teorico=pedes.id_pe 
                                                    left join Tipos_Servicio ts with(nolock) on r.cod_tipo_servicio = ts.cod_tipo_servicio 
                                                    where r.Cod_cliente=@CodCliente and r.id_remito=@IdRemito";

        internal readonly string remitosBultosDHLApple = @"SELECT DISTINCT VI.Nro_Viaje AS 'Nro_Viaje'
                                            			          , RE.Cod_cliente AS 'Cod_cliente'
                                            			          , VI.Nro_Operacion AS 'Nro_Operacion'
                                            			          , RE.ID_Remito AS 'ID_Remito'
                                            			          , RE.Bultos AS 'Bultos'
                                            			          , RB.ID_Pallet AS 'ParcelId'
                                            			          , -- Se debe imprimir una etiqueta por cada Id_Pallet
                                            				          PA.Orden_Entrega AS 'Parada'
                                            			          , GR.CantParadasTotales AS 'CantParadasTotales'
                                            			          , DV.Fecha_Hora_Ruteo AS 'FechaViaje'
                                            			          , [dbo].[FN_NRO_INTENTO_REMITO](RE.ID_Remito, VI.Nro_Operacion, VI.Nro_Viaje) AS 'NroReintento'
                                                          FROM Remitos RE WITH (NOLOCK)
                                                          JOIN Remitos_Viaje RV WITH (NOLOCK)
                                                          	       ON RV.ID_Remito = RE.ID_Remito
                                                          JOIN (SELECT DISTINCT ID_Remito
                                                          					, ID_Pallet
                                                          FROM Remitos_Bultos rb1 WITH (NOLOCK)) RB
                                                          	ON RB.ID_Remito = RE.ID_Remito
                                                          JOIN Viajes VI WITH (NOLOCK)
                                                          	ON VI.Nro_Viaje = RV.Nro_Viaje
                                                          		AND VI.Nro_Operacion = RV.Nro_Operacion
                                                          JOIN Datos_del_Viaje DV WITH (NOLOCK)
                                                          	ON DV.Nro_Viaje = VI.Nro_Viaje
                                                          		AND VI.Nro_Operacion = DV.Nro_Operacion
                                                          JOIN Paradas PA WITH (NOLOCK)
                                                          	ON PA.Nro_Viaje = VI.Nro_Viaje
                                                          		AND PA.Nro_Operacion = VI.Nro_Operacion
                                                          		AND RV.ID_Parada = PA.ID_Parada
                                                          
                                                          JOIN (SELECT Nro_Viaje
                                                          		   , Nro_Operacion
                                                          		   , MAX( Orden_Entrega ) AS CantParadasTotales
                                                          	    FROM Paradas WITH (NOLOCK)
                                                          	    GROUP BY Nro_Viaje
                                                          		   , Nro_Operacion) GR
                                                          	ON GR.Nro_Viaje = PA.Nro_Viaje
                                                          		AND GR.Nro_Operacion = PA.Nro_Operacion

                                                          WHERE VI.Nro_Viaje = @NroViaje
                                                          	AND VI.Nro_Operacion = @NroOperacion
                                                          	AND RE.ID_Remito = @IdRemito
                                                            AND (@IdPallet IS NULL OR rb.ID_Pallet = @IdPallet)";

        internal readonly string viajesDHLApple = @"
                                                    SELECT VI.Nro_Operacion AS 'Nro_Operacion'
                                                    	 , VI.Nro_Viaje AS 'Nro_Viaje'
                                                    	 , COUNT( DISTINCT CONVERT(VARCHAR(20),RV.ID_Remito) + RB.ID_Pallet) AS 'CantBultos'
                                                    	 , COUNT( DISTINCT RV.ID_Remito ) AS 'CantOrdenes'
                                                    	 , MAX( DV.Fecha_Hora_Ruteo ) AS 'FechaRuteo'
                                                    	 , COALESCE( MAX( VI.Fecha_Hora_Salida ), MAX( DV.Fecha_Hora_Presentacion ) ) AS 'FechaEstimadaSalida'
                                                    FROM Remitos RE WITH (NOLOCK)
                                                    JOIN Remitos_Viaje RV WITH (NOLOCK)
                                                    	ON RV.ID_Remito = RE.ID_Remito
                                                    JOIN Viajes VI WITH (NOLOCK)
                                                    	ON VI.Nro_Viaje = RV.Nro_Viaje
                                                    		AND VI.Nro_Operacion = RV.Nro_Operacion
                                                    JOIN Datos_del_Viaje DV WITH (NOLOCK)
                                                    	ON DV.Nro_Viaje = VI.Nro_Viaje
                                                    		AND VI.Nro_Operacion = DV.Nro_Operacion
                                                    LEFT JOIN Remitos_Bultos RB WITH (NOLOCK)
                                                    	ON RE.ID_Remito = RB.ID_Remito
                                                    WHERE VI.Nro_Viaje = @NroViaje
                                                    	AND VI.Nro_Operacion = @NroOperacion
                                                    GROUP BY VI.Nro_Viaje
                                                    	   , VI.Nro_Operacion";

    }
}
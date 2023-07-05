using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models
{
    public class Etiqueta
    {
        public string ID_Remito;
        public string Cod_Cliente;
        public string CE;
        public string Grupo;
        public string Nro_Remito;
        public string Nro_Pedido;
        public string Domicilio;
        public string Region;
        public string Provincia;
        public string Localidad;
        public string Zona;
        public string CP;
        public string Razon_Social;
        public string Bultos;
        public string Pallets;
        public string Descripcion_Documento;
        public string Kilos;
        public string M3;
        public string Cantidad;
        public string Fecha_Remito;
        public string Remitente;
        public string ID_PE_Real;
        public string Cod_Cia;
        public string Nombre_cliente;
        public string Celular;

        public Etiqueta(string idRemito, string codCliente, string strCE, string grupo, string nroRemito, string nroPedido, string domicilio, string region,
            string provincia, string localidad, string zona, string strCP, string razonSocial, string bultos, string pallets, string descripcionDoc, string kilos, string m3,
            string cantidad, string fechaRemito, string IDPEREAL, string CodCia, string NombreCliente, string Contacto_Celular)
        {
            ID_Remito = idRemito;
            Cod_Cliente = codCliente;
            CE = strCE;
            Grupo = grupo;
            Nro_Remito = nroRemito;
            Nro_Pedido = nroPedido;
            Domicilio = domicilio;
            Region = region;
            Provincia = provincia;
            Localidad = localidad;
            Zona = zona;
            CP = strCP;
            Razon_Social = razonSocial;
            Bultos = bultos;
            Pallets = pallets;
            Descripcion_Documento = descripcionDoc;
            Kilos = kilos;
            M3 = m3;
            Cantidad = cantidad;
            Fecha_Remito = fechaRemito;
            ID_PE_Real = IDPEREAL;
            Cod_Cia = CodCia;
            Nombre_cliente = NombreCliente;
            Celular = Contacto_Celular;
        }
    }
}
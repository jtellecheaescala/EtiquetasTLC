using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for DHL
/// </summary>
public class BultosVertical
{
    public string TrackingNumber { get; set; }
    public string Origen { get; set; }
    public string Origen_Ciudad { get; set; }
    public string Origen_Direccion { get; set; }
    public string Destino { get; set; }
    public string Destino_Ciudad { get; set; }
    public string Destino_Direccion { get; set; }
    public string Destino_Contacto { get; set; }
    public string Destino_Telefono { get; set; }
    public string Cuenta { get; set; }
    public string ModEnvio { get; set; }
    public string Bulto { get; set; }
    public string Peso { get; set; }
    public string IdRemito { get; set; }
    public string NroDcto { get; set; }
    public string NroPedido { get; set; }
    public string Conocimiento { get; set; }
    public string Comentario { get; set; }
    public string Cod_Cliente { get; set; }
    public BultosVertical(string trackingNo, string origen, string origen_ciudad, string origen_direccion, string destino, string destino_ciudad, string destino_direccion, string destino_contacto,
        string destino_telefono, string cuenta, string modEnvio, string bulto, string peso, string idRemito, string nroDcto, string nroPedido, string conocimiento,
        string comentario, string codCliente)
    {
        TrackingNumber = trackingNo;
        Origen = origen;
        Origen_Ciudad = origen_ciudad;
        Origen_Direccion = origen_direccion;
        Destino = destino;
        Destino_Ciudad = destino_ciudad;
        Destino_Direccion = destino_direccion;
        Destino_Contacto = destino_contacto;
        Destino_Telefono = destino_telefono;
        Cuenta = cuenta;
        ModEnvio = modEnvio;
        Bulto = bulto;
        Peso = peso;
        IdRemito = idRemito;
        NroDcto = nroDcto;
        NroPedido = nroPedido;
        Conocimiento = conocimiento;
        Comentario = comentario;
        Cod_Cliente = codCliente;
    }
}
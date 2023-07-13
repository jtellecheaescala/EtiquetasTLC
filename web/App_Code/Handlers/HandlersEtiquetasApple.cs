using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using Tecnologistica;
using ZXing;
using ZXing.Common;

/// <summary>
/// Summary description for HandlersEtiquetasApple
/// </summary>
public class HandlersEtiquetasApple
{
    private Log log;
    private SqlConnection connLog;
    private Globals.staticValues.SeveridadesClass severidades;
    private string MODO_OBTENCION_ARCHIVO;
    private Globals.staticValues gvalues;
    private PageSize pageSize;
    private Archivo archivoPdf = new Archivo();
    private string ruta;
    private PdfWriter writer;
    private PdfDocument pdf;
    private Document document;
    private ImageData logo;
    private string size;
    private string etiqueta;


    public HandlersEtiquetasApple(Log _log, SqlConnection _connLog, Globals.staticValues.SeveridadesClass _severidades, string _MODO_OBTENCION_ARCHIVO, Globals.staticValues _gvalues, string _size, string _etiqueta)
    {
        this.log = _log;
        this.connLog = _connLog;
        this.severidades = _severidades;
        this.MODO_OBTENCION_ARCHIVO = _MODO_OBTENCION_ARCHIVO;
        this.gvalues = _gvalues;
        this.size = _size;
        this.etiqueta = _etiqueta;
        InicializarHandler();
    }

    public void InicializarHandler()
    {
        try
        {
            ruta = String.Format(@"{0}\{1}_{2}.pdf", gvalues.PathOut, etiqueta, DateTime.Now.ToString("yyyyMMdd-HHmmssffff"));

            writer = new PdfWriter(ruta);
            pdf = new PdfDocument(writer);

            if (size.ToUpper().Equals("A4"))
            {
                pageSize = PageSize.A4;
            }
            else
            {
                pageSize = new PageSize(285, 220);
            }

            document = new Document(pdf, pageSize);
            document.SetMargins(0, 1, 0, 0);

            if (!Directory.Exists(gvalues.PathOut))
            {
                Directory.CreateDirectory(gvalues.PathOut);
            }

            logo = ImageDataFactory.Create(gvalues.PathLogo);
        }
        catch (Exception ex)
        {
            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", String.Format("Error al inicializar handler etiquetas Apple: {0}", ex.Message));
            throw ex;
        }
    }

    public Archivo GenerarPDFBultosDHLViajesApple(List<Remito> remitos, string size, string format)
    {
        try
        {
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
                    if (r.bultosXD.Bultos.Length >= 4)
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
            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "ERROR", "ERROR CREANDO ARCHIVO PDF" + e.Message.ToString());

            return archivoPdf;
        }
        finally
        {
            document.Close();
            writer.Close();
            pdf.Close();
        }

    }

    public Archivo GenerarPDFBultosDHLApple(List<EtiquetaBultoDHLApple> etiquetas, string size, string format)
    {

        try
        {
            foreach (var etiqueta in etiquetas)
            {
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando etiquetas BultosDHLApple para IDRemito: " + etiqueta.ID_Remito + " - Tamaño: " + size + " - Formato: " + format);

                Table tbHeader = new Table(UnitValue.CreatePercentArray(new float[] { 60, 40 }));

                tbHeader.AddCell(new Cell().Add(new Paragraph("Viaje:").SetFontSize(13).SetTextAlignment(TextAlignment.LEFT).SetBold().SetMinHeight(15).SetMaxHeight(40).SetMaxWidth(400))
                                                    .SetBorder(Border.NO_BORDER))
                                .SetMinHeight(35).SetMaxHeight(35);

                tbHeader.AddCell(new Cell().Add(new Paragraph("Parada:").SetFontSize(13).SetTextAlignment(TextAlignment.LEFT).SetBold().SetMinHeight(15).SetMaxHeight(40).SetMaxWidth(400).SetMarginLeft(-25))

                                         .SetBorder(Border.NO_BORDER))
                     .SetMinHeight(35).SetMaxHeight(35);

                tbHeader.SetWidth(UnitValue.CreatePercentValue(100));
                tbHeader.SetHeight(UnitValue.CreatePercentValue(100));
                tbHeader.SetMargins(6, 1, 0, 6);

                document.Add(tbHeader);

                int fontSizeParadas = 50;
                var vAlignBultos = VerticalAlignment.TOP;

                Table tbBody = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }));
                tbBody.AddCell(new Cell().Add(new Paragraph(etiqueta.Nro_Viaje.PadLeft(6, '0')).SetVerticalAlignment(vAlignBultos).SetFontSize(30).SetTextAlignment(TextAlignment.LEFT).SetBold()).SetMinHeight(100).SetMaxHeight(100).SetMaxWidth(50).SetBorder(Border.NO_BORDER));
                tbBody.AddCell(new Cell().Add(new Paragraph(String.Format("{0}/{1}", etiqueta.Parada.PadLeft(2, '0'), etiqueta.CantParadasTotales.PadLeft(2, '0'))).SetVerticalAlignment(vAlignBultos).SetFontSize(fontSizeParadas).SetTextAlignment(TextAlignment.LEFT).SetBold()).SetMinHeight(100).SetMaxWidth(50).SetMaxHeight(100).SetBorder(Border.NO_BORDER));
                tbBody.SetWidth(UnitValue.CreatePercentValue(100));
                tbBody.SetMargins(6, 1, 0, 6);

                document.Add(tbBody);

                int fontSizeFooter = 14;

                Table tbFooter = new Table(UnitValue.CreatePercentArray(new float[] { 300, 60 }));

                tbFooter.AddCell(new Cell().Add(new Paragraph(String.Format("Intento: {0}", etiqueta.NroReintento)).SetFontSize(fontSizeFooter).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(15).SetMaxHeight(500).SetMaxWidth(800))
                                                    .Add(new Paragraph(String.Format("Parcel ID: {0}", etiqueta.ParcelId.PadLeft(10, '0'))).SetMultipliedLeading(1).SetFontSize(fontSizeFooter).SetTextAlignment(TextAlignment.LEFT).SetBold().SetMinHeight(12).SetMaxHeight(500).SetMaxWidth(800))
                                                    .Add(new Paragraph(String.Format("Fecha Viaje: {0}", etiqueta.FechaViaje)).SetMultipliedLeading(1).SetFontSize(fontSizeFooter).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(12).SetMaxHeight(500).SetMaxWidth(3000))
                                                    .SetBorder(Border.NO_BORDER))
                                .SetMinHeight(35).SetMaxHeight(60);

                tbFooter.SetWidth(UnitValue.CreatePercentValue(100));
                tbFooter.SetHeight(UnitValue.CreatePercentValue(100));
                tbFooter.SetMargins(6, 1, 0, 6);

                document.Add(tbFooter);
            }

            pdf.Close();

            string fileName = System.IO.Path.GetFileName(ruta);
            if (MODO_OBTENCION_ARCHIVO == "BASE64")
            {
                Byte[] pdfEnByte = File.ReadAllBytes(ruta);
                archivoPdf.base64 = Convert.ToBase64String(pdfEnByte);
                archivoPdf.nombre = fileName;

                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Etiqueta PDFBultosVertical BultosDHLApple generada con exito Tamaño: " + size + " - Formato: " + format + ". Modo elegido: BASE64");
            }
            else if (MODO_OBTENCION_ARCHIVO == "URL")
            {
                string url = System.IO.Path.Combine(gvalues.RaizURL, fileName);
                archivoPdf.url = url;
                archivoPdf.nombre = fileName;
                log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Etiqueta BultosDHLApple generada con exito - Tamaño: " + size + " - Formato: " + format + ". Modo elegido: URL");
            }

            return archivoPdf;
        }
        catch (Exception e)
        {
            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "ERROR", "ERROR CREANDO ARCHIVO PDF BultosDHLApple" + e.Message.ToString());

            throw e;
        }
        finally
        {
            document.Close();
            writer.Close();
            pdf.Close();
        }
    }

}
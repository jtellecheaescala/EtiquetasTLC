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
    private string format;
    private string etiqueta;


    public HandlersEtiquetasApple(Log _log, SqlConnection _connLog, Globals.staticValues.SeveridadesClass _severidades, string _MODO_OBTENCION_ARCHIVO, Globals.staticValues _gvalues, string _size, string _etiqueta, string _format)
    {
        this.log = _log;
        this.connLog = _connLog;
        this.severidades = _severidades;
        this.MODO_OBTENCION_ARCHIVO = _MODO_OBTENCION_ARCHIVO;
        this.gvalues = _gvalues;
        this.size = _size;
        this.etiqueta = _etiqueta;
        this.format = _format;
        InicializarHandler();
    }

    public void InicializarHandler()
    {
        try
        {


            ruta = String.Format(@"{0}\{1}_{2}.pdf", gvalues.PathOut, etiqueta, DateTime.Now.ToString("yyyyMMdd-HHmmssffff"));
            log.GrabarLogs(connLog, severidades.MsgSoporte1, "INFO", String.Format("Inicializando handler etiquetas BULTOS_DHL_APPLE. Generando ruta: {0}", ruta));

            writer = new PdfWriter(ruta);
            pdf = new PdfDocument(writer);

            if (size.ToUpper().Equals("A4"))
            {
                pageSize = PageSize.A4;
            }
            else
            { // new PageSize(285, 220): representa: 100,5 x 77,6
              // new PageSize(170, 85):  representa: a 60 x 30 
                pageSize = new PageSize(285, 220);
            }

            document = new Document(pdf, pageSize);
            document.SetMargins(0, 1, 0, 0);

            if (!Directory.Exists(gvalues.PathOut))
            {
                Directory.CreateDirectory(gvalues.PathOut);
            }

        }
        catch (Exception ex)
        {
            log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", String.Format("Error al inicializar handler etiquetas Apple: {0}, Detalle: {1}", ex.Message, ex.StackTrace));

            throw ex;
        }
    }

    public Archivo GenerarPDFBultosDHLViajesApple(EtiquetaBultoViajeDHLApple etiqueta, out string message)
    {
        message = null;
        try
        {
            try
            {
                logo = ImageDataFactory.Create(gvalues.PathLogo);
            }
            catch (Exception e)
            {
                log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar logo de barra para el viaje: " + etiqueta.Nro_Viaje + ". Detalles: " + e.Message);
            }
            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando codigo de barra para viaje: " + etiqueta.Nro_Viaje);


            System.Drawing.Image barcode1 = null;
            ImageData codigoBarras = null;

            try
            {
                BarcodeWriter bcWriter = new BarcodeWriter();
                EncodingOptions encodingBC = new EncodingOptions() { Width = 400, Height = 100, Margin = 0, PureBarcode = true };
                bcWriter.Options = encodingBC;
                bcWriter.Format = BarcodeFormat.CODE_128;
                barcode1 = new Bitmap(bcWriter.Write(etiqueta.Nro_Viaje));
                var img = new ImageHelper().ImageToByte(barcode1);
                codigoBarras = ImageDataFactory.Create(img);
            }
            catch (Exception e)
            {
                log.GrabarLogs(connLog, severidades.MsgSoporte1, "ERROR", "No se pudo generar codigo de barra para el viaje: " + etiqueta.Nro_Viaje + ". Detalles: " + e.Message);
            }

            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "Notificacion", "Generando etiquetas para Viaje: " + etiqueta.Nro_Viaje + " - Tamaño: " + size + " - Formato: " + format);



            Table tbHeader = new Table(UnitValue.CreatePercentArray(new float[] { 60, 20 }));

            tbHeader.AddCell(new Cell().Add(new Paragraph(String.Empty).SetFontSize(11).SetTextAlignment(TextAlignment.CENTER).SetBold().SetMinHeight(15).SetMaxHeight(15).SetMaxWidth(160))
                                              .Add(new Paragraph("Rutas").SetMultipliedLeading(1).SetFontSize(10).SetTextAlignment(TextAlignment.LEFT).SetMinHeight(12).SetMaxHeight(12).SetMaxWidth(160)).SetBorder(Border.NO_BORDER))
                            .SetMinHeight(35).SetMaxHeight(35).SetBorder(Border.NO_BORDER);


            tbHeader.AddCell(new Cell().Add(new iText.Layout.Element.Image(logo).SetAutoScale(true).SetPadding(0).SetMargins(2, 5, 0, 0).SetHorizontalAlignment(HorizontalAlignment.RIGHT)).SetBorder(Border.NO_BORDER))
                .SetMinHeight(75).SetMaxHeight(75);
            tbHeader.SetWidth(UnitValue.CreatePercentValue(100));
            tbHeader.SetHeight(UnitValue.CreatePercentValue(40));
            tbHeader.SetMargins(6, 2, 0, 2);

            document.Add(tbHeader);


            Table tbBody = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }));
            tbBody.AddCell(new Cell().Add(new Paragraph("Numero de Viaje:").SetFontSize(13).SetTextAlignment(TextAlignment.CENTER).SetBold()));
            tbBody.AddCell(new Cell().Add(new Paragraph(etiqueta.Nro_Viaje).SetFontSize(13).SetTextAlignment(TextAlignment.CENTER).SetBold()));

            tbBody.AddCell(new Cell().Add(new Paragraph("# Bultos:").SetFontSize(13).SetTextAlignment(TextAlignment.CENTER).SetBold()));
            tbBody.AddCell(new Cell().Add(new Paragraph(etiqueta.Cantidad_Bultos).SetFontSize(13).SetTextAlignment(TextAlignment.CENTER).SetBold()));

            tbBody.AddCell(new Cell().Add(new Paragraph("# Ordenes:").SetFontSize(13).SetTextAlignment(TextAlignment.CENTER).SetBold()));
            tbBody.AddCell(new Cell().Add(new Paragraph(etiqueta.Cantidad_Ordenes).SetFontSize(13).SetTextAlignment(TextAlignment.CENTER).SetBold()));

            tbBody.SetWidth(UnitValue.CreatePercentValue(100));
            tbBody.SetMargins(0, 2, 0, 2);

            document.Add(tbBody);

            Cell celdaCodigoBarras = (new Cell().Add(new iText.Layout.Element.Image(codigoBarras).SetAutoScale(true).SetMargins(4, 0, 0, 0).SetHorizontalAlignment(HorizontalAlignment.CENTER)));

            Table tbFooter = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }));
            tbFooter.AddCell(new Cell()
                .Add(new Paragraph("Fecha ruteo: " + "01-01-2024").SetFontSize(11).SetTextAlignment(TextAlignment.LEFT).SetBold().SetMargins(15, 0, 0, 2))
                .Add(new Paragraph("Fecha estimada salida: " + "01-01-2024").SetFontSize(11).SetTextAlignment(TextAlignment.LEFT).SetBold().SetMargins(15, 0, 0, 2))
                );
            tbFooter.AddCell(celdaCodigoBarras.SetBorder(Border.NO_BORDER));
            tbFooter.SetWidth(UnitValue.CreatePercentValue(100));
            tbFooter.SetMargins(4, 2, 0, 2);

            document.Add(tbFooter);

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

    public Archivo GenerarPDFBultosDHLApple(List<EtiquetaBultoDHLApple> etiquetas)
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
            log.GrabarLogs(connLog, severidades.NovedadesEjecucion, "ERROR", "ERROR CREANDO ARCHIVO PDF BultosViajeDHLApple" + e.Message.ToString());

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
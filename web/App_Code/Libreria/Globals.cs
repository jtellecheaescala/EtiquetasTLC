using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Tecnologistica
{
    public class Globals
    {
        public class staticValues
        {
            public string NombreInterfazEnLogs = ConfigurationManager.AppSettings["NombreInterfazEnLogs"];
            public string ConnectionDefaultDatasource = ConfigurationManager.AppSettings["Connection-Default-Datasource"];
            public string ConnectionDefaultUser = ConfigurationManager.AppSettings["Connection-Default-User"];
            public string ConnectionDefaultPassword = ConfigurationManager.AppSettings["Connection-Default-Password"];
            public string ConnectionDefaultDB = ConfigurationManager.AppSettings["Connection-Default-DB"];
            public string ConnectionLogsDB = ConfigurationManager.AppSettings["Connection-Logs-DB"];

            public string ParPathIn = ConfigurationManager.AppSettings["ParPathIn"];
            public string ParPathOut = ConfigurationManager.AppSettings["ParPathOut"];
            public string ParPathBackUp = ConfigurationManager.AppSettings["ParPathBackUp"];
            public string ParPathError = ConfigurationManager.AppSettings["ParPathError"];
            public string Separador = ConfigurationManager.AppSettings["Separador"];

            public string ValidUserPassword = ConfigurationManager.AppSettings["ValidUserPassword"];

            public string MailFrom = ConfigurationManager.AppSettings["MailFrom"];
            public string MailSubject = ConfigurationManager.AppSettings["MailSubject"];
            public string MailServer = ConfigurationManager.AppSettings["MailServer"];
            public string MailTLS = ConfigurationManager.AppSettings["MailTLS"];
            public string MailPort = ConfigurationManager.AppSettings["MailPort"];
            public string MailUsername = ConfigurationManager.AppSettings["MailUsername"];
            public string MailPassword = ConfigurationManager.AppSettings["MailPassword"];
            public string MailDomain = ConfigurationManager.AppSettings["MailDomain"].Trim();
            public string MailUrl = ConfigurationManager.AppSettings["MailUrl"];
            public string MailExchVer = ConfigurationManager.AppSettings["MailExchangeVersion"];

            public string[] MailTo1 = ConfigurationManager.AppSettings["MailTo1"].Replace(" ", "").Split(',');
            public string[] MailCC1 = ConfigurationManager.AppSettings["MailCC1"].Replace(" ", "").Split(',');
            public string[] MailBCC1 = ConfigurationManager.AppSettings["MailBCC1"].Replace(" ", "").Split(',');
            public string[] MailSeveridad1 = ConfigurationManager.AppSettings["MailSeveridad1"].Replace(" ", "").Split(',');

            public string[] MailTo2 = ConfigurationManager.AppSettings["MailTo2"].Replace(" ", "").Split(',');
            public string[] MailCC2 = ConfigurationManager.AppSettings["MailCC2"].Replace(" ", "").Split(',');
            public string[] MailBCC2 = ConfigurationManager.AppSettings["MailBCC2"].Replace(" ", "").Split(',');
            public string[] MailSeveridad2 = ConfigurationManager.AppSettings["MailSeveridad2"].Replace(" ", "").Split(',');

            public string[] MailTo3 = ConfigurationManager.AppSettings["MailTo3"].Replace(" ", "").Split(',');
            public string[] MailCC3 = ConfigurationManager.AppSettings["MailCC3"].Replace(" ", "").Split(',');
            public string[] MailBCC3 = ConfigurationManager.AppSettings["MailBCC3"].Replace(" ", "").Split(',');
            public string[] MailSeveridad3 = ConfigurationManager.AppSettings["MailSeveridad3"].Replace(" ", "").Split(',');

            public string[] Severidades = ConfigurationManager.AppSettings["Severidades"].Replace(" ", "").Split(',');

            public string QueryTimeOut = ConfigurationManager.AppSettings["QueryTimeOut"];
            public string PathOut = ConfigurationManager.AppSettings["PathOut"];
            public string PathBack = ConfigurationManager.AppSettings["PathBack"];

            public string GeneraBackup = ConfigurationManager.AppSettings["GeneraBackup"];
            public string ModoObtencionArchivo = ConfigurationManager.AppSettings["ModoObtencionArchivo"];
            public string RaizURL = ConfigurationManager.AppSettings["RaizURL"];

            public string PathImagen = ConfigurationManager.AppSettings["PathImagen"];
            public string PathLogo = ConfigurationManager.AppSettings["PathLogo"];
            public string PathLogoChico = ConfigurationManager.AppSettings["PathLogoChico"];

            public string version = "5.0.0.2";

            public class SeveridadesClass
            {
                internal int NovedadesEjecucion = 1;
                internal int MsgUsuarios1 = 2;
                internal int MsgUsuarios2 = 3;
                internal int MsgSoporte1 = 4;
                internal int MsgSoporte2 = 5;
                internal int Debug = 6;
            }

            public bool severidadCheck(string[] severidades, int severidad)
            {
                foreach (var item in severidades)
                {
                    if ((item != "" && int.Parse(item) == severidad) || (item != "" && int.Parse(item) == 6))
                    {
                        return true;
                    }
                }
                return false;
            }
        };

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static void ImagesToPdf(Bitmap image, string pdfpath)
        {
            iTextSharp.text.Rectangle pageSize = null;
            byte[] imageData = null;
            using (var stream = new MemoryStream())
            {
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                imageData = stream.ToArray();
            }

            pageSize = new iTextSharp.text.Rectangle(0, 0, image.Width, image.Height);

            using (var ms = new MemoryStream())
            {
                var document = new iTextSharp.text.Document(pageSize, 0, 0, 0, 0);
                iTextSharp.text.pdf.PdfWriter.GetInstance(document, ms).SetFullCompression();
                document.Open();
                var imageResult = iTextSharp.text.Image.GetInstance(imageData);
                document.Add(imageResult);
                document.Close();

                File.WriteAllBytes(pdfpath, ms.ToArray());
            }
        }

        public static byte[] ImagesToSinglePdf(List<Bitmap> images, string pdfpath)
        {
            iTextSharp.text.Rectangle pageSize = null;
            byte[] imageData;
            pageSize = new iTextSharp.text.Rectangle(0, 0, images[0].Width, images[0].Height);

            using (var ms = new MemoryStream())
            {
                var document = new iTextSharp.text.Document(pageSize, 0, 0, 0, 0);
                iTextSharp.text.pdf.PdfWriter.GetInstance(document, ms).SetFullCompression();
                document.Open();
                foreach (Bitmap image in images)
                {
                    MemoryStream stream = new MemoryStream();
                    image.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    imageData = stream.ToArray();
                    stream.Close();

                    var imageResult = iTextSharp.text.Image.GetInstance(imageData);
                    document.Add(imageResult);
                    document.NewPage();
                }

                document.Close();

                File.WriteAllBytes(pdfpath, ms.ToArray());
                return ms.ToArray();
            }
        }

        public static Bitmap DrawFilledRectangle(int x, int y)
        {
            Bitmap bmp = new Bitmap(x, y);
            using (Graphics graph = Graphics.FromImage(bmp))
            {
                Rectangle ImageSize = new Rectangle(0, 0, x, y);
                graph.FillRectangle(Brushes.White, ImageSize);
            }
            return bmp;
        }
    }
}
﻿using iText.IO.Image;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using Tecnologistica;
using ZXing;
using ZXing.Common;

public class ImageHelper
{
    public byte[] ImageToByte(System.Drawing.Image img)
    {
        using (var stream = new MemoryStream())
        {
            img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return stream.ToArray();
        }
    }


    public Bitmap CrearHoja(Bitmap etiqueta, Boolean pageSizeIsA4, Globals Globals)
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

    public ImageData CrearCodigoBarras(string codigo)
    {

        ImageData codigoBarras = null;
        Image barcode1 = null;

        BarcodeWriter bcWriter = new BarcodeWriter();
        EncodingOptions encodingBC = new EncodingOptions() { Width = 400, Height = 100, Margin = 0, PureBarcode = true };
        bcWriter.Options = encodingBC;
        bcWriter.Format = BarcodeFormat.CODE_128;
        barcode1 = new Bitmap(bcWriter.Write(codigo));
        var img = new ImageHelper().ImageToByte(barcode1);
        codigoBarras = ImageDataFactory.Create(img);

        return codigoBarras;

    }



    public Image CrearCodigoQR(string codigo, int width, int height)
    {
        System.Drawing.Image QRcode = null;

        BarcodeWriter bcWriter = new BarcodeWriter();
        EncodingOptions encodingQr = new EncodingOptions() { Width = width, Height = height, Margin = 1 };
        bcWriter.Options = encodingQr;
        bcWriter.Format = BarcodeFormat.QR_CODE;
        QRcode = new Bitmap(bcWriter.Write(codigo));

        return QRcode;
    }

    public iText.Layout.Element.Image CrearCodigoQRIText(string codigo,int width, int height)
    {
        var codigoQR = CrearCodigoQR(codigo, width,height);

        ImageData imagen = ImageDataFactory.Create(ImageToByteArray(codigoQR));

        return new iText.Layout.Element.Image(imagen);
    }



    public byte[] ImageToByteArray(System.Drawing.Image image)
    {
        using (var ms = new MemoryStream())
        {
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return ms.ToArray();
        }
    }
}
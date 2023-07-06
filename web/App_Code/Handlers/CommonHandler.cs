using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;


public class CommonHandler
{
    public byte[] ImageToByte(System.Drawing.Image img)
    {
        using (var stream = new MemoryStream())
        {
            img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return stream.ToArray();
        }
    }
}
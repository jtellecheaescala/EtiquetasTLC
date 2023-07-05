using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models
{
    public class Response
    {
        public string message;
        public List<Archivo> Archivos = new List<Archivo>();
    }
}
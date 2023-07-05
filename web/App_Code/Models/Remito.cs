using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models
{
    public class Remito
    {
        public int ID_Remito;
        public List<Etiqueta> etiquetas;
        public List<BultosVertical> etiquetasVerticales;
        public BultosXD bultosXD;


        public Remito(int ID_Remito, List<Etiqueta> etiquetas)
        {
            this.ID_Remito = ID_Remito;
            this.etiquetas = etiquetas;
        }

        public Remito(int ID_Remito, List<BultosVertical> etiquetasDHL)
        {
            this.ID_Remito = ID_Remito;
            this.etiquetasVerticales = etiquetasDHL;
        }

        public Remito(int ID_Remito, BultosXD bultosXD)
        {
            this.ID_Remito = ID_Remito;
            this.bultosXD = bultosXD;
        }
    }
}
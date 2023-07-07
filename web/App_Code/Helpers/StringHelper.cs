using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public static class StringHelper
{
    ////esto de aca sirve para separar en 2 renglones un string input es el string que queres separar
    ///lenMax es el largo maximo que aguanta el renglon donde lo quieras meter
    ///len es la cantidad de caracteres donde queres cortar
    public static string[] SubstringChecked(string input, int lenMax, int len)
    {
        string[] ret = new string[2];
        if (input.Length > lenMax)
        {
            ret[0] = input.Substring(0, len);
            ret[1] = input.Substring(len);
        }
        else
            ret[0] = input;

        return ret;
    }

    public static string LimpiarCampo(object campo)
    {
        string str = "";

        if (campo != null)
        {
            str = campo.ToString().Trim();
        }

        return str;
    }
}
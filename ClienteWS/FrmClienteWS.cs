using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ClienteWS
{
    public partial class FrmClienteWS : Form
    {
        public FrmClienteWS()
        {
            InitializeComponent();
        }

        private void btnInvocar_Click(object sender, EventArgs e)
        {
            try
            {
                ServiceReferenceWSEtiquetas.WSEtiquetasSoapClient ws = new ServiceReferenceWSEtiquetas.WSEtiquetasSoapClient();

                ws.Endpoint.Address = new System.ServiceModel.EndpointAddress(txtURLWebService.Text);

                int separarPorDoc = String.IsNullOrEmpty(txtSepararPorDocumento.Text) ? 0 : Convert.ToInt32(txtSepararPorDocumento.Text);
               
                // Eliminar los siguientes comentarios si el servicio es https.
                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var response = ws.EtiquetasWS(txtUser.Text, txtPassword.Text, txtIdRemito.Text, txtCliente.Text, txtFormat.Text, txtSize.Text, separarPorDoc, txtTemplate.Text);

                txtResultado.Text = response.message;
            }
            catch (Exception ex)
            {
                txtResultado.Text = $"Se ha producido un error:  {ex.Message}. {Environment.NewLine} {ex.StackTrace} ";
            }

        }
    }
}

using System.Data.SqlClient;

namespace Tecnologistica
{
    public class ConnectionSQL
    {
        private static Globals.staticValues gvalues = new Globals.staticValues();
        public SqlConnection connect(char mode)
        {
            SqlConnection conn = new SqlConnection();
            SqlConnectionStringBuilder connStringBuilder;
            connStringBuilder = new SqlConnectionStringBuilder();
            connStringBuilder.DataSource = gvalues.ConnectionDefaultDatasource;
            if (mode == 'D')
            {
                connStringBuilder.InitialCatalog = gvalues.ConnectionDefaultDB;
            }
            else if (mode == 'L')
            {
                connStringBuilder.InitialCatalog = gvalues.ConnectionLogsDB;
            }
            connStringBuilder.UserID = gvalues.ConnectionDefaultUser;
            connStringBuilder.Password = gvalues.ConnectionDefaultPassword;
            connStringBuilder.MultipleActiveResultSets = true;
            conn = new SqlConnection(connStringBuilder.ToString());
            
            return conn;
        }
    }
}

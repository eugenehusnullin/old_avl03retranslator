using System.Data;
using System.Data.SqlClient;

namespace OnlineMonitoring.ServerCore.DataBase
{
    public abstract class ClsCon
    {
        //Local protected Variable to this clscntprp property
        protected SqlConnection Con = new SqlConnection();

        //setting values Con SqlConnection variable
        protected ClsCon(string connectionString)
        {
            Con.ConnectionString = connectionString;
        }

        /// <summary>
        /// Opens the connection, if not opened
        /// </summary>
        protected void Open()
        {
            if (Con.State != ConnectionState.Open)
                Con.Open();
        }
        /// <summary>
        /// Close the connection, if not closed
        /// </summary>
        protected void Close()
        {
            if (Con.State != ConnectionState.Closed)
                Con.Close();
        }

    }
}
using System;
using System.Data;
using System.Data.SqlClient;

namespace OnlineMonitoring.ServerCore.DataBase
{
    public abstract class ClsCmd : ClsCon
    {
        protected ClsCmd(string connectionString):base(connectionString){}
        /// <summary>
        /// Returns a sql Command with the specified command text, command type and parameters
        /// </summary>
        /// <param name="cmdType"></param>
        /// <param name="parameters">The SqlParameters to be passed to the command.
        /// EvenIndex should be parameterName, OddIndex parameters value
        /// (eg: "@Id", 3, "@name", "John", "@date", DateTime.Now)
        /// </param>
        /// <param name="cmdText"></param>
        private SqlCommand PrepareCommand(string cmdText, CommandType cmdType, object[] parameters)
        {
            var cmd = new SqlCommand(cmdText, Con) { CommandType = cmdType };
            for (var i = 0; i < parameters.Length; i += 2)
                cmd.Parameters.AddWithValue(parameters[i].ToString(), parameters[i + 1] ?? DBNull.Value);

            return cmd;
        }

        /// <summary>
        /// Throws an ArgumentException if parameters length is not even
        /// </summary>
        private static void CheckParamtersLength(object[] parameters)
        {
            if (parameters.Length % 2 != 0)
                throw new ArgumentException("Parameters Length must be even!");
        }

        /// <summary>
        /// Executes a SqlCommand with the specified query text and parameters and returns the numbers of rows affected
        /// </summary>
        /// <param name="query">The text of the query to be passed to the command</param>
        /// <param name="parameters">The parameters to be passed to the command.
        /// EvenIndex should be parameterName, OddIndex parameters value
        /// (eg: "@Id", 3, "@name", "John", "@date", DateTime.Now)
        /// </param>
        /// <returns>The numbers of rows affected</returns>
        protected int Execute(string query, params object[] parameters)
        {
            return Execute(query, CommandType.Text, parameters);
        }
        /// <summary>
        /// Executes the specified stored procedure with the specified parameters and returns the numbers of rows affected
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure</param>
        /// <param name="parameters">The parameters to be passed to the command.
        /// EvenIndex should be parameterName, OddIndex parameters value
        /// (eg: "@Id", 3, "@name", "John", "@date", DateTime.Now)
        /// </param>
        /// <returns>The numbers of rows affected</returns>
        protected int ExecuteSP(string storedProcedureName, params object[] parameters)
        {
            return Execute(storedProcedureName, CommandType.StoredProcedure, parameters);
        }
        /// <summary>
        /// Executes a SqlCommand with the specified query text, command text and parameters and returns the numbers of rows affected
        /// </summary>
        /// <param name="cmdText">The text of the query to be passed to the SqlCommand</param>
        /// <param name="cmdType">The type of the SqlCommand</param>
        /// <param name="parameters">The parameters to be passed to the SqlCommand.
        /// EvenIndex should be parameterName, OddIndex parameters value
        /// (eg: "@Id", 3, "@name", "John", "@date", DateTime.Now)
        /// </param>
        /// <returns>The numbers of rows affected</returns>
        private int Execute(string cmdText, CommandType cmdType, params object[] parameters)
        {
            CheckParamtersLength(parameters);
            Open();
            try
            {
                var cmd = PrepareCommand(cmdText, cmdType, parameters);
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                Close();
            }
        }

        /// <summary>
        /// Executes a SqlCommand with the specified query text and parameters and returns the first column of the first row returned by the query. Additional columns or rows are ignored
        /// </summary>
        /// <param name="query">The text of the query to be passed to the SqlCommand</param>
        /// <param name="parameters">The parameters to be passed to the SqlCommand.
        /// EvenIndex should be parameterName, OddIndex parameters value
        /// (eg: "@Id", 3, "@name", "John", "@date", DateTime.Now)
        /// </param>
        /// <returns>The first column of the first row returned by the query</returns>
        protected object ExecuteScalar(string query, params object[] parameters)
        {
            return ExecuteScalar(query, CommandType.Text, parameters);
        }
        /// <summary>
        /// Executes the specified stored procedure with the specified parameters and returns the first column of the first row returned by the query. Additional columns or rows are ignored
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure</param>
        /// <param name="parameters">The parameters to be passed to the command.
        /// EvenIndex should be parameterName, OddIndex parameters value
        /// (eg: "@Id", 3, "@name", "John", "@date", DateTime.Now)
        /// </param>
        /// <returns>The first column of the first row returned by the query. Additional columns or rows are ignored</returns>
        protected object ExecuteSPScalar(string storedProcedureName, params object[] parameters)
        {
            return ExecuteScalar(storedProcedureName, CommandType.StoredProcedure, parameters);
        }
        /// <summary>
        /// Executes a SqlCommand with the specified query text, command text and parameters and returns the first column of the first row returned by the query. Additional columns or rows are ignored
        /// </summary>
        /// <param name="cmdText">The text of the query to be passed to the SqlCommand</param>
        /// <param name="cmdType">The type of the SqlCommand</param>
        /// <param name="parameters">The parameters to be passed to the SqlCommand.
        /// EvenIndex should be parameterName, OddIndex parameters value
        /// (eg: "@Id", 3, "@name", "John", "@date", DateTime.Now)
        /// </param>
        /// <returns>The first column of the first row returned by the query</returns>
        private object ExecuteScalar(string cmdText, CommandType cmdType, params object[] parameters)
        {
            CheckParamtersLength(parameters);
            Open();
            try
            {
                var cmd = PrepareCommand(cmdText, cmdType, parameters);
                return cmd.ExecuteScalar();
            }
            finally
            {
                Close();
            }
        }

        /// <summary>
        /// Executes a SqlCommand with the specified query text, command text and parameters and returns a System.DataTable with the results of the query
        /// </summary>
        /// <param name="query">The text of the query to be passed to the SqlCommand</param>
        /// <param name="parameters">The parameters to be passed to the SqlCommand.
        /// EvenIndex should be parameterName, OddIndex parameters value
        /// (eg: "@Id", 3, "@name", "John", "@date", DateTime.Now)
        /// </param>
        /// <returns>A System.DataTable with the results of the query</returns>
        /// <remarks>Default Command Type is Text</remarks>
        protected DataTable GetTable(string query, params object[] parameters)
        {
            return GetTable(query, CommandType.Text, parameters);
        }
        /// <summary>
        /// Executes the specified stored procedure with the specified parameters and returns a System.DataTable with the results of the query
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure</param>
        /// <param name="parameters">The parameters to be passed to the command.
        /// EvenIndex should be parameterName, OddIndex parameters value
        /// (eg: "@Id", 3, "@name", "John", "@date", DateTime.Now)
        /// </param>
        /// <returns>A System.DataTable with the results of the query</returns>
        protected DataTable GetSPTable(string storedProcedureName, params object[] parameters)
        {
            return GetTable(storedProcedureName, CommandType.StoredProcedure, parameters);
        }
        /// <summary>
        /// Executes a SqlCommand with the specified query text, command text and parameters and returns a System.DataTable with the results of the query
        /// </summary>
        /// <param name="cmdText">The text of the query to be passed to the SqlCommand</param>
        /// <param name="cmdType">The type of the SqlCommand</param>
        /// <param name="parameters">The parameters to be passed to the SqlCommand.
        /// EvenIndex should be parameterName, OddIndex parameters value
        /// (eg: "@Id", 3, "@name", "John", "@date", DateTime.Now)
        /// </param>
        /// <returns>A System.DataTable with the results of the query</returns>
        private DataTable GetTable(string cmdText, CommandType cmdType, params object[] parameters)
        {
            CheckParamtersLength(parameters);
            Open();
            try
            {
                var cmd = PrepareCommand(cmdText, cmdType, parameters);
                var result = new DataTable();
                using (var reader = cmd.ExecuteReader())
                    result.Load(reader);
                return result;
            }
            finally
            {
                Close();
            }
        }

        /// <summary>
        /// Executes a SqlCommand with the specified query text, command text and parameters and returns a System.DataRow with the first row of the results of the query. null is returned if results is empty
        /// </summary>
        /// <param name="query">The text of the query to be passed to the SqlCommand</param>
        /// <param name="parameters">The parameters to be passed to the SqlCommand.
        /// EvenIndex should be parameterName, OddIndex parameters value
        /// (eg: "@Id", 3, "@name", "John", "@date", DateTime.Now)
        /// </param>
        /// <returns>A System.DataRow with the first row of the results of the query. null is returned if results is empty</returns>
        /// <remarks>Default Command Type is Text</remarks>
        protected DataRow GetRow(string query, params object[] parameters)
        {
            return GetRow(query, CommandType.Text, parameters);
        }
        /// <summary>
        /// Executes the specified stored procedure with the specified parameters and returns a System.DataRow with the first row of the results of the query. null is returned if results is empty
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure</param>
        /// <param name="parameters">The parameters to be passed to the command.
        /// EvenIndex should be parameterName, OddIndex parameters value
        /// (eg: "@Id", 3, "@name", "John", "@date", DateTime.Now)
        /// </param>
        /// <returns>The first row of the results of the query. null is returned if results is empty</returns>
        protected DataRow GetSPRow(string storedProcedureName, params object[] parameters)
        {
            return GetRow(storedProcedureName, CommandType.StoredProcedure, parameters);
        }
        /// <summary>
        /// Executes a SqlCommand with the specified query text, command text and parameters and returns a System.DataRow with the first row of the results of the query. null is returned if results is empty
        /// </summary>
        /// <param name="cmdText">The text of the query to be passed to the SqlCommand</param>
        /// <param name="cmdType">The type of the SqlCommand</param>
        /// <param name="parameters">The parameters to be passed to the SqlCommand.
        /// EvenIndex should be parameterName, OddIndex parameters value
        /// (eg: "@Id", 3, "@name", "John", "@date", DateTime.Now)
        /// </param>
        /// <returns>A System.DataRow with the first row of the results of the query. null is returned if results is empty</returns>
        private DataRow GetRow(string cmdText, CommandType cmdType, params object[] parameters)
        {
            var result = GetTable(cmdText, cmdType, parameters);
            if (result != null && result.Rows.Count > 0)
                return result.Rows[0];
            return null;
        }
    }
}
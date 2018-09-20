using Adjudications.Models;
using MySql.Data.MySqlClient;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using Adjudications.Utilities;

namespace Adjudications
{
    /// <summary>
    /// Saves Adjudication information to the database
    /// Implements IDisposable because it creates members of the following IDispoables Types (MySQLConnection, MySQLComand)
    /// </summary>
    class SaveAdjudications : IDisposable
    {
        //Reference to logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //Declare MySql objects
        private MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["GCIMS"].ToString());
        private MySqlCommand cmd = new MySqlCommand();

        public SaveAdjudications() { }

        /// <summary>
        /// Saves adjudication data to the database
        /// Tuple is used to return the multiple values from the DB back to the application
        /// Item 1 = Person ID
        /// Item 2 = Adjudication Status
        /// Item 3 = E-Mail Requested (Send E-Mail)
        /// Item 4 = Pers Status
        /// </summary>
        /// <param name="saveData"></param>
        /// <param name="ssn"></param>
        /// <returns></returns>
        public Tuple<int, string, bool, string> SaveAdjudication(AdjudicationData saveData, bool isDebug) //, byte[] ssn
        {
            try
            {
                using (conn)
                {
                    //open connection if not open
                    if (conn.State == ConnectionState.Closed)
                        conn.Open();

                    using (cmd)
                    {
                        cmd.Connection = conn;
                        //Call stored procedure
                        cmd.CommandType = CommandType.StoredProcedure;

                        //Specify stored procedure name
                        cmd.CommandText = "Adjudications";

                        //Clear sql parameters
                        cmd.Parameters.Clear();

                        //Date variables
                        string investDate = string.Empty;
                        string portOfEntryDate = string.Empty;

                        //Get dates from save data if not null
                        investDate = saveData.InvestigationDate.HasValue ? saveData.InvestigationDate.Value.ToString("yyyy-MM-dd") : string.Empty;
                        portOfEntryDate = saveData.DateOfEntry.HasValue ? saveData.DateOfEntry.Value.ToString("yyyy-MM-dd") : string.Empty;

                        //Set  new sql parameters
                        MySqlParameter[] adjudicationParameters = new MySqlParameter[]
                        {
                            new MySqlParameter { ParameterName = "lastname", Value = saveData.LastName, MySqlDbType = MySqlDbType.VarChar, Size = 60, Direction = ParameterDirection.Input },
                            new MySqlParameter { ParameterName = "ssn", Value = saveData.HashedSSN, MySqlDbType = MySqlDbType.VarBinary, Size = 32, Direction = ParameterDirection.Input },
                            new MySqlParameter { ParameterName = "investigationType", Value = saveData.Investigation.InvestigationType, MySqlDbType = MySqlDbType.VarChar, Size = 20, Direction = ParameterDirection.Input },
                            new MySqlParameter { ParameterName = "investigationDate", Value = investDate, MySqlDbType = MySqlDbType.Date, Direction = ParameterDirection.Input },
                            new MySqlParameter { ParameterName = "typeAccess", Value = saveData.Investigation.TypeAccess, MySqlDbType = MySqlDbType.VarChar, Size = 12, Direction = ParameterDirection.Input },
                            new MySqlParameter { ParameterName = "isDebug", Value = isDebug, DbType = DbType.Boolean, Direction = ParameterDirection.Input },
                            new MySqlParameter { ParameterName = "isNAC", Value = saveData.Investigation.isNAC, DbType = DbType.Boolean, Direction = ParameterDirection.Input },
                            new MySqlParameter { ParameterName = "isNACI", Value = saveData.Investigation.isNACI, DbType = DbType.Boolean, Direction = ParameterDirection.Input },
                            new MySqlParameter { ParameterName = "isFavorable", Value = saveData.Investigation.isFavorable, DbType = DbType.Boolean, Direction = ParameterDirection.Input },
                            new MySqlParameter { ParameterName = "portOfEntryDate", Value = portOfEntryDate, MySqlDbType = MySqlDbType.VarChar, Size = 10, Direction = ParameterDirection.Input },
                            new MySqlParameter { ParameterName = "updatePortOfEntryDate", Value = saveData.UpdatePortOfEntryDate, DbType = DbType.Boolean, Direction = ParameterDirection.Input },
                            new MySqlParameter { ParameterName = "adjudicationStatus", MySqlDbType = MySqlDbType.VarChar, Size = 500, Direction = ParameterDirection.Output },
                            new MySqlParameter { ParameterName = "id", MySqlDbType = MySqlDbType.Int32, Size = 20, Direction = ParameterDirection.Output },
                            new MySqlParameter { ParameterName = "sendEMail", DbType = DbType.Boolean, Direction = ParameterDirection.Output },
                            new MySqlParameter { ParameterName = "persStatus", MySqlDbType = MySqlDbType.VarChar, Size=12, Direction = ParameterDirection.Output }
                        };

                        //Add parameters to cmd
                        cmd.Parameters.AddRange(adjudicationParameters);

                        //Execute cmd
                        cmd.ExecuteNonQuery();

                        //Return tuple of id,adjudication status, sendEmail, and pers status
                        return new Tuple<int, string, bool, string>(
                            (int)cmd.Parameters["id"].Value, 
                            cmd.Parameters["adjudicationStatus"].GetStringValueOrEmpty(), 
                            Convert.ToBoolean(cmd.Parameters["sendEMail"].Value), 
                            cmd.Parameters["persStatus"].GetStringValueOrEmpty());
                    }
                }
            }
            //Catch all exceptions, log error, and return default values of 0,error,false
            catch (Exception ex)
            {
                log.Error("Save: " + ex.Message + " - " + ex.InnerException);
                return new Tuple<int, string, bool, string>(0, "ERROR", false, string.Empty);
            }
        }

        /// <summary>
        /// Disposes of object
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                cmd.Dispose();
                conn.Dispose();
            }
            // free native resources
        }

        /// <summary>
        /// Disposes of object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
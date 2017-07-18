using Adjudications.Models;
using MySql.Data.MySqlClient;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;

namespace Adjudications
{
    /// <summary>
    /// Saves Adjudication information to the database
    /// Implements IDisposable becasue it creates members of the following IDispoables Types (MySQLConnection, MySQLComand)
    /// </summary>
    class SaveAdjudications : IDisposable
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["GCIMS"].ToString());
        private MySqlCommand cmd = new MySqlCommand();

        public SaveAdjudications() { }

        /// <summary>
        /// Saves adjudiction data to the database
        /// 
        /// Tuple is used to return the multiple values from the DB back to the application
        /// 
        /// Item 1 = Person ID
        /// Item 2 = Adjudication Status
        /// Item 3 = E-Mail Requested (Send E-Mail)
        /// </summary>
        /// <param name="saveData"></param>
        /// <param name="ssn"></param>
        /// <returns></returns>
        public Tuple<int, string, bool> SaveAdjudication(AdjudicationData saveData, bool isDebug) //, byte[] ssn
        {
            //log.Error(saveData.FirstName + ' ' + saveData.LastName);            
            
            try
            {
                using (conn)
                {
                    if (conn.State == ConnectionState.Closed)
                        conn.Open();

                    using (cmd)
                    {
                        cmd.Connection = conn;
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.CommandText = "Adjudications"; //AdjudicationsTest

                        cmd.Parameters.Clear();

                        string investDate = string.Empty;
                        string portOfEntryDate = string.Empty;

                        investDate = saveData.InvestigationDate.HasValue ? saveData.InvestigationDate.Value.ToString("yyyy-MM-dd") : string.Empty;

                        portOfEntryDate = saveData.DateOfEntry.HasValue ? saveData.DateOfEntry.Value.ToString("yyyy-MM-dd") : string.Empty;
                        
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
                            new MySqlParameter { ParameterName = "sendEMail", DbType = DbType.Boolean, Direction = ParameterDirection.Output }
                        };

                        cmd.Parameters.AddRange(adjudicationParameters);

                        cmd.ExecuteNonQuery();

                        //foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(saveData))
                        //{
                        //    string name = descriptor.Name;
                        //    object value = descriptor.GetValue(saveData);
                        //    log.Info(name + ": " + value);
                        //}
                        
                        return new Tuple<int, string, bool>((int)cmd.Parameters["id"].Value, (string)cmd.Parameters["adjudicationStatus"].Value, Convert.ToBoolean(cmd.Parameters["sendEMail"].Value));
                    }
                }
            }
            catch (Exception ex)
            {
                //foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(saveData))
                //{
                //    string name = descriptor.Name;
                //    object value = descriptor.GetValue(saveData);
                //    log.Info(name + ": " + value);
                //}
                log.Error("Save: " + ex.Message + " - " + ex.InnerException);
                return new Tuple<int, string, bool>(0, "ERROR", false);
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
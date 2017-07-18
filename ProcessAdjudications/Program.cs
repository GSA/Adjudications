using Gsa.Sftp.Libraries.Utilities.Encryption;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Adjudications
{    
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static Stopwatch stopWatch = new Stopwatch();

        //Need better naming namespace and convention here
        private static Utilities.Utilities u = new Utilities.Utilities();

        static void Main(string[] args)
        {
            stopWatch.Start();
            
            log.Info("Application Started");

            //Process Debug files
            #region "Debug Files"
                log.Info("Processing Debug Files");

                var debugFiles = Directory.EnumerateFiles(ConfigurationManager.AppSettings["ADJUDICATIONDEBUGFILELOCATION"], "*.csv", SearchOption.TopDirectoryOnly);

                ProcessDebugFiles(debugFiles, ConfigurationManager.AppSettings["ADJUDICATIONDEBUGFILELOCATION"], true);
            #endregion

            //Process Prod files
            #region "Prod Files"
                log.Info("Processing Production Files");

                var prodFiles = Directory.EnumerateFiles(ConfigurationManager.AppSettings["ADJUDICATIONPRODUCTIONFILELOCATION"], "*.csv", SearchOption.TopDirectoryOnly);

                ProcessProdFiles(prodFiles, ConfigurationManager.AppSettings["ADJUDICATIONPRODUCTIONFILELOCATION"], false);
            #endregion

            log.Info(string.Format("Processed Adjudications in {0} milliseconds", stopWatch.ElapsedMilliseconds));

            stopWatch.Stop();

            log.Info("Application Done");

            Console.WriteLine("Done! " + stopWatch.ElapsedMilliseconds);
            
            return;
        }

        private static void SendSummary(string fileName, Tuple<int,string> summaryInformation, bool isDebug)
        {
            string emailBody = string.Empty;
            string emailSubject = string.Empty;

            emailBody = u.GenerateEMailBody(fileName,summaryInformation.Item1, summaryInformation.Item2);

            if (isDebug)
                emailSubject = "DEBUG: ";

            log.Info("E-Mailing Summary File");

            u.SendEMail(emailBody, emailSubject);
        }

        private static void ProcessDebugFiles(IEnumerable<string> adjudicationFiles, string fileLocation, bool isDebug)
        {
            ProcessAdjudications pa = new Adjudications.ProcessAdjudications(isDebug);

            foreach (string debugFile in adjudicationFiles)
            {               
                //1 = Total Processed
                //2 = Errors
                Tuple<int, string> processedInformation = new Tuple<int, string>(0, "");                               

                processedInformation = pa.ProcessAdjudicationFile(debugFile);

                try
                {
                    File.Delete(debugFile);                    
                }
                catch (IOException e)
                {
                    log.Error(e.Message);
                }

                SendSummary(debugFile, processedInformation, isDebug);

                if (string.IsNullOrEmpty(processedInformation.Item2))
                    BackupSummaryFile(true);
            }
        }

        private static void ProcessProdFiles(IEnumerable<string> adjudicationFiles, string fileLocation, bool isDebug)
        {
            ProcessAdjudications pa = new Adjudications.ProcessAdjudications(isDebug);

            foreach (string encryptedFile in adjudicationFiles)
            {
                byte[] buffer = new byte[] { };

                Tuple<int, string> processedInformation = new Tuple<int, string>(0, "");

                string decryptedFile = string.Empty;

                decryptedFile = fileLocation + u.GenerateDecryptedFilename(Path.GetFileNameWithoutExtension(encryptedFile));

                buffer = File.ReadAllBytes(encryptedFile);

                buffer.WriteToFile(decryptedFile, Cryptography.Security.Decrypt, true);

                processedInformation = pa.ProcessAdjudicationFile(decryptedFile);

                try
                {
                    File.Delete(encryptedFile);
                    File.Delete(decryptedFile);
                }
                catch (IOException e)
                {
                    log.Error(e.Message);
                }

                SendSummary(decryptedFile, processedInformation, isDebug);

                if (string.IsNullOrEmpty(processedInformation.Item2))
                    BackupSummaryFile(false);
            }
        }

        private static void BackupSummaryFile(bool isDebug)
        {
            StringBuilder destinationFile = new StringBuilder();

            string sourceFile = ConfigurationManager.AppSettings["ADJUDICATIONSSUMMARY"];
            //string destinationFile = ConfigurationManager.AppSettings["SUMMARYBACKUPLOCATION"] + Path.GetFileNameWithoutExtension(sourceFile) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";

            destinationFile.Append(ConfigurationManager.AppSettings["SUMMARYBACKUPLOCATION"]);
            destinationFile.Append(Path.GetFileNameWithoutExtension(sourceFile));
            destinationFile.Append("_");
            destinationFile.Append(DateTime.Now.ToString("yyyyMMddHHmmss_FFFF"));

            if (isDebug)
                destinationFile.Append("-D");

            destinationFile.Append(".csv");

            log.Info("Backing Up Summary File: " + destinationFile);

            File.Move(sourceFile, destinationFile.ToString());            
            File.Delete(sourceFile);
        }
    }
}
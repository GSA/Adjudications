using Gsa.Sftp.Libraries.Utilities.Encryption;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Adjudications
{
    /// <summary>
    /// Process Adjudication files located in folder path from .config
    /// </summary>
    class Program
    {
        //Reference to logger and stopwatch
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static Stopwatch stopWatch = new Stopwatch();

        //Need better naming namespace and convention here
        private static Utilities.Utilities u = new Utilities.Utilities();

        /// <summary>
        /// Start timer and begin processing of debug and production files, then stop timer and output results
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //Start stopwatch
            stopWatch.Start();

            //Log start of application
            log.Info("Application Started");

            //Process Debug files
            #region "Debug Files"
                //Log start of debug file section
                log.Info("Processing Debug Files");

                //Get list of debug files
                var debugFiles = Directory.EnumerateFiles(ConfigurationManager.AppSettings["ADJUDICATIONDEBUGFILELOCATION"], "*.csv", SearchOption.TopDirectoryOnly);

                //Begin processing list of debug files
                ProcessDebugFiles(debugFiles, ConfigurationManager.AppSettings["ADJUDICATIONDEBUGFILELOCATION"], true);
            #endregion

            //Process Prod files
            #region "Prod Files"
                //Log start of production file section
                log.Info("Processing Production Files");

                //Get list of production files
                var prodFiles = Directory.EnumerateFiles(ConfigurationManager.AppSettings["ADJUDICATIONPRODUCTIONFILELOCATION"], "*.csv", SearchOption.TopDirectoryOnly);

                //Begin processing list of production files
                ProcessProdFiles(prodFiles, ConfigurationManager.AppSettings["ADJUDICATIONPRODUCTIONFILELOCATION"], false);
            #endregion

            //Log elapsed time
            log.Info(string.Format("Processed Adjudications in {0} milliseconds", stopWatch.ElapsedMilliseconds));

            //Stop stopwatch
            stopWatch.Stop();

            //Log end of application
            log.Info("Application Done");

            //Output completion and elapsed time to console
            Console.WriteLine("Done! " + stopWatch.ElapsedMilliseconds);

            return;
        }

        /// <summary>
        /// Generates and sends email summary
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="summaryInformation"></param>
        /// <param name="isDebug"></param>
        private static void SendSummary(string fileName, Tuple<int,string> summaryInformation, bool isDebug)
        {
            //Variables for email body and subject
            string emailBody = string.Empty;
            string emailSubject = string.Empty;

            //Generate the email body
            emailBody = u.GenerateEMailBody(fileName,summaryInformation.Item1, summaryInformation.Item2);

            //Set subject to debug if true
            if (isDebug)
                emailSubject = "DEBUG: ";

            //Log email summary file
            log.Info("E-Mailing Summary File");

            //send email
            u.SendEMail(emailBody, emailSubject);
        }

        /// <summary>
        /// Processes all non-encrypted debug files
        /// </summary>
        /// <param name="adjudicationFiles"></param>
        /// <param name="fileLocation"></param>
        /// <param name="isDebug"></param>
        private static void ProcessDebugFiles(IEnumerable<string> adjudicationFiles, string fileLocation, bool isDebug)
        {
            //Create ProcessAdjudications object used for debug files
            ProcessAdjudications pa = new Adjudications.ProcessAdjudications(isDebug);

            //Iterate through debug files
            foreach (string debugFile in adjudicationFiles)
            {
                //Declare tuple to store processing progress
                //1 = Total Processed
                //2 = Errors
                Tuple<int, string> processedInformation = new Tuple<int, string>(0, "");

                //Call ProcessAdjudicationFile using ProcessAdjudications object previously declared
                processedInformation = pa.ProcessAdjudicationFile(debugFile);

                //Attempt to delete original file and catch any IOException errors
                //Write any errors to log
                try
                {
                    File.Delete(debugFile);
                }
                catch (IOException e)
                {
                    log.Error(e.Message);
                }

                //Send an email summary
                SendSummary(debugFile, processedInformation, isDebug);

                //If Item2 null or empty, call BackupSummaryFile
                if (string.IsNullOrEmpty(processedInformation.Item2))
                    BackupSummaryFile(true);
            }
        }

        /// <summary>
        /// Process all encrypted production files
        /// </summary>
        /// <param name="adjudicationFiles"></param>
        /// <param name="fileLocation"></param>
        /// <param name="isDebug"></param>
        private static void ProcessProdFiles(IEnumerable<string> adjudicationFiles, string fileLocation, bool isDebug)
        {
            //Create ProcessAdjudications object to process production files
            ProcessAdjudications pa = new Adjudications.ProcessAdjudications(isDebug);

            //Iterate through encrypted files
            foreach (string encryptedFile in adjudicationFiles)
            {
                //Decrypt file
                byte[] buffer = new byte[] { };
                Tuple<int, string> processedInformation = new Tuple<int, string>(0, "");
                string decryptedFile = string.Empty;
                decryptedFile = fileLocation + u.GenerateDecryptedFilename(Path.GetFileNameWithoutExtension(encryptedFile));
                buffer = File.ReadAllBytes(encryptedFile);
                buffer.WriteToFile(decryptedFile, Cryptography.Security.Decrypt, true);

                //Process decrypted file
                processedInformation = pa.ProcessAdjudicationFile(decryptedFile);

                //Attempt to delete decrypted and original file
                //Catch all IOException errors and write them to log
                try
                {
                    File.Delete(encryptedFile);
                    File.Delete(decryptedFile);
                }
                catch (IOException e)
                {
                    log.Error(e.Message);
                }

                //Send summary email
                SendSummary(decryptedFile, processedInformation, isDebug);

                //If Item2 is null or empty, call BackupSummaryFile
                if (string.IsNullOrEmpty(processedInformation.Item2))
                    BackupSummaryFile(false);
            }
        }

        /// <summary>
        /// Move source file to destination file location and provide new name for file
        /// </summary>
        /// <param name="isDebug"></param>
        private static void BackupSummaryFile(bool isDebug)
        {
            //Create string builder object
            StringBuilder destinationFile = new StringBuilder();

            //Path to source file
            string sourceFile = ConfigurationManager.AppSettings["ADJUDICATIONSSUMMARY"];

            //Should remove commented code from the project
            //string destinationFile = ConfigurationManager.AppSettings["SUMMARYBACKUPLOCATION"] + Path.GetFileNameWithoutExtension(sourceFile) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";

            //Create destination file name in string builder object
            destinationFile.Append(ConfigurationManager.AppSettings["SUMMARYBACKUPLOCATION"]);
            destinationFile.Append(Path.GetFileNameWithoutExtension(sourceFile));
            destinationFile.Append("_");
            destinationFile.Append(DateTime.Now.ToString("yyyyMMddHHmmss_FFFF"));

            //If true append -D
            if (isDebug)
                destinationFile.Append("-D");

            //Add .csv to end
            destinationFile.Append(".csv");

            //log back up action
            log.Info("Backing Up Summary File: " + destinationFile);

            //Move files and delete original
            //Should probably be in a try/catch. Could throw IOException
            File.Move(sourceFile, destinationFile.ToString());
            File.Delete(sourceFile);
        }
    }
}
using Adjudications.Mapping;
using Adjudications.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;

namespace Adjudications
{
    /// <summary>
    ///
    /// </summary>
    public class ProcessAdjudications
    {
        //Class variables
        private static CsvHelper.Configuration.Configuration config;
        private static int defaultColumnCount = 38;
        private static List<Investigation> investigations;
        private static bool isDebug;
        private static List<AdjudicationData> processed;
        private static Utilities.Utilities u = new Utilities.Utilities();

        //Reference to logger
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Initializes the processing of adjudication files
        /// </summary>
        public ProcessAdjudications(bool debugMode)
        {
            //Define function variables
            // CsvConfiguration changed to CsvHelper.Configuration.Configuration for v. 6.0.0
            config = new CsvHelper.Configuration.Configuration();
            investigations = new List<Investigation>();
            processed = new List<AdjudicationData>();

            int investColumnCount = 0;

            //CSV settings
            config.Delimiter = ",";
            config.HasHeaderRecord = true;

            config.MissingFieldFound = null; // (headerNames, index, context) =>
            //{
            //    log.Error($"Field with name ['{string.Join("', '", headerNames)}'] at index '{index}' was not found.");
            //};

            // Ignores header case
            config.PrepareHeaderForMatch = header => header?.ToLower();
            config.TrimOptions = TrimOptions.None;

            //Sets Debug Mode
            isDebug = debugMode; //bool.Parse(ConfigurationManager.AppSettings["DEBUGMODE"]);

            //Loads the investigation data (lookup)
            investigations = GetFileData<Investigation, InvestigationMapping>(AppDomain.CurrentDomain.BaseDirectory + "Lookups\\Investigations.csv", config, out investColumnCount);

            //If not int, log error and throw invalid cast exception
            if (!int.TryParse(ConfigurationManager.AppSettings["COLUMNCOUNT"].ToString(), out defaultColumnCount))
            {
                log.Error("Unable to convert column count size to integer");
                throw new InvalidCastException("Unable to convert column count size to integer");
            }
        }

        ///TODO: Should probably hash all the SSN early on (do this when you filter)
        /// <summary>
        /// Reads the adjudication file information and starts processing the information
        /// </summary>
        /// <param name="adjudicationFile">File Name</param>
        ///Add IsNullOrEmpty to the queries for all the other columns that are not associated with that method.
        public Tuple<int, string> ProcessAdjudicationFile(string adjudicationFile)
        {
            try
            {
                //Log start of function
                log.Info("Processing Adjudication File: " + adjudicationFile);

                //Define function variables
                int fileColumnCount = 0;
                string errors = string.Empty;
                int totalProcessed = 0;

                List<Adjudication> adjudicationList = new List<Adjudication>();

                //Gets all the data
                adjudicationList = GetFileData<Adjudication, AdjudicationMapping>(adjudicationFile, config, out fileColumnCount);

                //Store total
                totalProcessed = adjudicationList.Count;

                //Check the entire file for 3 year issues and then remove them before proceeding.
                //checks to see if we can process the file
                if (isProccessable(fileColumnCount, adjudicationList, out errors))
                {
                    log.Info("Adjudication Count: " + adjudicationList.Count);

                    //Process Case Discontinued
                    ProcessCaseDiscontinued(adjudicationList);

                    //Process Unfavorable SACs
                    ProcessUnfavorableSAC(adjudicationList);

                    //Process Unfavorable Finals
                    ProcessUnfavorableFinal(adjudicationList);

                    //Process Rescinded
                    ProcessRescinded(adjudicationList);

                    //Process Waiting For Final
                    ProcessWaitingForFinal(adjudicationList);

                    //Process SACs
                    ProcessSAC(adjudicationList);

                    //Process Finals
                    ProcessFinal(adjudicationList);

                    //Process Fingerprints
                    ProcessFingerprint(adjudicationList);

                    GenerateSummaryFile(adjudicationList, adjudicationFile);
                    //SendSummary(adjudicationList, adjudicationFile);
                }

                //Log completion of function
                log.Info("Done Processing Adjudication File: " + adjudicationFile);

                return new Tuple<int, string>(totalProcessed, errors);
            }
            //Catch all errors, log exception and return 0,""
            //Then clear processed
            catch (Exception ex)
            {
                log.Error(ex.Message + " - " + ex.InnerException);
                return new Tuple<int, string>(0, "");
            }
            finally
            {
                processed.Clear();
            }
        }

        /// <summary>
        /// Remove Found records from main list after processing.  Processing will capture any errors.
        /// </summary>
        /// <param name="adjudications"></param>
        /// <param name="adjudicationData"></param>
        private static void RemoveRecords(List<Adjudication> adjudications, List<AdjudicationData> adjudicationData)
        {
            //Remove all with matching SSN and Last Name
            adjudications.RemoveAll(r => adjudicationData.Any(a => a.SSN == r.SSN && a.LastName == r.LastName));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="indeterminable"></param>
        /// <param name="fileName"></param>
        private void GenerateSummaryFile(List<Adjudication> indeterminable, string fileName)
        {
            List<AdjudicationData> summary = new List<AdjudicationData>();

            //Variable to hold indeterminable
            var theRest = indeterminable
                            .Select
                                (
                                    s =>
                                        new AdjudicationData
                                        {
                                            FirstName = s.FirstName,
                                            MiddleName = s.MiddleName,
                                            LastName = s.LastName,
                                            Suffix = s.Suffix,
                                            InvestigationType = "NONE",
                                            InvestigationDate = null,
                                            AdjudicationStatus = "Indeterminable",
                                            EMailRequested = false,
                                            ID = 0
                                        }
                                )
                            .ToList();

            //Holds all processed
            summary = processed
                    .Select
                        (
                            s =>
                                new AdjudicationData
                                {
                                    FirstName = s.FirstName,
                                    MiddleName = s.MiddleName,
                                    LastName = s.LastName,
                                    Suffix = s.Suffix,
                                    InvestigationType = s.InvestigationType,
                                    InvestigationDate = s.InvestigationDate,
                                    AdjudicationStatus = s.AdjudicationStatus,
                                    EMailRequested = s.EMailRequested,
                                    ID = s.ID
                                }
                        )
                    .OrderBy(o => o.InvestigationType)
                    .ThenBy(t => t.LastName)
                    .ToList();

            //Combines the indeterminables and the processed into one list
            summary.AddRange(theRest);

            //Log count
            log.Info("Summary Count: " + summary.Count);

            //Attempt to write csv file, catch all exceptions and log the errors
            try
            {
                //Creates the summary file
                using (CsvWriter csvWriter = new CsvWriter(new StreamWriter(ConfigurationManager.AppSettings["ADJUDICATIONSSUMMARY"], false)))
                {
                    csvWriter.Configuration.RegisterClassMap(new SummaryMapping());
                    csvWriter.WriteRecords(summary);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message + " - " + ex.InnerException);
            }
        }

        /// <summary>
        /// Loads the adjudication information
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <typeparam name="TMap"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="config"></param>
        /// <param name="columnCount"></param>
        /// <returns></returns>
        private List<TClass> GetFileData<TClass, TMap>(string filePath, CsvHelper.Configuration.Configuration config, out int columnCount)
            where TClass : class
            where TMap : ClassMap<TClass>
        {
            //Import csv into a POCO
            // Updated this section due to breaking changes w/ CsvHelper v. 6.0.0
            // NullReference error thown due to multiple Dispose() calls
            var reader = new StreamReader(filePath);
            
            using (CsvReader csvReader = new CsvReader(reader, config, false))
            {
                csvReader.Configuration.RegisterClassMap<TMap>();

                List<TClass> allRecords = csvReader.GetRecords<TClass>().ToList();

                // Uupdated due to changes in CsvHelper v. 6.0.0
                columnCount = csvReader.Context.HeaderRecord.Count();

                return allRecords;
            }
        }

        /// <summary>
        /// Checks whether or not the file can be processed
        /// </summary>
        /// <param name="fileColumnCount"></param>
        /// <param name="allAdjudications"></param>
        /// <returns></returns>
        private bool isProccessable(int fileColumnCount, List<Adjudication> allAdjudications, out string error)
        {
            error = "";

            //check column counts
            if (fileColumnCount != defaultColumnCount)
            {
                log.Error("Column counts do not match!");

                error = "Column counts do not match";

                return false;
            }

            //Determines if there are duplicates
            var duplicates = allAdjudications
                                .GroupBy
                                    (
                                        g =>
                                            new
                                            {
                                                LastName = g.LastName,
                                                SSN = g.SSN
                                            }
                                    )
                                .Any(a => a.Count() > 1);

            //If duplicates, log error, set OUT
            if (duplicates)
            {
                log.Error("Duplicate Last Name + SSN Match Found");

                error = "Duplicate Last Name + SSN Match Found";

                return false;
            }

            return true;
        }

        /// <summary>
        /// Processes each individual adjudication and saves to the DB
        /// </summary>
        /// <param name="adjudications"></param>
        private void ProcessAdjudicationList(List<AdjudicationData> adjudications)
        {
            SaveAdjudications save = new SaveAdjudications();
            AdjudicationEMails adjudicationEMails = new AdjudicationEMails();

            //Item 1 = ID, Item 2 = Status, Item 3 = Send E-Mail
            Tuple<int, string, bool> result = new Tuple<int, string, bool>(0, string.Empty, false);

            //log.Info("Start: Looping Adjudications List");

            //Iterate through adjudicationData
            foreach (AdjudicationData adjudicationData in adjudications)
            {
                //Might be able to do this the same way we hash ssn
                //Get Investigation Information
                adjudicationData.Investigation = investigations.SingleOrDefault(w => w.InvestigationType.ToLower() == adjudicationData.InvestigationType.ToLower());

                //Missing Investigation Data
                if (adjudicationData.Investigation == null)
                {
                    adjudicationData.AdjudicationStatus = ProcessedStatusConstants.INDETERMINABLE;
                    processed.Add(adjudicationData);
                    continue;
                }

                //If unfavorable
                if (adjudicationData.InvestigationType != "Unfavorable")
                {
                    adjudicationData.UpdatePortOfEntryDate = false;

                    //3 Year Port of Entry Mismatch
                    if ((adjudicationData.LessThan3YearsUS.ToLower().Equals("(checked)") && adjudicationData.DateOfEntry == null) ||
                       (adjudicationData.LessThan3YearsUS.ToLower().Equals("(checked)") && CheckedGreaterThan3Years(adjudicationData.DateOfEntry, adjudicationData.InvestigationDate)) ||
                       (adjudicationData.LessThan3YearsUS.ToLower().Equals("(not checked)") && adjudicationData.DateOfEntry != null) ||
                       (adjudicationData.LessThan3YearsUS == null || adjudicationData.LessThan3YearsUS == "" || adjudicationData.LessThan3YearsUS.Length == 0))
                    {
                        adjudicationData.AdjudicationStatus = ProcessedStatusConstants.PORT_OF_ENTRY;
                        processed.Add(adjudicationData);
                        continue;
                    }

                    //if not sac and LessThan3YearsUS checked and CheckedLessThan3Years
                    if (adjudicationData.InvestigationType != "SAC" &&
                        adjudicationData.LessThan3YearsUS.ToLower().Equals("(checked)") && CheckedLessThan3Years(adjudicationData.DateOfEntry, adjudicationData.InvestigationDate))
                    {
                        adjudicationData.AdjudicationStatus = ProcessedStatusConstants.PORT_OF_ENTRY;
                        processed.Add(adjudicationData);
                        continue;
                    }

                    //Update Port Of Entry Date
                    if ((adjudicationData.LessThan3YearsUS.ToString().ToLower().Equals("(checked)") && adjudicationData.DateOfEntry != null) ||
                        (adjudicationData.LessThan3YearsUS.ToString().ToLower().Equals("(not checked)") && adjudicationData.DateOfEntry == null))
                    {
                        adjudicationData.UpdatePortOfEntryDate = true;
                    }
                }

                //Store result
                result = save.SaveAdjudication(adjudicationData, isDebug);

                //Separate tuple
                adjudicationData.ID = result.Item1;
                adjudicationData.AdjudicationStatus =result.Item2;
                adjudicationData.EMailRequested = result.Item3;

                //Update processed
                processed.Add(adjudicationData);

                //if email requested & not debug & not duplicate
                if (adjudicationData.EMailRequested && !isDebug && adjudicationData.AdjudicationStatus != "Duplicate")
                {
                    //need a try catch here
                    //Send adjudication notice
                    adjudicationEMails.SendNotice(adjudicationData.ID);
                    //SMTP server blocks emails if sent out too fast
                    //sleep is used to prevent emails from being flagged as spam by smtp server
                    Thread.Sleep(2500);
                }
            }

            //Clean up
            save.Dispose();
        }

        #region "Filter Adjudication Data"
        /*
        Here is a function that can replace CheckedGreaterThan3Years and CheckedLessThan3Years
        Called like this
        bool greaterThan = CompareThese(DateTimeOne,DateTimeTwo,(x, y) => x >= y);
 		bool lessThan = CompareThese(DateTimeOne,DateTimeTwo,(x, y) => x <= y);

        private static bool CompareTo3Years(DateTime? portOfEntryDate, DateTime? investigationDate, Func<double, int, bool> op)
        {
            DateTime poeDate, investDate;
            if (DateTime.TryParse(portOfEntryDate.ToString(), out poeDate) & DateTime.TryParse(investigationDate.ToString(), out investDate))
                return op(((investDate - poeDate).TotalDays / 365.2425), 3);
            else return false;
        }
        */


        /// <summary>
        /// Checks if investigation date minus poe date is greater than 3 years
        /// </summary>
        /// <param name="portOfEntryDate"></param>
        /// <param name="investigationDate"></param>
        /// <returns></returns>
        private bool CheckedGreaterThan3Years(DateTime? portOfEntryDate, DateTime? investigationDate)
        {

            //if null return false
            if (portOfEntryDate == null)
                return false;

            DateTime poeDate = Convert.ToDateTime(portOfEntryDate);
            DateTime investDate = (DateTime)investigationDate;

            //This should also handle leap years
            if (((investDate - poeDate).TotalDays / 365.2425) >= 3)
            {
                return true;
            }

            //else
            return false;
        }

        /// <summary>
        /// Checks if investigation date minus poe date is less than 3 years
        /// </summary>
        /// <param name="portOfEntryDate"></param>
        /// <param name="investigationDate"></param>
        /// <returns></returns>
        private bool CheckedLessThan3Years(DateTime? portOfEntryDate, DateTime? investigationDate)
        {
            //If null return false
            if (portOfEntryDate == null)
                return false;

            DateTime poeDate = Convert.ToDateTime(portOfEntryDate);
            DateTime investDate = (DateTime)investigationDate;

            //This should also handle leap years
            if (((investDate - poeDate).TotalDays / 365.2425) <= 3)
            {
                return true;
            }

            //else
            return false;
        }

        /// <summary>
        /// Determines the final investigation type
        /// </summary>
        /// <param name="investigationType1"></param>
        /// <param name="investigationType2"></param>
        /// <param name="investigationType3"></param>
        /// <returns></returns>
        private string DetermineFinalInvestigationType(string investigationType1, string investigationType2, string investigationType3)
        {
            string investigationType = string.Empty;

            if (!investigationType3.Equals("(none)"))
            {
                investigationType = investigationType3;
            }
            else if (!investigationType2.Equals("(none)"))
            {
                investigationType = investigationType2;
            }
            else if (!investigationType1.Equals("(none)"))
            {
                investigationType = investigationType1;
            }
            else
            {
                return "(none)";
            }

            switch (investigationType)
            {
                case "tier 1":
                    investigationType = "Tier 1";
                    break;
                case "tier 2":
                case "tier 2s":
                    investigationType = "Tier 2S";
                    break;
                case "tier 4":
                    investigationType = "Tier 4";
                    break;
                case "ssbi-ppr":
                    investigationType = "PPR";
                    break;
                default:
                    break;
            }

            return investigationType;
        }

        /// <summary>
        /// Returns date of sac adjudication or final adjudication if unfavorable. otherwise returns null
        /// </summary>
        /// <param name="SACAdjudicationDate"></param>
        /// <param name="FinalAdjudicationDate"></param>
        /// <param name="SACAdjudicationGSA"></param>
        /// <param name="FinalAdjudicativeAction"></param>
        /// <returns></returns>
        private DateTime? DetermineUnfavorableDate(DateTime? SACAdjudicationDate, DateTime? FinalAdjudicationDate, string SACAdjudicationGSA, string FinalAdjudicativeAction)
        {
            if (SACAdjudicationGSA.ToLower().Equals("unfavorable"))
                return SACAdjudicationDate;
            else if (FinalAdjudicativeAction.ToLower().Equals("unfavorable"))
                return FinalAdjudicationDate;
            else
                return null;
        }

        /// <summary>
        /// Determines the final investigation date
        /// </summary>
        /// <param name="investigationDate1"></param>
        /// <param name="investigationDate2"></param>
        /// <param name="investigationDate3"></param>
        /// <returns></returns>
        private DateTime? DetermineInvestigationDate(DateTime? investigationDate1, DateTime? investigationDate2, DateTime? investigationDate3)
        {
            List<DateTime?> dateList = new List<DateTime?>();

            dateList.Add(investigationDate1);
            dateList.Add(investigationDate2);
            dateList.Add(investigationDate3);

            return dateList.Min();
        }

        /// <summary>
        /// Gets a list of case discontinued.  No need to process these
        /// </summary>
        /// <param name="adjudications"></param>
        /// <returns></returns>
        private void ProcessCaseDiscontinued(List<Adjudication> adjudications)
        {
            //Log start of function
            log.Info("Processing Case Discontinued");

            //get discontinued
            var caseDiscontinued = adjudications
                                    .Where
                                        (
                                            w =>
                                                w.InterimAdjudication1.ToLower().Equals("(none)") &&
                                                w.InterimAdjudication2.ToLower().Equals("(none)") &&
                                                w.InterimAdjudication3.ToLower().Equals("(none)") &&
                                                w.InterimActionTakenIfNotClearedToEOD1.ToLower().Equals("(none)") &&
                                                w.InterimActionTakenIfNotClearedToEOD2.ToLower().Equals("(none)") &&
                                                w.InterimActionTakenIfNotClearedToEOD3.ToLower().Equals("(none)") &&
                                                (
                                                    w.FinalAdjudicativeAction.ToLower().Equals("discontinued-unable to process due to lack of information") ||
                                                    w.SACAdjudicationGSA.ToLower().Equals("discontinued")
                                                ) &&
                                                (
                                                    !string.IsNullOrEmpty(w.FinalAdjudicationDate.ToString())
                                                )
                                        )
                                    .Select
                                        (
                                            s =>
                                                new AdjudicationData
                                                {
                                                    FirstName = s.FirstName,
                                                    MiddleName = s.MiddleName,
                                                    LastName = s.LastName,
                                                    Suffix = s.Suffix,
                                                    SSN = s.SSN,
                                                    HashedSSN = u.HashSSN(s.SSN),
                                                    InvestigationType = "No Action",
                                                    InvestigationDate = s.FinalAdjudicationDate,
                                                    AdjudicationStatus = ProcessedStatusConstants.NOACTION
                                                }
                                       )
                                    .ToList();

            //log number found
            log.Info("Case Discontinued: " + caseDiscontinued.Count);

            //add to processed
            processed.AddRange(caseDiscontinued);

            //remove the records from adjudications
            RemoveRecords(adjudications, caseDiscontinued);

            //log completion of function
            log.Info("Done Processing Case Discontinued");

            return;
        }

        /// <summary>
        /// Gets a list of finals and processes them
        /// </summary>
        /// <param name="adjudications"></param>
        private void ProcessFinal(List<Adjudication> adjudications)
        {
            //Log start of function
            log.Info("Processing Final");

            //Get all finals
            var finals = adjudications
                            .Where(w =>
                                        (w.FinalAdjudicativeAction.ToLower().Equals("favorable") ||
                                        w.FinalAdjudicativeAction.ToLower().Equals("favorable-reciprocity") &&
                                        (
                                            !string.IsNullOrEmpty(w.FinalAdjudicationDate.ToString())
                                        )) &&
                                        (
                                            //SAC - (AF, AG)
                                            w.SACAdjudicationGSA.ToLower().Equals("(none)") &&
                                            string.IsNullOrEmpty(w.SACAdjudicationDate.ToString())
                                        )
                                    )
                            .Select
                                (
                                    s =>
                                        new AdjudicationData
                                        {
                                            FirstName = s.FirstName,
                                            MiddleName = s.MiddleName,
                                            LastName = s.LastName,
                                            Suffix = s.Suffix,
                                            SSN = s.SSN,
                                            HashedSSN = u.HashSSN(s.SSN),
                                            DateOfEntry = s.DateOfEntry,
                                            LessThan3YearsUS = s.LessThan3YearsInUS,
                                            InvestigationType = DetermineFinalInvestigationType(s.InvestigationType1.ToLower(), s.InvestigationType2.ToLower(), s.InvestigationType3.ToLower()),
                                            InvestigationDate = s.FinalAdjudicationDate
                                        }
                                )
                            .ToList();

            //Try to process finals, catch all errors and log as fatal
            try
            {
                ProcessAdjudicationList(finals);
            }
            catch (Exception ex)
            {
                log.Fatal("Final: " + ex.Message, ex);
            }

            //log count of finals
            log.Info("Final: " + finals.Count);

            //remove from adjudications
            RemoveRecords(adjudications, finals);

            //log completion
            log.Info("Done Processing Final");

            return;
        }

        /// <summary>
        /// Gets a list of Fingerprints and processes them
        /// </summary>
        /// <param name="adjudications"></param>
        private void ProcessFingerprint(List<Adjudication> adjudications)
        {
            //log start of function
            log.Info("Processing Fingerprint");

            //get all fingerprints
            var fingerprints = adjudications
                            .Where(w =>
                                    (w.FinalAdjudicativeAction.ToLower().Equals("(none)") &&
                                    (
                                        w.InterimAdjudication1.ToLower().Equals("cleared to enter on duty (eod)") ||
                                        w.InterimAdjudication2.ToLower().Equals("cleared to enter on duty (eod)") ||
                                        w.InterimAdjudication3.ToLower().Equals("cleared to enter on duty (eod)")
                                    ) &&
                                    (
                                        !string.IsNullOrEmpty(w.InterimAdjudicationDate1.ToString()) ||
                                        !string.IsNullOrEmpty(w.InterimAdjudicationDate2.ToString()) ||
                                        !string.IsNullOrEmpty(w.InterimAdjudicationDate3.ToString())
                                    )) &&
                                    (
                                        //SAC - (AF, AG)
                                        w.SACAdjudicationGSA.ToLower().Equals("(none)") &&
                                        string.IsNullOrEmpty(w.SACAdjudicationDate.ToString())
                                    ) &&
                                    (
                                        //Final Actions (AI)
                                        //w.FinalAdjudicativeAction.ToLower().Equals("(none)") &&
                                        string.IsNullOrEmpty(w.FinalAdjudicationDate.ToString())
                                    )
                                )
                            .Select
                                (
                                    s =>
                                        new AdjudicationData
                                        {
                                            FirstName = s.FirstName,
                                            MiddleName = s.MiddleName,
                                            LastName = s.LastName,
                                            Suffix = s.Suffix,
                                            SSN = s.SSN,
                                            HashedSSN = u.HashSSN(s.SSN),
                                            DateOfEntry = s.DateOfEntry,
                                            LessThan3YearsUS = s.LessThan3YearsInUS,
                                            InvestigationType = "Fingerprint",
                                            InvestigationDate = DetermineInvestigationDate(s.InterimAdjudicationDate1, s.InterimAdjudicationDate2, s.InterimAdjudicationDate3) //new[] { s.InterimAdjudicationDate1, s.InterimAdjudicationDate2, s.InterimAdjudicationDate3 }.Min().ToString()
                                        }
                                )
                            .ToList();

            //process fingerprints
            ProcessAdjudicationList(fingerprints);

            //log count
            log.Info("Fingerprint (EOD): " + fingerprints.Count);

            //remove from adjudications
            RemoveRecords(adjudications, fingerprints);

            //log completion
            log.Info("Done Processing Fingerprint");

            return;
        }

        /// <summary>
        /// Gets a list of rescinded.  No need to process them
        /// </summary>
        /// <param name="adjudications"></param>
        private void ProcessRescinded(List<Adjudication> adjudications)
        {
            //log start of function
            log.Info("Processing Rescinded");

            //get all rescinded
            var rescinded = adjudications
                                .Where(w => w.FinalAdjudicativeAction.ToLower().Equals("(none)") &&
                                        (
                                            w.InterimAdjudication1.ToLower().Equals("rescinded eod") ||
                                            w.InterimAdjudication2.ToLower().Equals("rescinded eod") ||
                                            w.InterimAdjudication3.ToLower().Equals("rescinded eod")
                                        )
                                      )
                                .Select
                                    (
                                        s =>
                                            new AdjudicationData
                                            {
                                                FirstName = s.FirstName,
                                                MiddleName = s.MiddleName,
                                                LastName = s.LastName,
                                                Suffix = s.Suffix,
                                                SSN = s.SSN,
                                                HashedSSN = u.HashSSN(s.SSN),
                                                DateOfEntry = s.DateOfEntry,
                                                LessThan3YearsUS = s.LessThan3YearsInUS,
                                                InvestigationType = "Rescinded",
                                                InvestigationDate = null,
                                                AdjudicationStatus = ProcessedStatusConstants.RESCINDED
                                            }
                                    )
                                .ToList();

            //log count
            log.Info("Rescinded: " + rescinded.Count);

            //add to processed
            processed.AddRange(rescinded);

            //remove from adjudications
            RemoveRecords(adjudications, rescinded);

            //log completion
            log.Info("Done Processing Rescinded");

            return;
        }

        /// <summary>
        /// Gets a list of SAC and processes them
        /// </summary>
        /// <param name="adjudications"></param>
        private void ProcessSAC(List<Adjudication> adjudications)
        {
            //log start of function
            log.Info("Processing SAC");

            //get all sacs
            var sacs = adjudications
                            .Where(w =>
                                        (w.SACAdjudicationGSA.ToLower().Equals("favorable") &&
                                        (
                                            !string.IsNullOrEmpty(w.SACAdjudicationDate.ToString())
                                        )) &&
                                        (
                                            //Interim Adjudications (T, U, V)
                                            w.InterimAdjudication1.ToLower().Equals("(none)") &&
                                            w.InterimAdjudication2.ToLower().Equals("(none)") &&
                                            w.InterimAdjudication2.ToLower().Equals("(none)")
                                        ) &&
                                        (
							                //Interim Dates (Z, AA, AB)
							                string.IsNullOrEmpty(w.InterimAdjudicationDate1.ToString()) &&
							                string.IsNullOrEmpty(w.InterimAdjudicationDate2.ToString()) &&
							                string.IsNullOrEmpty(w.InterimAdjudicationDate2.ToString())
						                ) &&
                                        (
                                            //Final Actions (AH, AI)
                                            w.FinalAdjudicativeAction.ToLower().Equals("(none)") &&
                                            string.IsNullOrEmpty(w.FinalAdjudicationDate.ToString())
                                        )
                                    )
                            .Select
                                (
                                    s =>
                                        new AdjudicationData
                                        {
                                            FirstName = s.FirstName,
                                            MiddleName = s.MiddleName,
                                            LastName = s.LastName,
                                            Suffix = s.Suffix,
                                            SSN = s.SSN,
                                            HashedSSN = u.HashSSN(s.SSN),
                                            DateOfEntry = s.DateOfEntry,
                                            LessThan3YearsUS = s.LessThan3YearsInUS,
                                            InvestigationType = "SAC",
                                            InvestigationDate = s.SACAdjudicationDate
                                        }
                                )
                            .ToList();

            //try process all sacs, catch all exceptions, log errors as fatal
            try
            {
                ProcessAdjudicationList(sacs);
            }
            catch (Exception ex)
            {
                log.Fatal("SAC: " + ex.Message,ex);
            }

            //log count
            log.Info("SAC: " + sacs.Count);

            //remove from adjudications
            RemoveRecords(adjudications, sacs);

            //log completion
            log.Info("Done Processing SAC");

            return;
        }

        /// <summary>
        /// Process unfavorable sacs
        /// </summary>
        /// <param name="adjudications"></param>
        private void ProcessUnfavorableSAC(List<Adjudication> adjudications)
        {
            //log start of function
            log.Info("Processing Unfavorable SAC");

            //get all unfavorable sacs
            var unfavorableSAC = adjudications
                                .Where(w =>
                                            (
                                                w.SACAdjudicationGSA.ToLower().Equals("unfavorable")
                                            ) &&
                                            (
                                                !string.IsNullOrEmpty(w.SACAdjudicationDate.ToString())
                                            )
                                        )
                                .Select
                                    (
                                        s =>
                                            new AdjudicationData
                                            {
                                                FirstName = s.FirstName,
                                                MiddleName = s.MiddleName,
                                                LastName = s.LastName,
                                                Suffix = s.Suffix,
                                                SSN = s.SSN,
                                                HashedSSN = u.HashSSN(s.SSN),
                                                DateOfEntry = s.DateOfEntry,
                                                LessThan3YearsUS = s.LessThan3YearsInUS,
                                                InvestigationType = "Unfavorable",
                                                InvestigationDate = s.SACAdjudicationDate
                                            }
                                    )
                                .ToList();

            //process unfavorable sacs
            ProcessAdjudicationList(unfavorableSAC);

            //log count
            log.Info("Unfavorable SAC: " + unfavorableSAC.Count);

            //remove from adjudications
            RemoveRecords(adjudications, unfavorableSAC);

            //log completion
            log.Info("Done Processing Unfavorable SAC");

            return;
        }

        /// <summary>
        /// Processes unfavorable finals
        /// </summary>
        /// <param name="adjudications"></param>
        private void ProcessUnfavorableFinal(List<Adjudication> adjudications)
        {
            //log start of function
            log.Info("Processing Unfavorable Final");

            //get all unfavorable finals
            var unfavorableFinal = adjudications
                                .Where(w =>
                                            (
                                                w.FinalAdjudicativeAction.ToLower().Equals("unfavorable")
                                            ) &&
                                            (
                                                !string.IsNullOrEmpty(w.FinalAdjudicationDate.ToString())
                                            )
                                        )
                                .Select
                                    (
                                        s =>
                                            new AdjudicationData
                                            {
                                                FirstName = s.FirstName,
                                                MiddleName = s.MiddleName,
                                                LastName = s.LastName,
                                                Suffix = s.Suffix,
                                                SSN = s.SSN,
                                                HashedSSN = u.HashSSN(s.SSN),
                                                DateOfEntry = s.DateOfEntry,
                                                LessThan3YearsUS = s.LessThan3YearsInUS,
                                                InvestigationType = "Unfavorable",
                                                InvestigationDate = s.FinalAdjudicationDate
                                            }
                                    )
                                .ToList();

            //process unfavorable finals
            ProcessAdjudicationList(unfavorableFinal);

            //log count
            log.Info("Unfavorable Final: " + unfavorableFinal.Count);

            //remove from adjudications
            RemoveRecords(adjudications, unfavorableFinal);

            //log completion
            log.Info("Done Processing Unfavorable Final");

            return;
        }

        /// <summary>
        /// Gets a list of unfavorables and processes them
        /// </summary>
        /// <param name="adjudications"></param>
        /// <returns></returns>
        private void ProcessUnfavorable(List<Adjudication> adjudications)
        {
            //log start of function
            log.Info("Processing Unfavorable");

            //get all unfavorable
            var unfavorables = adjudications
                                .Where(w =>
                                            (
                                                w.SACAdjudicationGSA.ToLower().Equals("unfavorable") ||
                                                w.FinalAdjudicativeAction.ToLower().Equals("unfavorable")
                                            ) &&
                                            (
                                                !string.IsNullOrEmpty(w.SACAdjudicationDate.ToString()) ||
                                                !string.IsNullOrEmpty(w.FinalAdjudicationDate.ToString())
                                            )
                                        )
                                .Select
                                    (
                                        s =>
                                            new AdjudicationData
                                            {
                                                FirstName = s.FirstName,
                                                MiddleName = s.MiddleName,
                                                LastName = s.LastName,
                                                Suffix = s.Suffix,
                                                SSN = s.SSN,
                                                HashedSSN = u.HashSSN(s.SSN),
                                                DateOfEntry = s.DateOfEntry,
                                                LessThan3YearsUS = s.LessThan3YearsInUS,
                                                InvestigationType = "Unfavorable",
                                                InvestigationDate = s.SACAdjudicationDate == null ? s.FinalAdjudicationDate : s.SACAdjudicationDate
                                            }
                                    )
                                .ToList();

            //process unfavorable
            ProcessAdjudicationList(unfavorables);

            //log count
            log.Info("Unfavorables: " + unfavorables.Count);

            //remove from adjudications
            RemoveRecords(adjudications, unfavorables);

            //log completion
            log.Info("Done Processing Unfavorable");

            return;
        }

        //change to constants
        /// <summary>
        /// Gets a list of finals and processes them
        /// </summary>
        /// <param name="adjudications"></param>
        /// if nac/naci dates are filled leave alone
        private void ProcessWaitingForFinal(List<Adjudication> adjudications)
        {
            //log start of function
            log.Info("Processing Waiting For Final");

            //get waiting for finals
            var waitingForFinals = adjudications
                            .Where(w =>
                                    (w.FinalAdjudicativeAction.ToLower().Equals("(none)") &&
                                    (
                                        w.InterimAdjudication1.ToLower().Equals("not cleared to enter on duty (eod)") ||
                                        w.InterimAdjudication2.ToLower().Equals("not cleared to enter on duty (eod)") ||
                                        w.InterimAdjudication3.ToLower().Equals("not cleared to enter on duty (eod)")
                                    ) &&
                                    (
                                        !string.IsNullOrEmpty(w.InterimAdjudicationDate1.ToString()) ||
                                        !string.IsNullOrEmpty(w.InterimAdjudicationDate2.ToString()) ||
                                        !string.IsNullOrEmpty(w.InterimAdjudicationDate3.ToString())
                                    )) &&
                                    (
                                        //SAC - (AF, AG)
                                        w.SACAdjudicationGSA.ToLower().Equals("(none)") &&
                                        string.IsNullOrEmpty(w.SACAdjudicationDate.ToString())
                                    ) &&
                                    (
                                        //Final Actions (AH, AI)
                                        w.FinalAdjudicativeAction.ToLower().Equals("(none)") &&
                                        string.IsNullOrEmpty(w.FinalAdjudicationDate.ToString())
                                    )
                                )
                            .Select
                                (
                                    s =>
                                        new AdjudicationData
                                        {
                                            FirstName = s.FirstName,
                                            MiddleName = s.MiddleName,
                                            LastName = s.LastName,
                                            Suffix = s.Suffix,
                                            SSN = s.SSN,
                                            HashedSSN = u.HashSSN(s.SSN),
                                            DateOfEntry = s.DateOfEntry,
                                            LessThan3YearsUS = s.LessThan3YearsInUS,
                                            InvestigationType = "Waiting for final",
                                            InvestigationDate = DetermineInvestigationDate(s.InterimAdjudicationDate1, s.InterimAdjudicationDate2, s.InterimAdjudicationDate3) //new[] {s.InterimAdjudicationDate1, s.InterimAdjudicationDate2, s.InterimAdjudicationDate3}.Min().ToString()
                                        }
                                )
                            .ToList();

            //process waiting for finals
            ProcessAdjudicationList(waitingForFinals);

            //log count
            log.Info("Waiting For Final: " + waitingForFinals.Count);

            //remove from adjudications
            RemoveRecords(adjudications, waitingForFinals);

            //log completion
            log.Info("Done Processing Waiting For Final");

            return;
        }
        #endregion "Filter Adjudication Data"
    }
}
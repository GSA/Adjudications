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
    public class ProcessAdjudications
    {
        private static CsvConfiguration config; // = new CsvConfiguration();
        private static int defaultColumnCount = 36; //Should be a constant?
        private static List<Investigation> investigations; //= new List<Investigation>();
        private static bool isDebug; // = true;
        private static List<AdjudicationData> processed; //= new List<AdjudicationData>();
        //Need better naming namespace and convention here
        private static Utilities.Utilities u = new Utilities.Utilities();

        private readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        /// Initializes the process to process the adjudication files
        /// </summary>
        public ProcessAdjudications(bool debugMode) //bool debugMode//, log4net.ILog appLog)
        {
            config = new CsvConfiguration();
            investigations = new List<Investigation>();
            processed = new List<AdjudicationData>();

            int investColumnCount = 0;

            config.Delimiter = ",";
            config.HasHeaderRecord = true;
            config.WillThrowOnMissingField = true;
            config.IsHeaderCaseSensitive = false;
            config.TrimHeaders = false;

            //Sets Debug Mode
            isDebug = debugMode; //bool.Parse(ConfigurationManager.AppSettings["DEBUGMODE"]);

            //Loads the investigation data (lookup)
            investigations = GetFileData<Investigation, InvestigationMapping>(AppDomain.CurrentDomain.BaseDirectory + "Lookups\\Investigations.csv", config, out investColumnCount);

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
        /// 
        ///Add IsNullOrEmpty to the quries for all the other columsnt hat are not associated with that method.        
        public Tuple<int, string> ProcessAdjudicationFile(string adjudicationFile)
        {
            try
            {
                log.Info("Processing Adjudication File: " + adjudicationFile);

                int fileColumnCount = 0;
                string errors = string.Empty;
                int totalProcessed = 0;

                List<Adjudication> adjudicationList = new List<Adjudication>();

                //Gets all the data
                adjudicationList = GetFileData<Adjudication, AdjudicationMapping>(adjudicationFile, config, out fileColumnCount);

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

                log.Info("Done Processing Adjudication File: " + adjudicationFile);

                return new Tuple<int, string>(totalProcessed, errors);
            }
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
            adjudications.RemoveAll(r => adjudicationData.Any(a => a.SSN == r.SSN && a.LastName == r.LastName));
        }

        private void GenerateSummaryFile(List<Adjudication> indeterminable, string fileName)
        {
            List<AdjudicationData> summary = new List<AdjudicationData>();

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

            log.Info("Summary Count: " + summary.Count);

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
        /// Loads the adjudiction information
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <typeparam name="TMap"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="config"></param>
        /// <param name="columnCount"></param>
        /// <returns></returns>
        private List<TClass> GetFileData<TClass, TMap>(string filePath, CsvConfiguration config, out int columnCount)
            where TClass : class
            where TMap : CsvClassMap<TClass>
        {
            using (CsvParser csvParser = new CsvParser(new StreamReader(filePath), config))
            {
                using (CsvReader csvReader = new CsvReader(csvParser))
                {
                    csvReader.Configuration.RegisterClassMap<TMap>();

                    List<TClass> allRecords = csvReader.GetRecords<TClass>().ToList();

                    columnCount = csvReader.FieldHeaders.Count();

                    return allRecords;
                }
            }
            //CsvParser csvParser = new CsvParser(new StreamReader(filePath), config);
            //CsvReader csvReader = new CsvReader(csvParser);

            //csvReader.Configuration.RegisterClassMap<TMap>();

            //List<TClass> allRecords = csvReader.GetRecords<TClass>().ToList();

            //columnCount = csvReader.FieldHeaders.Count();

            //csvReader.Dispose();
            //csvParser.Dispose();

            //return allRecords; //csvReader.GetRecords<TClass>().ToList();
        }

        /// <summary>
        /// Checks wether or not the file can be processed
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

            if (duplicates)
            {
                log.Error("Duplicate Last Name + SSN Match Found");

                error = "Duplicate Last Name + SSN Match Found";

                return false;
            }

            return true;
        }
        /// <summary>
        /// Processes each individual adjudiction and saves to the DB
        /// </summary>
        /// <param name="adjudications"></param>
        private void ProcessAdjudicationList(List<AdjudicationData> adjudications)
        {
            SaveAdjudications save = new SaveAdjudications();
            AdjudicationEMails adjudicationEMails = new AdjudicationEMails();

            //Item 1 = ID, Item 2 = Status, Item 3 = Send E-Mail
            Tuple<int, string, bool> result = new Tuple<int, string, bool>(0, string.Empty, false);

            //log.Info("Start: Looping Adjdications List");

            foreach (AdjudicationData adjudicationData in adjudications)
            {
                //log.Info(adjudicationData.FirstName + ' ' + adjudicationData.LastName);
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

                //result = save.SaveAdjudication(adjudicationData, ssn);
                result = save.SaveAdjudication(adjudicationData, isDebug);

                adjudicationData.ID = result.Item1;
                adjudicationData.AdjudicationStatus =result.Item2;
                adjudicationData.EMailRequested = result.Item3;

                processed.Add(adjudicationData);

                if (adjudicationData.EMailRequested && !isDebug && adjudicationData.AdjudicationStatus != "Duplicate")
                {
                    //need a try catch here
                    adjudicationEMails.SendNotice(adjudicationData.ID);
                    Thread.Sleep(2500);
                }
            }

            save.Dispose();
        }
        #region "Filter Adjudication Data"

        private bool CheckedGreaterThan3Years(DateTime? portOfEntryDate, DateTime? investigationDate)
        {
            if (portOfEntryDate == null)
                return false;

            DateTime poeDate = Convert.ToDateTime(portOfEntryDate);            
            DateTime investDate = (DateTime)investigationDate;

            //This should also handle leap years
            if (((investDate - poeDate).TotalDays / 365.2425) >= 3)
            {
                return true;
            }

            return false;
        }

        private bool CheckedLessThan3Years(DateTime? portOfEntryDate, DateTime? investigationDate)
        {
            if (portOfEntryDate == null)
                return false;

            DateTime poeDate = Convert.ToDateTime(portOfEntryDate);
            DateTime investDate = (DateTime)investigationDate;

            //This should also handle leap years
            if (((investDate - poeDate).TotalDays / 365.2425) <= 3)
            {
                return true;
            }

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

            //switch (investigationType)
            //{
            //    case "tier 1":
            //        investigationType = "NACI";
            //        break;

            //    case "tier 2":
            //        investigationType = "MBI";
            //        break;

            //    case "ssbi-ppr":
            //        investigationType = "PPR";
            //        break;

            //    default:
            //        break;
            //}

            return investigationType;
        }

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
        /// Gets a list of case disontinued.  No need to process these
        /// </summary>
        /// <param name="adjudications"></param>
        /// <returns></returns>
        private void ProcessCaseDiscontinued(List<Adjudication> adjudications)
        {
            log.Info("Processing Case Discontinued");

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

            log.Info("Case Discontinued: " + caseDiscontinued.Count);

            processed.AddRange(caseDiscontinued);

            RemoveRecords(adjudications, caseDiscontinued);

            log.Info("Done Processing Case Discontinued");

            return;
        }

        /// <summary>
        /// Gets a list of finals and processes them
        /// </summary>
        /// <param name="adjudications"></param>
        private void ProcessFinal(List<Adjudication> adjudications)
        {
            log.Info("Processing Final");

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

            try
            {
                ProcessAdjudicationList(finals);
            }
            catch (Exception ex)
            {
                log.Fatal("Final: " + ex.Message, ex);
                //foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(finals))
                //{
                //    string name = descriptor.Name;
                //    object value = descriptor.GetValue(finals);
                //    log.Fatal(name + ": " + value);
                //}
                //throw;
            }

            log.Info("Final: " + finals.Count);

            RemoveRecords(adjudications, finals);

            log.Info("Done Processing Final");

            return;
        }

        /// <summary>
        /// Gets a list of Fingerprints and processes them
        /// </summary>
        /// <param name="adjudications"></param>
        private void ProcessFingerprint(List<Adjudication> adjudications)
        {
            log.Info("Processing Fingerprint");

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

            ProcessAdjudicationList(fingerprints);

            log.Info("Fingerprint (EOD): " + fingerprints.Count);

            RemoveRecords(adjudications, fingerprints);

            log.Info("Done Processing Fingerprint");

            return;
        }

        /// <summary>
        /// Gets a list of rescinded.  No need to process them
        /// </summary>
        /// <param name="adjudications"></param>
        private void ProcessRescinded(List<Adjudication> adjudications)
        {
            log.Info("Processing Rescinded");

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

            log.Info("Rescinded: " + rescinded.Count);

            processed.AddRange(rescinded);

            RemoveRecords(adjudications, rescinded);

            log.Info("Done Processing Rescinded");

            return;
        }

        /// <summary>
        /// Gets a list of SAC and processes them
        /// </summary>
        /// <param name="adjudications"></param>
        private void ProcessSAC(List<Adjudication> adjudications)
        {
            log.Info("Processing SAC");

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

            try
            {
                ProcessAdjudicationList(sacs);
            }
            catch (Exception ex)
            {
                log.Fatal("SAC: " + ex.Message,ex);
                //foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(sacs))
                //{
                //    string name = descriptor.Name;
                //    object value = descriptor.GetValue(sacs);
                //    log.Fatal(name + ": " + value);
                //}
                //throw;
            }
            
            

            log.Info("SAC: " + sacs.Count);

            RemoveRecords(adjudications, sacs);

            log.Info("Done Processing SAC");

            return;
        }

        private void ProcessUnfavorableSAC(List<Adjudication> adjudications)
        {
            log.Info("Processing Unfavorable SAC");

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

            ProcessAdjudicationList(unfavorableSAC);

            log.Info("Unfavorable SAC: " + unfavorableSAC.Count);

            RemoveRecords(adjudications, unfavorableSAC);

            log.Info("Done Processing Unfavorable SAC");

            return;
        }

        private void ProcessUnfavorableFinal(List<Adjudication> adjudications)
        {
            log.Info("Processing Unfavorable Final");

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

            ProcessAdjudicationList(unfavorableFinal);

            log.Info("Unfavorable Final: " + unfavorableFinal.Count);

            RemoveRecords(adjudications, unfavorableFinal);

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
            log.Info("Processing Unfavorable");

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

            ProcessAdjudicationList(unfavorables);

            log.Info("Unfavorables: " + unfavorables.Count);

            RemoveRecords(adjudications, unfavorables);

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
            log.Info("Processing Waiting For Final");

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

            ProcessAdjudicationList(waitingForFinals);

            log.Info("Waiting For Final: " + waitingForFinals.Count);

            RemoveRecords(adjudications, waitingForFinals);

            log.Info("Done Processing Waiting For Final");

            return;
        }
        #endregion "Filter Adjudication Data"
    }
}
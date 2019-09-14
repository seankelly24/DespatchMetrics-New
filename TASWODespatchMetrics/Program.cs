using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TASWODespatchMetrics
{
    class Program
    {
        static void Main(string[] args)
        {
            reportThas01Entities thas01dB = new reportThas01Entities();
            ConnectDbEntities cDb = new ConnectDbEntities();
            thas01dB.Database.CommandTimeout = 1500;

            FileInfo fileInfo;
            string theDate = DateTime.Now.ToString("yyyyMMdd");
            string theDateHours = DateTime.Now.ToString("yyyyMMdd HH.mm.ss");

            string from = "TaskMaster@thompsonaero.com";
            string to = "sean.kelly@thompsonaero.com";

            try
            {
                if (CreateDirectoryStructure(out fileInfo, theDate, theDateHours, @"PAXProduced\DespatchMetrics", @"MRP Standup Reports", false))
                {
                    using (ExcelPackage excelPackage = new ExcelPackage(fileInfo))
                    {
                        var ws1_Despatch = excelPackage.Workbook.Worksheets.Add("Despatch");
                        string regexPattern = @"\{\*?\\[^{}]+}|[{}]|\\\n?[A-Za-z]+\n?(?:-?\d+)?[ ]?";
                        Regex rgx = new Regex(regexPattern);

                        var resultCount = 0;

                        var despatchSeatQuery = thas01dB.THAS_CONNECT_DespatchSeats().ToList();
                        resultCount = despatchSeatQuery.Count();
                        var failCount = 0;
                        while (resultCount == 0 && failCount < 10)
                        {
                            Thread.Sleep(10000);
                            failCount++;
                            despatchSeatQuery = thas01dB.THAS_CONNECT_DespatchSeats().ToList();
                        }

                        var groupedUp = despatchSeatQuery.OrderBy(x => x.Transaction_Date).GroupBy(x => x.SerialNumber).ToList();
                        var seatExport = new List<seatRecoveryExport>();
                        var despatchExport = new List<despatchExport>();
                        foreach (var line in groupedUp)
                        {
                            var export = new despatchExport();
                            export.WorksOrderNumber = line.First().WorksOrderNumber;
                            export.CommercialNote = line.First().CommercialNotes != null ? rgx.Replace(line.First().CommercialNotes, "") : "";
                            export.SerialNumber = line.First().SerialNumber;
                            export.TransactionDate = line.First().Transaction_Date;
                            export.PartNumber = line.First().Part_Number;
                            export.PartDescription = line.First().Part_Description;
                            export.PaxCount = (line.First().Part_Description.ToLower().Contains("double") || line.First().Part_Description.ToLower().Contains("dbl") || line.First().Part_Number.ToLower() == "vt36-00-201-01-lx02") ? 2 :
                                               line.First().Part_Description.ToLower().Contains("triple") ? 3 : 1;
                            export.Batch = line.First().Batch;
                            export.BatchLocation = line.First().Batch_Location;
                            export.CurrentBatchLocation = line.First().Current_Batch_Location;
                            export.Username = line.First().User_Name;
                            export.PartDescription = line.First().Part_Description;
                            export.MethodType = line.First().Method_Type;
                            export.DefaultLocationCode = line.First().Default_Location_Code;
                            export.ProductGroupCode = line.First().Product_Group_Code;
                            export.RespCode = line.First().Responsibility_Codes;
                            if (export.CommercialNote.ToLower().Contains("-ss") || export.CommercialNote.ToLower().Contains("bo1") || export.CommercialNote.ToLower().Contains("fai") || export.CommercialNote.ToLower().Contains("fpdr"))
                            {
                                despatchExport.Add(export);
                            }
                        }

                        try
                        {
                            var checkTableForRecords = cDb.SeatThroughputHourlyResultSets.ToList();
                            if (checkTableForRecords.Count() > 0)
                            {
                                cDb.SeatThroughputHourlyResultSets.RemoveRange(cDb.SeatThroughputHourlyResultSets);
                                cDb.SaveChanges();
                            }
                            CopyDespatchThroughputToDB(despatchExport);
                        }
                        catch (Exception ex)
                        {
                            using (MailMessage mail = new MailMessage(from, to))
                            {

                                mail.Subject = "TAS WO Despatch Metrics Generation Failure";
                                mail.Body = "An error has occurred, exception message: " + ex.Message + " Inner Exception: " + ex.InnerException;
                                mail.IsBodyHtml = true;
                                SmtpClient client = new SmtpClient("remote.thompsonaero.com");
                                client.Send(mail);
                            }
                        }

                        ws1_Despatch.Cells["A1"].LoadFromCollection(despatchExport, true, OfficeOpenXml.Table.TableStyles.Medium2);
                        ws1_Despatch.Cells[ws1_Despatch.Dimension.Address].AutoFitColumns();
                        int sheetCount = ws1_Despatch.Dimension.Rows;
                        ws1_Despatch.Cells["D2:D" + sheetCount].Style.Numberformat.Format = "dd-mm-yyyy";
                        excelPackage.Save();
                    }
                    using (MailMessage mail = new MailMessage(from, to))
                    {

                        mail.Subject = "TAS WO Despatch Metrics Generation Successful";
                        mail.Body = "Metrics have been successfully generated and saved.";
                        mail.IsBodyHtml = true;
                        SmtpClient client = new SmtpClient("remote.thompsonaero.com");
                        client.Send(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                using (MailMessage mail = new MailMessage(from, to))
                {

                    mail.Subject = "TAS WO Despatch Metrics Generation Failure";
                    mail.Body = "An error has occurred, exception message: " + ex.Message + " Inner Exception: " + ex.InnerException;
                    mail.IsBodyHtml = true;
                    SmtpClient client = new SmtpClient("remote.thompsonaero.com");
                    client.Send(mail);
                }
            }
            Console.WriteLine("Finished Despatch");
            Console.ReadKey();
            try
            {
                if (CreateDirectoryStructure(out fileInfo, theDate, theDateHours, @"WOVSThroughput\WorksOrderThroughput", @"MRP Standup Reports", false))
                {
                    using (ExcelPackage excelPackage = new ExcelPackage(fileInfo))
                    {
                        var woQuery = excelPackage.Workbook.Worksheets.Add("WOVSThroughput");
                        string regexPattern = @"\{\*?\\[^{}]+}|[{}]|\\\n?[A-Za-z]+\n?(?:-?\d+)?[ ]?";
                        Regex rgx = new Regex(regexPattern);

                        var resultCount = 0;
                        var VSThroughputQuery = thas01dB.THAS_CONNECT_VSWOThroughput().ToList();
                        var failCount = 0;
                        resultCount = VSThroughputQuery.Count();
                        while (resultCount == 0 && failCount < 10)
                        {
                            Thread.Sleep(10000);
                            failCount++;
                            VSThroughputQuery = thas01dB.THAS_CONNECT_VSWOThroughput().ToList();

                        }

                        foreach (var line in VSThroughputQuery)
                        {
                            line.SalesNotes = line.SalesNotes != null ? rgx.Replace(line.SalesNotes, "") : "";
                        }

                        woQuery.Cells["A1"].LoadFromCollection(VSThroughputQuery, true, OfficeOpenXml.Table.TableStyles.Medium2);
                        woQuery.Cells[woQuery.Dimension.Address].AutoFitColumns();
                        int sheetCount = woQuery.Dimension.Rows;
                        
                        excelPackage.Save();
                    }
                   
                }
            }
            catch (Exception ex)
            {
                using (MailMessage mail = new MailMessage(from, to))
                {

                    mail.Subject = "TAS WO Despatch Metrics Generation Failure";
                    mail.Body = "An error has occurred, exception message: " + ex.Message + " Inner Exception: " + ex.InnerException;
                    mail.IsBodyHtml = true;
                    SmtpClient client = new SmtpClient("remote.thompsonaero.com");
                    client.Send(mail);
                }
            }

        }
        private static bool CreateDirectoryStructure(out FileInfo fileInfo, string date, string dateHours, string filename)
        {
            string path = @"\\thas-nas01\DepartmentShares$\TAS Perform\TAS Daily Metrics\{0}\PAXProduced\";

            fileInfo = new FileInfo(string.Format(path + filename + "_{1}.xlsx", date, dateHours));
            try
            {
                var fullpath = string.Format(path + filename + "_{1}.xlsx", date, dateHours);
                if (!File.Exists(fullpath))
                {
                    fileInfo = new FileInfo(fullpath);
                    fileInfo.Directory.Create();
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Issue : " + ex.Message);
                return false;
            }
        }

        private static bool CreateDirectoryStructureV2(out FileInfo fileInfo, string date, string dateHours, string filename)
        {
            string path = @"\\thas-nas01\DepartmentShares$\TAS Perform\TAS Daily Metrics\{0}\WOVSThroughput\";

            fileInfo = new FileInfo(string.Format(path + filename + "_{1}.xlsx", date, dateHours));
            try
            {
                var fullpath = string.Format(path + filename + "_{1}.xlsx", date, dateHours);
                if (!File.Exists(fullpath))
                {
                    fileInfo = new FileInfo(fullpath);
                    fileInfo.Directory.Create();
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Issue : " + ex.Message);
                return false;
            }
        }
        private static bool CreateDirectoryStructure(out FileInfo fileInfo, string date, string dateHours, string filename, string folderPath, bool costed)
        {
            string path = @"\\tas\reports$\{0}\{1}\";
            if (costed)
            {
                path = @"\\tas\reports$\{0}\With Costing Info\{1}\";
            }
            else
            {
                path = @"\\tas\reports$\{0}\Without Costing Info\{1}\";
            }


            fileInfo = new FileInfo(string.Format(path + filename + "_{2}.xlsx", folderPath, date, dateHours));
            try
            {
                var fullpath = string.Format(path + filename + "_{2}.xlsx", folderPath, date, dateHours);
                if (!File.Exists(fullpath))
                {
                    fileInfo = new FileInfo(fullpath);
                    fileInfo.Directory.Create();
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Issue : " + ex.Message);
                return false;
            }
        }

        public static void CopyDespatchThroughputToDB(List<despatchExport> dataSet)
        {
            ConnectDbEntities connect = null;
            try
            {
                connect = new ConnectDbEntities();
                connect.Configuration.AutoDetectChangesEnabled = false;

                int count = 0;
                foreach (var line in dataSet)
                {
                    ++count;
                    var record = new SeatThroughputHourlyResultSet();
                    record.WorksOrderNumber = line.WorksOrderNumber;
                    record.CommercialNote = line.CommercialNote;
                    record.SerialNumber = line.SerialNumber;
                    record.TransactionDate = line.TransactionDate;
                    record.PartNumber = line.PartNumber;
                    record.PartDescription = line.PartDescription;
                    record.PaxCount = line.PaxCount;
                    record.Batch = line.Batch;
                    record.BatchLocation = line.BatchLocation;
                    record.CurrentBatchLocation = line.CurrentBatchLocation;
                    record.Username = line.Username;
                    record.MethodType = line.MethodType;
                    record.DefaultLocationCode = line.DefaultLocationCode;
                    record.ProductGroupCode = line.ProductGroupCode;
                    record.RespCode = line.RespCode;

                    connect = AddToContextSeatThroughput(connect, record, count, 500, true);
                }
                connect.SaveChanges();
            }
            finally
            {
                if (connect != null)
                    connect.Dispose();
            }

        }
        private static ConnectDbEntities AddToContextSeatThroughput(ConnectDbEntities context, SeatThroughputHourlyResultSet entity, int count, int commitCount, bool recreateContext)
        {
            context.Set<SeatThroughputHourlyResultSet>().Add(entity);

            if (count % commitCount == 0)
            {
                context.SaveChanges();
                if (recreateContext)
                {
                    context.Dispose();
                    context = new ConnectDbEntities();
                    context.Configuration.AutoDetectChangesEnabled = false;
                }
            }
            return context;
        }
    }
}


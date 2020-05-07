using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogExtractionUtility
{
    class Program
    {
        static List<SourceLocation> sourceLocations = ConfigurationManager.GetSection("SourceLocations") as List<SourceLocation>;
        static string currentDateOffset = System.Configuration.ConfigurationManager.AppSettings["NumberOfPreviousDaysOfLogsToCollect"];
        static string tempArchivePlaceLocation = System.Configuration.ConfigurationManager.AppSettings["TempArchivePlaceLocation"]; 
        static string destinationLocation = System.Configuration.ConfigurationManager.AppSettings["DestinationLocation"]; 
        static string archiveNamePrefix = System.Configuration.ConfigurationManager.AppSettings["ArchiveNamePrefix"]; 

        static void Main(string[] args)
        {
            int dateOff = -1;
            if(!Int32.TryParse(currentDateOffset, out dateOff) || dateOff <0)
            {
                Console.WriteLine("Invalid date range {0} specified in the config file. Aborting..", currentDateOffset);
                return;
            }
            DateTime endDate = DateTime.Now.Date;
            DateTime startDate = endDate.AddDays(-1 * dateOff);


            var drive = Path.GetPathRoot(destinationLocation).ToUpper();            
            var drives  = Directory.GetLogicalDrives();
            if(!drives.Contains(drive))
            {
                Console.WriteLine(@"Specified target location {0} is not connected. Aborting...", drive);
                return;
            }

            foreach (var srcLoc in sourceLocations)
            {
                if (!Directory.Exists(srcLoc.Location))
                {
                    Console.WriteLine(@"Specified Log file source location {0} is not found. Aborting...", srcLoc.Location);
                    return;
                }
            }

            if(startDate.CompareTo(endDate) > 0)
            {
                Console.WriteLine("specified date range is not valid, Aborting. {0} - {1}", startDate.ToShortDateString(), endDate.ToShortDateString());
                return;
            }

            Dictionary<SourceLocation, List<string>> locationWiseLogFiles = new Dictionary<SourceLocation, List<string>>();

            foreach(var srcLoc in sourceLocations.Distinct())
            {
                var logFileSourceLocation = srcLoc.Location;
                var folders = Directory.EnumerateDirectories(logFileSourceLocation).ToList();

                List<string> datafiles = new List<string>();

                for (int i = 0; i < folders.Count(); i++)
                {
                    datafiles.AddRange(Directory.EnumerateFiles(folders[i])
                        .Select(fileName => new { name = fileName, date = new FileInfo(fileName).CreationTime.Date })
                        .Where(file => file.date >= startDate && file.date <= endDate)
                        .Select(file => file.name).ToList());
                }

                locationWiseLogFiles.Add(srcLoc, datafiles);
            }

            if (!Directory.Exists(tempArchivePlaceLocation))
                Directory.CreateDirectory(tempArchivePlaceLocation);

            string zipFileName = Path.Combine(tempArchivePlaceLocation, archiveNamePrefix + "_" + startDate.ToString("yyMMdd") + "_" + endDate.ToString("yyMMdd") + ".zip");

            if (File.Exists(zipFileName))
                File.Delete(zipFileName);

            using (ZipArchive zip = ZipFile.Open(zipFileName, ZipArchiveMode.Create))
            {
                foreach (var kvp in locationWiseLogFiles)
                {
                    kvp.Value.ForEach(f => zip.CreateEntryFromFile(Path.Combine(kvp.Key.Location, f), kvp.Key.Key+"\\"+Path.GetFileName(f)));
                }
            }

            if (!Directory.Exists(destinationLocation))
                Directory.CreateDirectory(destinationLocation);

            try
            {

                File.Copy(zipFileName, Path.Combine(destinationLocation, Path.GetFileName(zipFileName)), true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error trying to copy files to destination location.");
                Console.WriteLine(ex);
            }
            finally
            {
                File.Delete(zipFileName);
            }
        }
    }
}

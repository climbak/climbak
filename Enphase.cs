 /*
 * Enphase data collection program
 * Author:              Ryan Courreges
 * Publication Date:    6/18/2012
 * Description:         Program connects to the Enphase Enlighten site and downloads
 *                  the power output time-series data for each microinverter in the
 *                  array and saves it to disk. It also downloads the energy data and
 *                  stores it to disk in a separate set of files. This data can then 
 *                  be used to compare within externally defined sub-arrays or on a per 
 *                  module basis.
 *
 * Changelog:           6/28/2012 - finished adding energy data download ability. Fixed
 *                  file overwriting issue from file not found exception being thrown.
 *
 * Acknowledgements:  Thanks to Michael Park for all the help figuring out how to use C#.
 */



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
namespace Enphase_Data_Retrieval
{
    class Program
    {
        static void Main(string[] args)
        {
            /*****    
             ***** 
             ***** Enter your username and password in the quotes here
             *****  
             *****/
            string myUserName = "username";
            string myPassword = "password";

            /****
             ****
             **** You will need to enter the individual links to your power data downloads here. I left 4 sample links for the power and energy data as reference.
             **** In the "modulesPower" array you will only need to deal with the authenticity token. In your link there will be a 40-something character value
             **** after the "authenticity_token=" in the address. Replace this entire token with "{0} as I did below
             **** 
             ****/
            string[] modulesPower = new string[]{ // Replace the authenticity token here |
                "https://enlighten.enphaseenergy.com/systems/.../...&authenticity_token={0}&commit...&report[title]=Microinverter+Recent+Power+Production...",
                "https://enlighten.enphaseenergy.com/systems/.../...&authenticity_token={0}&commit...&report[title]=Microinverter+Recent+Power+Production...",
                "https://enlighten.enphaseenergy.com/systems/.../...&authenticity_token={0}&commit...&report[title]=Microinverter+Recent+Power+Production...",
                "https://enlighten.enphaseenergy.com/systems/.../...&authenticity_token={0}&commit...&report[title]=Microinverter+Recent+Power+Production..."
            };

            /****
             ****
             **** For "modulesEnergy", you will have to change a few extra items. Replace the authenticity token just like you did in the "modulesPower" array.
             **** In addition, replace the end date with {1} for the month, {2} for the day and {3} for the year. If you look at my sample links below you will
             **** see the part that says "&report[enddate]={1}%2F{2}%2F{3}". Leave the start date section alone unless you want to add specific start dates.
             ****
             ****/
            string[] modulesEnergy = new string[]{ //            authenticity token here |         month/day/year here |     |     |
                "https://enlighten.enphaseenergy.com/systems/.../...&authenticity_token={0}&commit...&report[enddate]={1}%2F{2}%2F{3}&report[startdate]=6%2F5%2F12&report[title]=Microinverter+Energy+Production...",
                "https://enlighten.enphaseenergy.com/systems/.../...&authenticity_token={0}&commit...&report[enddate]={1}%2F{2}%2F{3}&report[startdate]=6%2F5%2F12&report[title]=Microinverter+Energy+Production...",
                "https://enlighten.enphaseenergy.com/systems/.../...&authenticity_token={0}&commit...&report[enddate]={1}%2F{2}%2F{3}&report[startdate]=6%2F5%2F12&report[title]=Microinverter+Energy+Production...",
                "https://enlighten.enphaseenergy.com/systems/.../...&authenticity_token={0}&commit...&report[enddate]={1}%2F{2}%2F{3}&report[startdate]=6%2F5%2F12&report[title]=Microinverter+Energy+Production..."
            };

            /****
             ****
             **** This is where you enter the directory you want the data to be saved. If you want the files to be saved to the same
             **** directory as the executable file is in, just change the value to filePath = "";
             ****
             ****/
            string filePath = @"C:\myDirectory\Enphase Data\";

            /****
             ****
             **** Enter the filenames for each module here. I named my files based off of the inverter number, but you can
             **** use whatever naming scheme you want. Just make sure you have the same number of names as download links.
             ****
             ****/
            string[] filenamesPower = new string[]
            {
                "inverterXXXXXXXXXXXX_power",
                "inverterXXXXXXXXXXXX_power",
                "inverterXXXXXXXXXXXX_power",
                "inverterXXXXXXXXXXXX_power"
            };

            string[] filenamesEnergy = new string[]
            {
                "inverterXXXXXXXXXXXX_energy",
                "inverterXXXXXXXXXXXX_energy",
                "inverterXXXXXXXXXXXX_energy",
                "inverterXXXXXXXXXXXX_energy"
            };


            // needs to store cookies to stay logged in
            CookieContainer cookie_container = new CookieContainer();

            // create the web request
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create("https://enlighten.enphaseenergy.com/login/login");
            req.CookieContainer = cookie_container;
            req.Method = "POST";

            // spoof user agent
            req.UserAgent = "Mozlla/5.0 (compatible; MSIE 8.0; Windows NT 6.0; Trident/4.0; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; .NET CLR 1.0.3705; .NET CLR 1.1.4322)";

            // write to post data
            StreamWriter sw_request = new StreamWriter(req.GetRequestStream());
            sw_request.Write(String.Format("user[email]={0}&user[password]={1}", myUserName, myPassword));
            sw_request.Close();

            // get response (this is where you actually go the the url)
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();

            string auth_token = null;

            // get today's date for the energy data request
            DateTime dateToday = DateTime.Today;
            int month = dateToday.Month;
            int day = dateToday.Day - 1;
            int year = dateToday.Year % 100;

            using (StreamReader sr = new StreamReader(res.GetResponseStream()))
            {
                string response = sr.ReadToEnd();


                if (response.Contains("Log Out"))
                {
                    Console.WriteLine("\nLogin Successful!\n");

                    int index = response.IndexOf("name=\"authenticity_token\"");

                    int startIndex = index + 47;

                    auth_token = response.Substring(startIndex, 43);

                }

                else
                {
                    Console.WriteLine("Login unsuccessful! Exiting...");

                    /*  Wait for 5 seconds so the user can see login was unsuccessful   */
                    Stopwatch sw = new Stopwatch(); // sw cotructor
                    sw.Start(); // starts the stopwatch
                    for (int z = 0; ; z++)
                    {
                        if (z % 100000 == 0) // if in 100000th iteration (could be any other large number
                        // depending on how often you want the time to be checked) 
                        {
                            sw.Stop(); // stop the time measurement
                            if (sw.ElapsedMilliseconds > 5000) // check if desired period of time has elapsed
                            {
                                break; // if more than 5000 milliseconds have passed, stop looping and return
                                // to the existing code
                            }
                            else
                            {
                                sw.Start(); // if less than 5000 milliseconds have elapsed, continue looping
                                // and resume time measurement
                            }
                        }
                    }

                    // Exit application
                    Environment.Exit(0);
                }
            }

            
            int i = 0;

            // download and save every module's power data
            foreach (string url in modulesPower)
            {

                Console.Write(String.Format("Power file {0,2} downloading...", i+1));

                // create web request and download data
                HttpWebRequest req_csv = (HttpWebRequest)HttpWebRequest.Create(String.Format(url, auth_token));
                req_csv.CookieContainer = cookie_container;
                HttpWebResponse res_csv = (HttpWebResponse)req_csv.GetResponse();

                // save the data to files
                using (StreamReader sr = new StreamReader(res_csv.GetResponseStream()))
                {
                    string response = sr.ReadToEnd();
                    string fileName = filenamesPower[i] + ".csv";
                    string path = Path.Combine(filePath, fileName);

                    // save the data to file
                    try
                    {
                        int startIndex = 0;          // start index for substring to append to file
                        int searchResultIndex = 0;   // index returned when searching downloaded data for last entry of data on file
                        string lastEntry;            // will hold the last entry in the current data
                        //open existing file and find last entry
                        using (StreamReader sr2 = new StreamReader(path))
                        {
                            //get last line of existing data
                            string fileContents = sr2.ReadToEnd();
                            string nl = System.Environment.NewLine;  // newline string
                            int nllen = nl.Length;                   // length of a newline
                            if (fileContents.LastIndexOf(nl) == fileContents.Length - nllen)
                            {
                                lastEntry = fileContents.Substring(0, fileContents.Length - nllen).Substring(fileContents.Substring(0, fileContents.Length - nllen).LastIndexOf(nl) + nllen);
                            }
                            else
                            {
                                lastEntry = fileContents.Substring(fileContents.LastIndexOf(nl) + 2);
                            }
                            //Debug.WriteLine(lastEntry);

                            // search the new data for the last existing line
                            searchResultIndex = response.LastIndexOf(lastEntry);
                        }

                        // if the downloaded data contains the last record on file, append the new data
                        if (searchResultIndex != -1)
                        {
                            startIndex = searchResultIndex + lastEntry.Length;
                            if (startIndex < response.Length - 50)
                            {
                                File.AppendAllText(path, response.Substring(startIndex + 2));
                            }
                        }
                        // else append all the data
                        else
                        {
                            Console.Write("The last entry of the existing data was not found\nin the downloaded data. Appending all data...");
                            File.AppendAllText(path, response.Substring(110)); // the index of 109 removes the file header
                        }
                    }
                    // if there is no file for this module, create the first one
                    catch (FileNotFoundException e)
                    {
                        // write data to file
                        //Console.WriteLine(path); // debugging statement
                        Console.Write("file does not exist, creating new file...");
                        File.WriteAllText(path, response);
                        //Console.ReadKey(); // for debugging
                    }

                }

                Console.WriteLine("finished.");
                i++;
            }

            Console.WriteLine("\nPower data finished!\n");
            //Debug.WriteLine("\nFinished\n");
            //Console.ReadKey();

            /*  Now get energy data.  */

            i = 0;

            foreach (string url in modulesEnergy)
            {
                // Debug.WriteLine(String.Format(url, auth_token));

                Console.Write(String.Format("Energy file {0,2} downloading...", i+1));

                // create web request and download data
                HttpWebRequest req_csv = (HttpWebRequest)HttpWebRequest.Create(String.Format(url, auth_token, "" + month, "" + day, "" + year));
                req_csv.CookieContainer = cookie_container;
                HttpWebResponse res_csv = (HttpWebResponse)req_csv.GetResponse();

                // save the data to files
                using (StreamReader sr = new StreamReader(res_csv.GetResponseStream()))
                {
                    string response = sr.ReadToEnd();
                    int energyEndIndex = response.LastIndexOf("Total");
                    response = response.Substring(0, energyEndIndex);
                    string fileName = filenamesEnergy[i] + ".csv";
                    string path = Path.Combine(filePath, fileName);

                    // save the data to file
                    try
                    {
                        int startIndex = 0;          // start index for substring to append to file
                        int searchResultIndex = 0;   // index returned when searching downloaded data for last entry of data on file
                        string lastEntry;            // will hold the last entry in the current data
                        //open existing file and find last entry
                        using (StreamReader sr2 = new StreamReader(path))
                        {
                            //get last line of existing data
                            string fileContents = sr2.ReadToEnd();
                            string nl = System.Environment.NewLine;  // newline string
                            int nllen = nl.Length;                   // length of a newline
                            if (fileContents.LastIndexOf(nl) == fileContents.Length - nllen)
                            {
                                lastEntry = fileContents.Substring(0, fileContents.Length - nllen).Substring(fileContents.Substring(0, fileContents.Length - nllen).LastIndexOf(nl) + nllen);
                            }
                            else
                            {
                                lastEntry = fileContents.Substring(fileContents.LastIndexOf(nl) + 2);
                            }
                            //Debug.WriteLine(lastEntry);

                            // search the new data for the last existing line
                            searchResultIndex = response.LastIndexOf(lastEntry);
                        }

                        // if the downloaded data contains the last record on file, append the new data
                        if (searchResultIndex != -1)
                        {
                            startIndex = searchResultIndex + lastEntry.Length;
                            if (startIndex < response.Length - 30)
                            {
                                File.AppendAllText(path, response.Substring(startIndex + 2));
                            }
                        }
                        // else append all the data
                        else
                        {
                            Console.Write("\nThe last entry of the existing data was not found\nin the downloaded data. Appending all data...");
                            //Debug.WriteLine("The last entry of the existing data was not found\nin the downloaded data. Appending all data.");
                            File.AppendAllText(path, response.Substring(33)); // the index of 32 removes the file header
                        }
                    }
                    // if there is no file for this module, create the first one
                    catch (FileNotFoundException e)
                    {
                        // write data to file
                        Console.Write("file does not exist, creating new file...");
                        File.WriteAllText(path, response);
                        //Debug.WriteLine(response);
                    }

                }

                //Console.WriteLine("Energy file " + (i + 1) + " finished.");
                Console.WriteLine("finished.");
                //Debug.WriteLine("File " + (i + 1) + " finished.");
                i++;
            }

            Console.WriteLine("\nEnergy data finished!\n");
            //Debug.WriteLine("\nFinished\n");
            //Console.ReadKey();


            Console.Write("\nAll data finished! Exiting");

            /*  Wait for 5 seconds so the user can see the downloads finished   */
            Stopwatch sw1 = new Stopwatch(); // sw cotructor
            sw1.Start(); // starts the stopwatch
            int count = 0;
            for (int z = 0; ; z++)
            {
                if (z % 100000 == 0) // if in 100000th iteration (could be any other large number
                // depending on how often you want the time to be checked) 
                {
                    sw1.Stop(); // stop the time measurement
                    if (sw1.ElapsedMilliseconds > 1000) // check if desired period of time has elapsed
                    {
                        if (count < 4)
                        {
                            Console.Write(".");
                            count++;
                            sw1.Reset();
                            sw1.Start();
                        }
                        else
                        {
                            break; // if more than 5000 milliseconds have passed, stop looping and return
                            // to the existing code
                        }
                    }
                    else
                    {
                        sw1.Start(); // if less than 5000 milliseconds have elapsed, continue looping
                        // and resume time measurement
                    }

                }
            }
        }
    }
}
using System;
using TimHanewichToolkit.ApolloArchives;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ApolloArchives;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.IO;

namespace Apollo_Archives_DB_Downloader
{
    class Program
    {
        static void Main(string[] args)
        {
            GenerateDatabaseAsync("C:\\Users\\TaHan\\Downloads\\Apollo-Archives-DB-Downloader\\ArchiveOnlineReferences\\Apollo11.json", "C:\\Users\\TaHan\\Downloads\\A11-2").Wait();
        }

        public static async Task GenerateDatabaseAsync(string archive_online_reference_path, string db_folder_path)
        {
            //Deserialize aa
            Console.WriteLine("Deserializing online reference path...");
            string aa_content = await System.IO.File.ReadAllTextAsync(archive_online_reference_path);
            ApolloArchive aa = JsonConvert.DeserializeObject<ApolloArchive>(aa_content);
            
            //Does the db folder exist? if not, make it
            if (System.IO.Directory.Exists(db_folder_path) == false)
            {
                System.IO.Directory.CreateDirectory(db_folder_path);
            }

            //Make the folder inside of the folder for holding images
            string image_folder_path = db_folder_path + "\\Files";
            System.IO.Directory.CreateDirectory(image_folder_path);

            HttpClient hc = new HttpClient();

            //Make them one by one
            List<ApolloLogEntry> LogEntries = new List<ApolloLogEntry>();
            int t = 0; //For tracking progress
            foreach (ArchivedImage ai in aa.Images)
            {
                Console.WriteLine("Starting new log!");       

                //Basic info
                ApolloLogEntry ale = new ApolloLogEntry();
                ale.Title = ai.HtmlTitle;
                ale.Description = ai.Description;
                
                //Get each of the images, one by one
                List<string> this_entry_image_ids = new List<string>();
                foreach (AttachedImage attimg in ai.AttachedImages)
                {
                    
                    //Get the extension of this link
                    int loc_sla = attimg.LinkToImage.LastIndexOf("/");
                    int loc_per = attimg.LinkToImage.LastIndexOf(".");
                    if (loc_per > loc_sla)
                    {     
                        //Get the extension
                        string ext = attimg.LinkToImage.Substring(loc_per+1);
                        
                        if (ext.ToLower().Contains("htm") == false)
                        {
                            //Download the stream
                            Console.WriteLine("Downloading file from " + attimg.LinkToImage + "...");
                            HttpResponseMessage hrm = await hc.GetAsync(attimg.LinkToImage);
                            Stream s = await hrm.Content.ReadAsStreamAsync();

                            //Get a title for this
                            string this_img_title = Guid.NewGuid().ToString() + "." + ext;
                            string downloadpath = image_folder_path + "\\" + this_img_title;
                            Stream write_to = System.IO.File.Create(downloadpath);
                            s.CopyTo(write_to);
                            write_to.Dispose();
                            s.Dispose();

                            //Add it to the list of images for this entry
                            this_entry_image_ids.Add(this_img_title);
                        }
                    }                    
                }

                //Add those downloaded images to this
                ale.AssociatedFiles = this_entry_image_ids.ToArray();

                //Add it to the list
                LogEntries.Add(ale);
                t = t + 1;
                float proj = (float)t / (float)aa.Images.Length;
                Console.WriteLine(t.ToString("#,##0") + "/" + aa.Images.Length.ToString("#,##0") + " complete (" + proj.ToString("#0.0%") + ")");
            }


            //Now that we have all of the entries, serialize it and put it in the folder
            Console.WriteLine("Saving log file...");
            string to_save = JsonConvert.SerializeObject(LogEntries.ToArray());
            string to_save_path = db_folder_path + "\\Logs.json";
            System.IO.File.Create(to_save_path);
            await System.IO.File.WriteAllTextAsync(to_save_path, to_save);
            Console.WriteLine("File saved to " + to_save_path);

            Console.WriteLine("Complete!");
        }


    }
}

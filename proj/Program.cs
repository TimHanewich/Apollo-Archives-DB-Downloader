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
            GenerateDatabaseAsync("C:\\Users\\TaHan\\Downloads\\Apollo-Archives-DB-Downloader\\ArchiveOnlineReferences\\Apollo11.json", "C:\\Users\\TaHan\\Downloads\\A11").Wait();
        }

        public static async Task GenerateDatabaseAsync(string archive_online_reference_path, string db_folder_path)
        {
            //Deserialize aa
            Console.WriteLine("Deserializing online reference path...");
            string aa_content = await System.IO.File.ReadAllTextAsync(archive_online_reference_path);
            ApolloArchive aa = JsonConvert.DeserializeObject<ApolloArchive>(aa_content);

            //Make the folder inside of the folder for holding images
            string image_folder_path = db_folder_path + "\\Images";
            System.IO.Directory.CreateDirectory(image_folder_path);

            HttpClient hc = new HttpClient();

            //Make them one by one
            List<ApolloLogEntry> LogEntries = new List<ApolloLogEntry>();
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
                    //Download the stream
                    Console.WriteLine("Downloading image from " + attimg.LinkToImage + "...");
                    HttpResponseMessage hrm = await hc.GetAsync(attimg.LinkToImage);
                    Stream s = await hrm.Content.ReadAsStreamAsync();

                    //Get a title for this
                    string this_img_title = Guid.NewGuid().ToString() + ".jpg";
                    string downloadpath = image_folder_path + "\\" + this_img_title;
                    Stream write_to = System.IO.File.Create(downloadpath);
                    s.CopyTo(write_to);
                    write_to.Dispose();
                    s.Dispose();

                    //Add it to the list of images for this entry
                    this_entry_image_ids.Add(this_img_title);
                }

                //Add those downloaded images to this
                ale.AssociatedImages =this_entry_image_ids.ToArray();

                //Add it to the list
                LogEntries.Add(ale);
            }


            //Now that we have all of the entries, serialize it and put it in the folder
            Console.WriteLine("Saving log file...");
            string to_save = JsonConvert.SerializeObject(LogEntries.ToArray());
            await System.IO.File.WriteAllTextAsync(archive_online_reference_path + "\\Logs.json", to_save);

            Console.WriteLine("Complete!");
        }


    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagManager
{
    static class Utilities
    {


        //adapted source: https://stackoverflow.com/questions/14488796/does-net-provide-an-easy-way-convert-bytes-to-kb-mb-gb-etc#14488941
        /// <summary>
        /// Function to convert filesize to string with size suffix
        /// </summary>
        /// <param name="value">filesize to be converted to string</param>
        /// <returns></returns>
        public static string SizeSuffix(Int64 value)
        {
            string[] SizeSuffixes = { "B", "KB" ,"MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            if (value < 0) { return "-" + SizeSuffix(-value); }

            int i = 0;
            decimal dValue = (decimal)value;
            while (Math.Round(dValue / 1024) >= 1 && i < SizeSuffixes.Length -1 )
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n1} {1}", dValue, SizeSuffixes[i]);
        }

        /// <summary>
        /// Loads Tags from a TagDB.json file located at DBpath
        /// </summary>
        /// <param name="DBpath">location of the TagDB.json file</param>
        /// <returns></returns>
        public static Dictionary<string, List<string>> loadTags(string DBpath)
        {
            string inText;
            var initialDB = new Dictionary<string, List<string>>();
            initialDB.Add("<Tags>", new List<string> { "System", "Creativity", "Done", "TODO" }); //Supply a couple of tags to start with


            if (!File.Exists(DBpath + @"\TagDB.json")) return initialDB; //return empty DB if json file doesn't exist. saveTags takes care of creation.

            using (var inFile = new StreamReader(DBpath+@"\TagDB.json", Encoding.Default, true)) //TODO: Handle reading permissions
            {
                inText = inFile.ReadToEnd();
            }


            return JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(inText);

        }
        /// <summary>
        /// Saves tags to TagDB.json at DBpath location
        /// </summary>
        /// <param name="TagDB">The tag database to be saved</param>
        /// <param name="DBpath">Directory to save TagDB in</param>
        public static void saveTags(Dictionary<string, List<string>> TagDB, string DBpath)
        {

            string jsonOut = JsonConvert.SerializeObject(TagDB);

            using (StreamWriter outFile = new StreamWriter(DBpath+@"\TagDB.json")) //TODO: Handle writing permissions
            {
                outFile.Write(jsonOut);
            }

        }
        

    }





}

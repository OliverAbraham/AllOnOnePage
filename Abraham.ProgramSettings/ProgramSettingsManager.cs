using Hjson;
using Newtonsoft.Json; // IMPORTANT: Newtonsoft.Json didn't work on 7 inch tablet!
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.IO;
using System.Collections.Generic;

namespace Abraham.ProgramSettings
{
    /// <summary>
    /// Load and save program settings with your own class, using JSON or HJSON format
    /// </summary>
    /// <typeparam name="T">your class containing your data</typeparam>
	public class ProgramSettingsManager<T> where T : class
    {
        #region ------------- Properties ----------------------------------------------------------
        public string FileName { get; set; }
        #endregion



        #region ------------- Fields --------------------------------------------------------------
        private string _filePath;
        private bool _UseDotNetJsonSerializer;
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        /// <summary>
        /// use hjson extension so use the Hjson format, otherwise json will be used.
        /// </summary>
        public ProgramSettingsManager(string fileName = "appsettings.hjson")
        {
            FileName = fileName;
            _filePath = FileName;
        }
        #endregion



        #region ------------- Methods -------------------------------------------------------------
        /// <summary>
        /// Call this method to set the appdata folder as storage, default is the current folder
        /// </summary>
        public ProgramSettingsManager<T> UseAppDataFolder()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _filePath = Path.Combine(appDataFolder, FileName);
            return this;
        }

        /// <summary>
        /// Call this method to set a special folder as storage
        /// </summary>
        public ProgramSettingsManager<T> UseFolder(string folder)
        {
            _filePath = Path.Combine(folder, FileName);
            return this;
        }

        /// <summary>
        /// Call this method to use the Dotnet JSON serializer instead of NewtonsoftJson
        /// </summary>
        public ProgramSettingsManager<T> UseDotNetJsonSerializer()
        {
            _UseDotNetJsonSerializer = true;
            return this;
        }

        public T Load()
        {
            if (!File.Exists(_filePath))
                return null;

            string jsonString;
            if (Path.GetExtension(_filePath) == ".hjson")
                jsonString = HjsonValue.Load(_filePath).ToString();
            else
                jsonString = File.ReadAllText(_filePath);

            if (_UseDotNetJsonSerializer)
                return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString);
            else
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonString);
        }

        public void Save(T data)
        {
            string json;
            if (_UseDotNetJsonSerializer)
                json = System.Text.Json.JsonSerializer.Serialize<T>(data);
            else
                json = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            HjsonValue.Save(json, _filePath, new HjsonOptions(){EmitRootBraces = false });

            var Temp = File.ReadAllText(_filePath);
            Temp = Temp.Trim('\'');
            File.WriteAllText(_filePath, Temp);
        }
        #endregion
    }
}

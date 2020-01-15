using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Plugin.Media;
using XamarinCognitive.Models;

namespace TextExtractor
{
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        //Αρχικοποιηση του κλειδιου και του endpoint για την επικοινωνια με το API
        private static readonly string key = "04e9282d44eb4078bec62c55a515a449";
        private static readonly string endpoint = "https://visiontextextractor.cognitiveservices.azure.com/";

        public MainPage()
        {
            InitializeComponent();
        }
        //Δημιουργια των λειτουργιων για το κουμπι της επιλογης φωτογραφιας απο τον χρηστη
        async private void PickButton(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();
            try
            {
                var file = await CrossMedia.Current.PickPhotoAsync(new Plugin.Media.Abstractions.PickMediaOptions
                {
                    PhotoSize = Plugin.Media.Abstractions.PhotoSize.Medium
                });
                if (file == null) return;
                imgSelected.Source = ImageSource.FromStream(() =>
                {
                    var stream = file.GetStream();
                    return stream;
                });
                MakeRequest(file.Path);
            }
            catch (Exception ex)
            {
                string test = ex.Message;
            }
        }
        //Δημιουργια των λειτουργιων για το κουμπι της λήψης φωτογραφιας απο τον χρηστη
        async private void TakePhotoButton(object sender, EventArgs e)
        {
            var photo = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions()
            {
                PhotoSize = Plugin.Media.Abstractions.PhotoSize.Medium
            });

            if (photo != null)
            {


                imgSelected.Source = ImageSource.FromStream(() =>
                    {

                        var camerasStream = photo.GetStream();
                        return camerasStream;
                    }
                );
            }
            else
            {

            }

            MakeRequest(photo.Path);
        }


        //Δημιουργια συναρτησης για την επικοινωνια με το API 
        public async void MakeRequest(string text)
        {
            var errors = new List<string>();
            string extractedResult = "";
            ImageInfoViewModel responeData = new ImageInfoViewModel();

            try
            {
                HttpClient client = new HttpClient();

                // Header αιτηματος  
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

                // Παραμετροι αιτηματος. 
                string requestParameters = "language=unk&detectOrientation=true";

                // Δημιουργια του URI για την επικοινωνια με το API  
                string uri = endpoint + "vision/v2.0/ocr?" + requestParameters;

                HttpResponseMessage response;

                // Μετατροπη της εικονας σε bytes  
                byte[] byteData = GetImageAsByteArray(text);

                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response = await client.PostAsync(uri, content);


                }
                // Ληψη της απαντησης και μετατροπη σε string
                string result = await response.Content.ReadAsStringAsync();
                // Ελεγχος για την ορθοτητα της απαντησης
                if (response.IsSuccessStatusCode)
                {
                    responeData = JsonConvert.DeserializeObject<ImageInfoViewModel>(result, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Include,
                            Error = delegate(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs earg)
                            {
                                errors.Add(earg.ErrorContext.Member.ToString());
                                earg.ErrorContext.Handled = true;
                            }
                        }
                    );

                    var linesCount = responeData.regions[0].lines.Count;
                    for (int i = 0; i < linesCount; i++)
                    {
                        var wordsCount = responeData.regions[0].lines[i].words.Count;
                        for (int j = 0; j < wordsCount; j++)
                        {
                            //Δημιουργια ενος string για την εμφανιση των αποτελεσματων  
                            extractedResult += responeData.regions[0].lines[i].words[j].text + " ";
                        }

                        extractedResult += Environment.NewLine;
                    }


                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message);
            }
            //Εμφανιση των αποτελεσματων στο Label
            showText.Text = extractedResult;
            showText.TextColor = Color.Red;
        }

        //Δημιουργια συναρτησης για την μετατροπη της φωτογραφιας σε bytes
        public byte[] GetImageAsByteArray(string imageFilePath)
        {
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

    }

}



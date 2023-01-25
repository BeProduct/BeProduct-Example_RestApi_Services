using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OAuth2.Helpers;
using RestSharp;

namespace OAuth2
{
    class Program
    {
        private const string AuthUrl = "https://id.winks.io/ids";
        private const string ClientId = "#CLIENTID#";
        private const string ClientSecret = "#CLIENTSECRET#";
        private const string CompanyName = "#CompanyName#";
        private const string CallbackUrl = "#CALLBACKURL#";
        private const string RefreshToken = "#REFRESH_TOKEN#";
        private static string _accessToken = "";

        static void Main(string[] args)
        {
            GetStyle();
            Console.ReadLine();
        }

        private static void GetStyle()
        {
            // Nuget Packages:
            // RestSharp (http://restsharp.org/)
            // Newtonsoft.Json (http://json.codeplex.com/)
            
            Console.WriteLine("Loading Style API...");
            Console.WriteLine("Getting access token ...");
            _accessToken = Auth.RefreshAccessToken(AuthUrl, ClientId, ClientSecret, RefreshToken);

            if (string.IsNullOrEmpty(_accessToken))
            {
                Console.WriteLine("Failed to get access token");
                Console.WriteLine("Press any key...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Listing Style folders ...");
                var client = new RestClient("https://developers.beproduct.com/");
                
                var listStyleFolder = ListAllStyleFolder(client);
                var theFirstStyleFolder = ListStyleFromFirstFolder(listStyleFolder, client);
                var listStylePages = ListStylePages(theFirstStyleFolder, client);
                
                string headerId = theFirstStyleFolder["result"][0]["id"];
                var theFirstStylePage = GetFirstStylePage(headerId, listStylePages, client);

                const string folderId = "[ENTER_FOLDER_ID]";
                
                //Get Schema
                var styleSchema = GetStyleSchema(folderId, client);
                
                const string filters = "{'filters': [{'field': 'header_number', 'operator': 'eq', 'value': '[ENTER_STYLE_NUMBER]'}]}";
                var searchStyle = SearchStyle(folderId, filters , client);

                if (searchStyle != null)
                {
                    
                    //Get Attributes
                    string styleId = searchStyle["result"][0]["id"];
                    JArray jsonHeader = JArray.Parse(JsonConvert.SerializeObject(searchStyle["result"][0]["headerData"]["fields"]));
                    var styleNumber = jsonHeader.Where(i => (string)i["id"]! == "header_number").Select(i => (string)i["value"]!).FirstOrDefault();
                    var styleName = jsonHeader.Where(i => (string)i["id"]! == "header_name").Select(i => (string)i["value"]!).FirstOrDefault();
                    var division = jsonHeader.Where(i => (string)i["id"]! == "division").Select(i => (string)i["value"]!).FirstOrDefault();
                    var seasonYear = jsonHeader.Where(i => (string)i["id"]! == "season_year").Select(i => (string)i["value"]!).FirstOrDefault(); 

                    //Get Sizes
                    var sizeName = searchStyle["result"][0]["sizeClasses"][0]["name"];
                    JArray jsonSize = JArray.Parse(JsonConvert.SerializeObject(searchStyle["result"][0]["sizeClasses"][0]["sizeRange"]));
                    var sampleSize = jsonSize.Where(i => (bool)i["isSampleSize"]! == true).Select(i => (string)i["name"]!).FirstOrDefault();
                    var sizeRange = string.Join(", ", jsonSize.ToList().Select(i => (string)i["name"]!).ToList());
                    
                    
                    //Get Colorways
                    JArray jsonColor = JArray.Parse(JsonConvert.SerializeObject(searchStyle["result"][0]["colorways"]));
                    foreach (var color in jsonColor.Where(i => (bool)i["hideColorway"]! == false))
                    {
                        var colorNumber = color["colorNumber"];
                        var colorName = color["colorName"];
                        var colorHex = color["primaryColor"];
                    }

                    //Get BoM
                    const string pageBomId = "ENTER_PAGE_ID";
                    var styleBom = GetStylePage(pageBomId, styleId, client);
                    
                    //Update Style
                    const string fields = "{'fields': [{'id': 'header_name','value': 'Test API Update Style Name'}]}";
                    var updateStyle = UpdateStyle(styleId, fields, client);
                    
                }
                
                Console.WriteLine("That's All Folks!");
                Console.WriteLine("Press any key...");
                Console.ReadKey();
            }
        }

        private static dynamic? ListStylePages(dynamic theFirstStyleFolder, RestClient client)
        {
            Console.WriteLine("List pages of the first style ...");
            string headerId = theFirstStyleFolder["result"][0]["id"];
            var request = new RestRequest("/api/" + CompanyName + "/Style/Pages?headerId=" + headerId, Method.Get);
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            var response = client.Execute<dynamic>(request);
            if (response.Content == null) return null;
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            Console.WriteLine(response.Content);
            return result;
        }
        
        private static dynamic? ListStyleFromFirstFolder(dynamic? allStyleFolder, RestClient client)
        {
            Console.WriteLine("List of styles from the first folder ...");
            if (allStyleFolder == null) return null;
            string folderId = allStyleFolder[0]["id"];
            var request = new RestRequest(
                $"/api/{CompanyName}/Style/Headers?folderId={folderId}&pageSize=10&pageNumber=0",
                Method.Post);
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new {filters = Array.Empty<object>() });
            var response = client.Execute<dynamic>(request);
            if (response.Content == null) return null;
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            Console.WriteLine(response.Content);
            return result;
        }
        
        private static dynamic? GetStylePage(string pageId, string headerId, RestClient client)
        {
            Console.WriteLine("Get style page ...");
            var request = new RestRequest(
                "/api/" + CompanyName + "/Style/Page?headerId=" + headerId + "&pageId=" + pageId, Method.Get);
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            var response = client.Execute<dynamic>(request);
            if (response.Content == null) return null;
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            Console.WriteLine(response.Content);
            return result?.data;
        }        
        
        private static dynamic? GetFirstStylePage(string headerId, dynamic? listStylePages, RestClient client)
        {
            Console.WriteLine("Get the first style page ...");
            var request = new RestRequest(
                "/api/" + CompanyName + "/Style/Page?headerId=" + headerId + "&pageId=" +
                ((IEnumerable<dynamic>) listStylePages).First(i => i["id"] != Guid.Empty.ToString())["id"], Method.Get);
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            var response = client.Execute<dynamic>(request);
            if (response.Content == null) return null;
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            Console.WriteLine(response.Content);
            return result;
        }
        
        private static dynamic? SearchStyle(string folderId, string filters, RestClient client)
        {
            Console.WriteLine("Search style folder...");
            var request = new RestRequest(
                $"/api/{CompanyName}/Style/Headers?folderId={folderId}&pageSize=10&pageNumber=0",
                Method.Post);
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(filters);
            var response = client.Execute<dynamic>(request);
            if (response.Content == null) return null;
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            Console.WriteLine(response.Content);
            return result;
        }
        
        private static dynamic? GetStyleSchema(string folderId, RestClient client)
        {
            Console.WriteLine("Get style schema ...");
            var request = new RestRequest(
                $"/api/{CompanyName}/Style/FolderSchema?folderId={folderId}");
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            var response = client.Execute<dynamic>(request);
            if (response.Content == null) return null;
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            Console.WriteLine(response.Content);
            return result;
        }

        private static dynamic? UpdateStyle(string headerId, string fields, RestClient client)
        {
            Console.WriteLine("Search style folder...");
            var request = new RestRequest(
                $"/api/{CompanyName}/Style/Header/{headerId}/Update",
                Method.Post);
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(fields);
            var response = client.Execute<dynamic>(request);
            if (response.Content == null) return null;
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            Console.WriteLine(response.Content);
            return result;
        }
        
        private static dynamic? ListAllStyleFolder(RestClient client)
        {
            Console.WriteLine("List of style folders ...");
            var request = new RestRequest("/api/" + CompanyName + "/Style/Folders", Method.Get);
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            var response = client.Execute<dynamic>(request);
            if (response.Content == null) return null;
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            Console.WriteLine(response.Content);
            return result;
        }
    }
}
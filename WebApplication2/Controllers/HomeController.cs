using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;

        //private readonly IConnectionMultiplexer _redisConnection;
        //private readonly IDatabase _redisCache;
        //private readonly IDistributedCache _distributedCache;
        //private readonly IConnectionMultiplexer _redis;

        public HomeController(IConfiguration configuration, ILogger<HomeController> logger, ICacheService cacheService)
        {
            _logger = logger;
            _configuration = configuration;
            _cacheService = cacheService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> RemoveMemoryCache(string url)
        {
            if (url != null)
            {
                string fileName = new Uri(url).Segments.Last();
                var tempstr = _cacheService.Get<Dictionary<int, Dictionary<string, string>>>(fileName);
                if (tempstr == null) ViewBag.Message = "Cache not exist";
                else
                {
                    _cacheService.Remove(fileName);
                    ViewBag.Message = "Cache is removed";
                }
            }
            else ViewBag.Message = "Select or Input URL";
            return View("Index");
        }

        public async Task<IActionResult> RemoveBlobStorage(string url)
        {
            if (url != null)
            {
                var doctemp = new HtmlWeb().Load(url);
                string fileName = new Uri(url).Segments.Last();

                // Retrieve the connection string and container name from the appsettings.json file
                string connectionString = _configuration.GetSection("AzureBlobStorage:ConnectionString").Value;
                string containerName = _configuration.GetSection("AzureBlobStorage:ContainerName").Value;

                // Create a BlobServiceClient object and get a reference to the container
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                if (await blobClient.ExistsAsync())
                {
                    blobClient.Delete();
                    _cacheService.Remove(fileName);
                    ViewBag.Message = "Deleted Blob Storage and Cache";
                }
                else ViewBag.Message = "The Blob Storage does not exist";
            }
            else ViewBag.Message = "Select or Input URL";
            return View("Index");
        }

        public async Task<IActionResult> UpdateBlob(string url)
        {
            if (url != null)
            {
                var doctemp = new HtmlWeb().Load(url);
                string fileName = new Uri(url).Segments.Last();

                // Retrieve the connection string and container name from the appsettings.json file
                string connectionString = _configuration.GetSection("AzureBlobStorage:ConnectionString").Value;
                string containerName = _configuration.GetSection("AzureBlobStorage:ContainerName").Value;

                // Create a BlobServiceClient object and get a reference to the container
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                if (await blobClient.ExistsAsync())
                {
                    blobClient.Delete();
                    _cacheService.Remove(fileName);
                    SaveBlob(url, blobClient);
                    ViewBag.Message = $"Updated Cache and Blob storage titled \"{fileName}\"";
                }
                else ViewBag.Message = "The Blob Storage does not exist";
            }
            else ViewBag.Message = "Select or Input URL";
            return View("Index");
        }

        public void SaveBlob(string url, BlobClient blobClient)
        {
            var doctemp = new HtmlWeb().Load(url);
            string fileName = new Uri(url).Segments.Last();

            //////////////////////////////////////////////////
            /// var doctemp = new HtmlWeb().Load(url);
            var linkData = new Dictionary<string, string>();
            var metapropertyTags = new Dictionary<string, string>();
            var metanameTags = doctemp.DocumentNode.Descendants("meta")
                .Where(n => n.Attributes["name"] != null)
                .ToDictionary(n => n.Attributes["name"].Value.ToLower(), n => n.Attributes["content"].Value);
            var propertytags = doctemp.DocumentNode.SelectNodes("//meta[@property]");
            string tempproperty = "";
            int i = 0;
            foreach (var propertyTag in propertytags)
            {
                // Access the attributes or inner HTML of the meta tag
                var property = propertyTag.Attributes["property"]?.Value;
                var content = propertyTag.Attributes["content"]?.Value;
                if (tempproperty != property)
                {
                    metapropertyTags[property] = content;
                    tempproperty = property;
                }
                else
                {
                    i++;
                    metapropertyTags[property + i] = content;
                }
            }

            foreach (HtmlNode link in doctemp.DocumentNode.SelectNodes("//link"))
            {
                var rel = link.GetAttributeValue("rel", "");
                var href = link.GetAttributeValue("href", "");

                if (!string.IsNullOrEmpty(rel) && !string.IsNullOrEmpty(href))
                {
                    linkData[rel] = href;
                }
            }
            string title = doctemp.DocumentNode.SelectSingleNode("//title").InnerText;

            using (var client = new HttpClient())
            {
                var response = client.GetAsync(url);
                BlobMetatag json_metaTags = new BlobMetatag();

                var scriptTag = doctemp.DocumentNode.SelectSingleNode("//script");
                var scriptTag_json = scriptTag.InnerHtml;
                var contentObject = JsonDocument.Parse(scriptTag_json).RootElement;
                var replaced_metanamedata = new Dictionary<string, string>() { };
                var replaced_metapropertydata = new Dictionary<string, string>() { };
                var scriptTag_metadata = new Dictionary<string, string>() { };
                var extracted_content_dict = new Dictionary<string, string>() { };
                Dictionary<string, string> title_dict = new Dictionary<string, string> { { "Title", title } };

                if (contentObject.TryGetProperty("headline", out JsonElement headlineValue))
                {
                    extracted_content_dict["publisher"] = contentObject.GetProperty("publisher").GetProperty("name").GetString();
                    extracted_content_dict["headline"] = contentObject.GetProperty("headline").GetString();
                    extracted_content_dict["datePublished"] = contentObject.GetProperty("datePublished").GetString();
                    extracted_content_dict["dateModified"] = contentObject.GetProperty("dateModified").GetString();
                    extracted_content_dict["entityType"] = contentObject.GetProperty("mainEntityOfPage").GetProperty("@type").GetString();
                    extracted_content_dict["description"] = contentObject.GetProperty("description").GetString();
                }

                replaced_metanamedata = json_metaTags.ReplaceCharacter(metanameTags);
                replaced_metapropertydata = json_metaTags.ReplaceCharacter(metapropertyTags);
                linkData = json_metaTags.ReplaceCharacter(linkData);
                replaced_metanamedata = title_dict.Concat(replaced_metanamedata).ToDictionary(x => x.Key, x => x.Value);
                json_metaTags = json_metaTags.InitFromDictionary(extracted_content_dict);
                //json += linkData.ToString();

                var blobUploadOptions = new BlobUploadOptions
                {
                    Metadata = replaced_metanamedata,
                };
                var final_data = new Dictionary<int, Dictionary<string, string>>()
                {
                    {1, replaced_metanamedata},
                    {2, replaced_metapropertydata},
                    {3, linkData},
                    //{4, extracted_content_dict}
                };
                string json = json_metaTags.ToJson(final_data);
                // Add data to the cache
                _cacheService.Set(fileName, final_data);
                //await _redisCache.StringSetAsync(fileName, json);

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    blobClient.UploadAsync(stream, blobUploadOptions);
                }

                ViewBag.MetaTags = replaced_metanamedata;
                ViewBag.MetaPropertyTags = replaced_metapropertydata;
                ViewBag.LinkTags = linkData;
                //ViewBag.ScriptTags = extracted_content_json;
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> UploadFromUrl(string url)
        {
            if (url == null)
            {
                ViewBag.Message = "Select or Input URL";
                return View("Index");
            }
            string fileName = new Uri(url).Segments.Last();
            // Get data from the cache
            var data = _cacheService.Get<Dictionary<int, Dictionary<string, string>>>(fileName);
            Dictionary<string, string> metanamedata, metapropertydata, metalinkdata, metascriptdata;
            //Dictionary<int, Dictionary<string, string>> rediswholedata = null;
            metanamedata = null;
            if (data != null)
            {
                metanamedata = data[1];
                metapropertydata = data[2];
                metalinkdata = data[3];
                //var metascriptdata = data[4];
            }
            // Retrieve the connection string and container name from the appsettings.json file
            string connectionString = _configuration.GetSection("AzureBlobStorage:ConnectionString").Value;
            string containerName = _configuration.GetSection("AzureBlobStorage:ContainerName").Value;

            // Create a BlobServiceClient object and get a reference to the container
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Create a BlobClient object and upload the file to the blob
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            if (metanamedata == null)
            {
                var doctemp = new HtmlWeb().Load(url);
                if (await blobClient.ExistsAsync())
                {
                    // Download the JSON data from Azure Blob Storage
                    Dictionary<int, Dictionary<string, string>> wholedata;
                    using (var stream = new MemoryStream())
                    {
                        await blobClient.DownloadToAsync(stream);
                        stream.Position = 0;

                        // Deserialize the JSON data to the head section
                        using (var reader = new StreamReader(stream))
                        {
                            var json = await reader.ReadToEndAsync();
                            wholedata = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<string, string>>>(json);
                            //await _redisCache.StringSetAsync(fileName, json);
                        }
                    }
                    BlobProperties properties = blobClient.GetProperties();
                    _cacheService.Set(fileName, wholedata);
                    ViewBag.MetaTags = wholedata[1];
                    ViewBag.MetaPropertyTags = wholedata[2];
                    ViewBag.LinkTags = wholedata[3];
                    //ViewBag.ScriptTags = JsonConvert.SerializeObject(wholedata[4].Values);
                    ViewBag.Message = "Blob with the same name already exists so saved at Cache";
                }
                else
                {
                    SaveBlob(url, blobClient);
                    ViewBag.Message = $"Saved at Cache and Blob storage titled \"{fileName}\"";
                }
            }
            else
            {
                ViewBag.MetaTags = data[1];
                ViewBag.MetaPropertyTags = data[2];
                ViewBag.LinkTags = data[3];
                //ViewBag.ScriptTags = rediswholedata[4];
                ViewBag.Message = "Loaded from Cache";
            }
            return View("Index");
        }


    }
}
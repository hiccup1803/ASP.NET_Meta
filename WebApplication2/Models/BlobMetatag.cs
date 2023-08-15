using System.Text.Json;

namespace WebApplication2.Models
{
    public class BlobMetatag
    {
        public string Publisher_name { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string DatePublished { get; set; } = string.Empty;
        public string DateModified { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;

        public BlobMetatag InitFromDictionary(Dictionary<string, string> dict)
        {
            if (dict.TryGetValue("description", out string description))
            {
                this.Description = description;
            }
            if (dict.TryGetValue("publisher", out string publisher_name))
            {
                this.Publisher_name = publisher_name;
            }
            if (dict.TryGetValue("datePublished", out string date_published))
            {
                this.DatePublished = date_published;
            }
            if (dict.TryGetValue("dateModified", out string date_modified))
            {
                this.DateModified = date_modified;
            }
            if (dict.TryGetValue("headline", out string headline))
            {
                this.Headline = headline;
            }
            if (dict.TryGetValue("entityType", out string entity_type))
            {
                this.EntityType = entity_type;
            }
            // Map more keys to properties as needed
            return this;
        }
        public Dictionary<string, string> ReplaceCharacter(Dictionary<string, string> metadata)
        {
            Dictionary<string, string> new_metadata = new Dictionary<string, string>();
            //new_metadata.Add("Title", metadata[Title]);
            foreach (var tag in metadata)
            {
                string tempmetakey = tag.Key;
                if (tempmetakey.Contains(":"))
                    tempmetakey = tempmetakey.Replace(":", "_");
                if (tempmetakey.Contains("-"))
                    tempmetakey = tempmetakey.Replace("-", "_");
                new_metadata.Add(tempmetakey, tag.Value);
            }
            return new_metadata;
        }

        public string ToJson(Dictionary<int, Dictionary<string, string>> data)
        {
            return JsonSerializer.Serialize(data);
        }
    }
}

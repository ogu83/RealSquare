using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FoursquareApi.Objects
{
    [DataContract]
    public class Meta
    {
        [DataMember]
        public int code { get; set; }
        [DataMember]
        public string errorType { get; set; }
        [DataMember]
        public string errorDetail { get; set; }
        [DataMember]
        public Response response { get; set; }
    }

    [DataContract]
    public class Response
    {
        [DataMember]
        public List<Groups> groups { get; set; }
    }

    [DataContract]
    public class Groups
    {
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public List<Nearby> items { get; set; }
    }

    [DataContract]
    public class Nearby
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public Contact contact { get; set; }
        [DataMember]
        public Location location { get; set; }
        [DataMember]
        public List<Category> categories { get; set; }
        [DataMember]
        public bool verified { get; set; }
        [DataMember]
        public Stat stats { get; set; }
        [DataMember]
        public HereNow hereNow { get; set; }
    }

    [DataContract]
    public class Contact
    {
        [DataMember]
        public string phone { get; set; }
        [DataMember]
        public string formattedPhone { get; set; }
    }

    [DataContract]
    public class Location
    {
        [DataMember]
        public double lat { get; set; }
        [DataMember]
        public double lng { get; set; }
        [DataMember]
        public int distance { get; set; }
        [DataMember]
        public string city { get; set; }
        [DataMember]
        public string address { get; set; }
    }

    [DataContract]
    public class Category
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string pluralName { get; set; }
        [DataMember]
        public string shortName { get; set; }
        [DataMember]
        public string icon { get; set; }
        [DataMember]
        public List<string> parents { get; set; }
        [DataMember]
        public bool primary { get; set; }
    }

    [DataContract]
    public class Stat
    {
        [DataMember]
        public int checkinsCount { get; set; }
        [DataMember]
        public int usersCount { get; set; }
        [DataMember]
        public int tipCount { get; set; }
    }

    [DataContract]
    public class HereNow
    {
        [DataMember]
        public int count { get; set; }
    }
}
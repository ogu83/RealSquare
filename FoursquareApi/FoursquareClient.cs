using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;

using FoursquareApi.Objects;

namespace FoursquareApi
{
    public class FoursquareClient
    {
        /// <summary>
        /// gets the foursquare application client id
        /// </summary>
        public string ClientId { get; private set; }
        /// <summary>
        /// gets the foursquare application client secret 
        /// </summary>
        public string ClientSecret { get; private set; }

        public delegate void GetNearbyVenuesCompletedEventHandler(List<Nearby> nearbys);
        public event GetNearbyVenuesCompletedEventHandler GetNearbyVenuesCompleted;

        public event EventHandler GetNearbyVenuesFailed;

        /// <summary>
        /// constructor of Foursquare application client 
        /// </summary>
        /// <param name="_clientId">foursquare application client id</param>
        /// <param name="_clientSecret">foursquare application client secret</param>
        public FoursquareClient(string _clientId, string _clientSecret)
        {
            ClientId = _clientId;
            ClientSecret = _clientSecret;
        }

        /// <summary>
        /// Gets the nearby locations from foursquare api asyncrously
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lng">Longtitude</param>
        /// <param name="radius">Search Radius in meters</param>
        public void GetNearbyVenuesAsync(double lat, double lng, int radius)
        {
            StringBuilder urlBuilder = new StringBuilder("https://api.foursquare.com/v2/venues/search?");
            urlBuilder.Append("ll=" + lat.ToString().Replace(",", ".") + "," + lng.ToString().Replace(",", "."));
            urlBuilder.Append(@"&radius=" + radius.ToString());
            urlBuilder.Append("&client_id=" + ClientId);
            urlBuilder.Append("&client_secret=" + ClientSecret);

            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(urlBuilder.ToString());
            webRequest.BeginGetResponse(new AsyncCallback(FinishGetNearbyRequest), webRequest);
        }

        private void FinishGetNearbyRequest(IAsyncResult result)
        {
            string content = string.Empty;
            //List<Nearby> nearbys = new List<Nearby>();
            Meta m = new Meta();

            try
            {
                HttpWebResponse response = (result.AsyncState as HttpWebRequest).EndGetResponse(result) as HttpWebResponse;
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    content = sr.ReadToEnd();
                }

                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(content)))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(m.GetType());
                    m = ser.ReadObject(ms) as Meta;
                }

                Deployment.Current.Dispatcher.BeginInvoke(delegate()
                {
                    if (GetNearbyVenuesCompleted != null)
                        GetNearbyVenuesCompleted(m.response.groups[0].items);
                });
            }
            catch (Exception ex)
            {
                Deployment.Current.Dispatcher.BeginInvoke(delegate()
                {
                    if (GetNearbyVenuesFailed != null)
                        GetNearbyVenuesFailed(this, null);
                });
            }
        }
    }
}
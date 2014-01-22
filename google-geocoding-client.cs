using System;
using System.Net;
using System.Runtime.Serialization;
using System.Xml;

#region "Data Contracts"

namespace Geocoding.Entities
{
	[DataContract]
	public class Response
	{
		[DataMember(Name = "results")]
		public Result[] Results { get; set; }

		[DataMember(Name = "status")]
		public string Status { get; set; }
	}

	[DataContract]
	public class Result
	{
		[DataMember(Name = "address_components")]
		public AddressComponent[] AddressComponents { get; set; }

		[DataMember(Name = "formatted_address")]
		public string FormattedAddress { get; set; }

		[DataMember(Name = "geometry")]
		public Geometry Geometry { get; set; }

		[DataMember(Name = "types")]
		public string[] Types { get; set; }
	}

	[DataContract]
	public class AddressComponent
	{
		[DataMember(Name = "long_name")]
		public string LongName { get; set; }

		[DataMember(Name = "short_name")]
		public string ShortName { get; set; }

		[DataMember(Name = "types")]
		public string[] Types { get; set; }
	}

	[DataContract]
	public class Geometry
	{
		[DataMember(Name = "location")]
		public Location Location { get; set; }

		[DataMember(Name = "location_type")]
		public string LocationType { get; set; }

		[DataMember(Name = "viewport")]
		public Viewport Viewport { get; set; }
	}

	[DataContract]
	public class Location
	{
		[DataMember(Name = "lat")]
		public double Latitude { get; set; }

		[DataMember(Name = "lng")]
		public double Longitude { get; set; }
	}

	[DataContract]
	public class Viewport
	{
		[DataMember(Name = "northeast")]
		public Location NorthEast { get; set; }

		[DataMember(Name = "southwest")]
		public Location SouthWest { get; set; }
	}
}

#endregion

namespace Geocoding
{
	using Geocoding.Entities;
	using System.Runtime.Serialization.Json;

	class Boot
	{
		private static void showWeatherInfo(string woeId, string temperatureUnit = "c")
		{
			UriBuilder yahooWeatherUriBuilder = new UriBuilder("http://weather.yahooapis.com/forecastrss");
			yahooWeatherUriBuilder.Query = string.Format("w={0}&u={1}", woeId,
				(temperatureUnit.Equals("c") || temperatureUnit.Equals("f")) ? temperatureUnit : "c"
			);

			// Inject proxy credentials for transport through secured network (if any)
			WebRequest.DefaultWebProxy = WebRequest.GetSystemWebProxy();
			WebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;

			string yahooWeatherUrl = yahooWeatherUriBuilder.Uri.AbsoluteUri;

			using (XmlReader yahooWeatherXmlReader = new XmlTextReader(yahooWeatherUrl))
			{
				XmlDocument yahooWeatherXmlDocument = new XmlDocument();
				yahooWeatherXmlDocument.Load(yahooWeatherXmlReader);

				// Set up namespace manager for XPath
				XmlNamespaceManager yahooWeatherXmlNamespaceManager = new XmlNamespaceManager(yahooWeatherXmlDocument.NameTable);
				yahooWeatherXmlNamespaceManager.AddNamespace("yweather", "http://xml.weather.yahoo.com/ns/rss/1.0");

				XmlNode yahooWeatherLocationNode = yahooWeatherXmlDocument.SelectSingleNode("/rss/channel/yweather:location", yahooWeatherXmlNamespaceManager);
				XmlNode yahooWeatherUnitsNode = yahooWeatherXmlDocument.SelectSingleNode("/rss/channel/yweather:units", yahooWeatherXmlNamespaceManager);
				XmlNode yahooWeatherWindNode = yahooWeatherXmlDocument.SelectSingleNode("/rss/channel/yweather:wind", yahooWeatherXmlNamespaceManager);
				XmlNode yahooWeatherAtmosphereNode = yahooWeatherXmlDocument.SelectSingleNode("/rss/channel/yweather:atmosphere", yahooWeatherXmlNamespaceManager);
				XmlNode yahooWeatherAstronomyNode = yahooWeatherXmlDocument.SelectSingleNode("/rss/channel/yweather:astronomy", yahooWeatherXmlNamespaceManager);
				XmlNodeList yahooWeatherForecaseNodes = yahooWeatherXmlDocument.SelectNodes("/rss/channel/item/yweather:forecast", yahooWeatherXmlNamespaceManager);

				Console.WriteLine("Location: [city: {0}], [region: {1}], [country: {2}]",
					yahooWeatherLocationNode.Attributes["city"].InnerText,
					yahooWeatherLocationNode.Attributes["region"].InnerText,
					yahooWeatherLocationNode.Attributes["country"].InnerText
				);

				Console.WriteLine("Wind: [chill: {0}], [direction: {1}], [speed: {2}]",
					yahooWeatherWindNode.Attributes["chill"].InnerText,
					yahooWeatherWindNode.Attributes["direction"].InnerText,
					yahooWeatherWindNode.Attributes["speed"].InnerText + yahooWeatherUnitsNode.Attributes["speed"].InnerText
				);

				Console.WriteLine("Atmosphere: [humidity: {0}], [visibility: {1}], [pressure: {2}]",
					yahooWeatherAtmosphereNode.Attributes["humidity"].InnerText,
					yahooWeatherAtmosphereNode.Attributes["visibility"].InnerText + yahooWeatherUnitsNode.Attributes["distance"].InnerText,
					yahooWeatherAtmosphereNode.Attributes["pressure"].InnerText + yahooWeatherUnitsNode.Attributes["pressure"].InnerText
				);

				Console.WriteLine("Astronomy: [sunrise: {0}], [sunset: {1}]",
					yahooWeatherAstronomyNode.Attributes["sunrise"].InnerText,
					yahooWeatherAstronomyNode.Attributes["sunset"].InnerText
				);

				foreach (XmlNode yahooWeatherForecaseNode in yahooWeatherForecaseNodes)
				{
					Console.WriteLine("Forecast for [{0}]: [date: {1}], [low: {2}], [high: {3}], [text: {4}]",
						yahooWeatherForecaseNode.Attributes["day"].InnerText,
						yahooWeatherForecaseNode.Attributes["date"].InnerText,
						yahooWeatherForecaseNode.Attributes["low"].InnerText,
						yahooWeatherForecaseNode.Attributes["high"].InnerText,
						yahooWeatherForecaseNode.Attributes["text"].InnerText
					);
				}
			}
		}

		public static void Main(string[] args)
		{
			const string googleGeocodingBaseUrl = "http://maps.googleapis.com/maps/api/geocode/json";
			string address = string.Empty;

			Console.Write("Address? ");
			address = Console.ReadLine();

			if (string.IsNullOrWhiteSpace(address))
			{
				return;
			}

			UriBuilder googleGeocodingUriBuilder = new UriBuilder(googleGeocodingBaseUrl);
			googleGeocodingUriBuilder.Query = string.Format("address={0}&sensor=false", Uri.EscapeUriString(address));

			// Inject proxy credentials for transport through secured network (if any)
			WebRequest.DefaultWebProxy = WebRequest.GetSystemWebProxy();
			WebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;

			HttpWebRequest googleGeocodingRequest = WebRequest.Create(googleGeocodingUriBuilder.Uri) as HttpWebRequest;

			using (HttpWebResponse googleGeocodingResponse = googleGeocodingRequest.GetResponse() as HttpWebResponse)
			{
				if (googleGeocodingResponse.StatusCode == HttpStatusCode.OK)
				{
					DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Response));
					Response googleGeocodingJsonResponse = jsonSerializer.ReadObject(googleGeocodingResponse.GetResponseStream()) as Response;

					foreach (Result result in googleGeocodingJsonResponse.Results)
					{
						foreach (AddressComponent addrComponent in result.AddressComponents)
						{
							Console.WriteLine("Long Name: {0}, ShortName: {1}, Types: {2}", addrComponent.LongName, addrComponent.ShortName, string.Join(",", addrComponent.Types));
						}

						Console.WriteLine("Formatted Address: {0}", result.FormattedAddress);
						Console.WriteLine("Geometry: [Location: [Latitude: {0}], [Longitude: {1}]], [Location Type: {2}], [Viewport: [North East: [Latitude: {3}], [Longitude: {4}]], [South West: [Latitude: {5}], [Longitude: {6}]]]",
							result.Geometry.Location.Latitude,
							result.Geometry.Location.Longitude,
							result.Geometry.LocationType,
							result.Geometry.Viewport.NorthEast.Latitude,
							result.Geometry.Viewport.NorthEast.Longitude,
							result.Geometry.Viewport.SouthWest.Latitude,
							result.Geometry.Viewport.SouthWest.Longitude
						);
					}
					
					Console.WriteLine(googleGeocodingJsonResponse.Status);
				}
			}

			Console.ReadKey(false);
		}
	}
}

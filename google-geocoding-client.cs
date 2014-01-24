using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;


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

	/// <summary>
	/// REF: https://developers.google.com/maps/documentation/javascript/geocoding?hl=en
	/// </summary>
	[DataContract]
	public enum GeocodingLocationType
	{
		[EnumMember(Value = "street_address")]
		StreetAddress, // indicates a precise street address

		[EnumMember(Value = "
	}
}

#endregion

namespace Geocoding
{
	using Geocoding.Entities;

	class Boot
	{
		public static void Main(string[] args)
		{
			const string googleGeocodingBaseUrl = "http://maps.googleapis.com/maps/api/geocode/json";
			const string googleStaticMapsBaseUrl = "http://maps.googleapis.com/maps/api/staticmap";
			string paramAddress = string.Empty;

			Console.Write("Address? ");
			paramAddress = Console.ReadLine();

			if (string.IsNullOrWhiteSpace(paramAddress))
			{
				return;
			}

			UriBuilder googleGeocodingUriBuilder = new UriBuilder(googleGeocodingBaseUrl);
			googleGeocodingUriBuilder.Query = string.Format("address={0}&sensor=false", Uri.EscapeUriString(paramAddress));

			// Inject proxy credentials for transport through secured network (if any)
			WebRequest.DefaultWebProxy = WebRequest.GetSystemWebProxy();
			WebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;

			Response googleGeocodingJsonResponse = null;
			HttpWebRequest googleGeocodingRequest = WebRequest.Create(googleGeocodingUriBuilder.Uri) as HttpWebRequest;

			using (HttpWebResponse googleGeocodingResponse = googleGeocodingRequest.GetResponse() as HttpWebResponse)
			{
				if (googleGeocodingResponse.StatusCode != HttpStatusCode.OK)
				{
					return;
				}

				DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Response));
				googleGeocodingJsonResponse = jsonSerializer.ReadObject(googleGeocodingResponse.GetResponseStream()) as Response;

				if (googleGeocodingResponse == null)
				{
					return;
				}

				foreach (Result result in googleGeocodingJsonResponse.Results)
				{
					foreach (AddressComponent addrComponent in result.AddressComponents)
					{
						Console.WriteLine("Long Name: {0}, ShortName: {1}, Types: {2}", addrComponent.LongName, addrComponent.ShortName, string.Join(", ", addrComponent.Types));
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

				Console.WriteLine("Status: {0}", googleGeocodingJsonResponse.Status);
			}

			if (googleGeocodingJsonResponse.Results.Length > 0)
			{
				Result googleGeocodingResult = googleGeocodingJsonResponse.Results.First();
				string paramCenter = googleGeocodingResult.FormattedAddress;
				int paramZoom = 14;
				string paramMapType = "roadmap";
				string paramSize = "500x500";

				MD5 md5Hash = MD5.Create();
				var pngFilenameBytes = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(paramCenter));
				StringBuilder hashBuilder = new StringBuilder();

				foreach (byte b in pngFilenameBytes)
				{
					hashBuilder.Append(b.ToString("x2"));
				}

				string paramPngFilename = string.Format("{0}-{1}-{2}.png", hashBuilder.ToString(), paramZoom, paramSize);

				UriBuilder googleStaticMapsUriBuilder = new UriBuilder(googleStaticMapsBaseUrl);
				googleStaticMapsUriBuilder.Query = string.Format("center={0}&zoom={1}&size={2}&maptype={3}&sensor=false", Uri.EscapeUriString(paramCenter), paramZoom, Uri.EscapeUriString(paramSize), paramMapType);

				HttpWebRequest googleStaticMapsRequest = WebRequest.Create(googleStaticMapsUriBuilder.Uri) as HttpWebRequest;

				using (HttpWebResponse googleStaticMapsResponse = googleStaticMapsRequest.GetResponse() as HttpWebResponse)
				{
					if (googleStaticMapsResponse.StatusCode == HttpStatusCode.OK)
					{
						using (BufferedStream googleStaticMapsResponseBufferedStream = new BufferedStream(googleStaticMapsResponse.GetResponseStream()))
						{
							if (googleStaticMapsResponseBufferedStream.CanRead)
							{
								PngBitmapDecoder googleStaticMapsImageDecoder = new PngBitmapDecoder(googleStaticMapsResponseBufferedStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
								BitmapSource googleStaticMapsImageBitmapSource = googleStaticMapsImageDecoder.Frames[0];

								if (File.Exists(paramPngFilename))
								{
									File.Delete(paramPngFilename);
								}

								using (FileStream googleStaticMapsPngStream = new FileStream(paramPngFilename, FileMode.CreateNew))
								{
									PngBitmapEncoder encoder = new PngBitmapEncoder();
									encoder.Interlace = PngInterlaceOption.On;
									encoder.Frames.Add(BitmapFrame.Create(googleStaticMapsImageBitmapSource));
									encoder.Save(googleStaticMapsPngStream);
								}
							}
						}
					}
				}
			}

			Console.ReadKey(false);
		}
	}
}

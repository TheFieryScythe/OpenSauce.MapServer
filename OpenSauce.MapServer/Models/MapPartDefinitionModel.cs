using System.Xml.Serialization;

namespace OpenSauce.MapServer.Models
{
	[XmlRoot("osHTTPServer")]
	public sealed class MapPartDefinitionModel
	{
		[XmlElement("map_download")]
		public MapDownloadModel MapDownload { get; set; }
	}
}
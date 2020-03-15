namespace OpenSauce.MapServer.Lib.Models
{
	public sealed class MapDownloadParameters
	{
		public string Map { get; set; }

		public string Part { get; set; }

		public bool IsValid()
		{
			return !string.IsNullOrWhiteSpace(Map) && (Part == null || !string.IsNullOrWhiteSpace(Part));
		}

		public RequestType GetRequestType()
		{
			return Part switch
			{
				null => RequestType.MapDefinition,
				_ => RequestType.MapPart
			};
		}
	}
}
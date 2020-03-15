using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using OpenSauce.MapServer.Lib.Models;
using OpenSauce.MapServer.Lib.Storage;

namespace OpenSauce.MapServer.Functions
{
	public static class OpenSauceMapServerRequest
	{
		[FunctionName("OpenSauceMapServerRequest")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mapdownload")] [FromQuery]
			MapDownloadParameters parameters,
			ILogger log,
			ExecutionContext context)
		{
			if (!parameters.IsValid())
			{
				return new NotFoundResult();
			}

			return parameters.GetRequestType() switch
			{
				RequestType.MapDefinition => await GetMapDefinitionAsync(log, context, parameters.Map),
				RequestType.MapPart => await GetMapPartAsync(log, context, parameters.Map, parameters.Part),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		private static async Task<IActionResult> GetMapDefinitionAsync(ILogger log, ExecutionContext context, string map)
		{
			try
			{
				var mapStorage = GetMapStorage(log, context);
				var mapPartDefinitionModel = await mapStorage.GetMapPartDefinitionAsync(map);

				var writer = new StringWriter();
				new XmlSerializer(typeof(MapPartDefinitionModel)).Serialize(writer, mapPartDefinitionModel);
				var content = writer.ToString();

				return new ContentResult
				{
					Content = content,
					ContentType = "application/xml"
				};
			}
			catch (FileNotFoundException)
			{
				return new NotFoundResult();
			}
		}

		private static async Task<IActionResult> GetMapPartAsync(ILogger log, ExecutionContext context, string map, string part)
		{
			try
			{
				var mapStorage = GetMapStorage(log, context);
				var partStream = await mapStorage.GetMapPartStreamAsync(map, part);

				return new FileStreamResult(partStream, new MediaTypeHeaderValue("application/octet-stream"))
				{
					FileDownloadName = part
				};
			}
			catch (FileNotFoundException)
			{
				return new NotFoundResult();
			}
		}

		private static MapStorage GetMapStorage(ILogger log, ExecutionContext context)
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(context.FunctionAppDirectory)
				.AddJsonFile("local.settings.json", true, true)
				.AddEnvironmentVariables()
				.Build();
			var serviceClient = new BlobServiceClient(configuration["ConnectionStrings:opensaucemapserverstorage"]);
			var containerClient = serviceClient.GetBlobContainerClient("opensauce-mapserver-maps");
			return new MapStorage(log, containerClient);
		}
	}
}
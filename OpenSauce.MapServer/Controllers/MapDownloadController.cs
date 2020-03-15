using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using OpenSauce.MapServer.Lib.Models;
using OpenSauce.MapServer.Lib.Storage;

namespace OpenSauce.MapServer.Controllers
{
	[Route("api/[controller]")]
	public sealed class MapDownloadController : Controller
	{
		public MapDownloadController(BlobServiceClient blobServiceClient, ILogger<MapDownloadController> logger)
		{
			_logger = logger;
			_mapBlobContainerClient = blobServiceClient.GetBlobContainerClient("opensauce-mapserver-maps");
		}

		[HttpGet]
		public async Task<IActionResult> Get([FromQuery] MapDownloadParameters parameters)
		{
			if (!parameters.IsValid())
			{
				return NotFound();
			}

			return parameters.GetRequestType() switch
			{
				RequestType.MapDefinition => await GetMapDefinitionAsync(parameters.Map),
				RequestType.MapPart => await GetMapPartAsync(parameters.Map, parameters.Part),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		private async Task<IActionResult> GetMapDefinitionAsync(string map)
		{
			try
			{
				var mapStorage = GetMapStorage();
				var mapPartDefinitionModel = await mapStorage.GetMapPartDefinitionAsync(map);

				Response.ContentType = "application/xml";
				return Ok(mapPartDefinitionModel);
			}
			catch (FileNotFoundException)
			{
				return NotFound($"Unknown map \"{map}\"");
			}
		}

		private async Task<IActionResult> GetMapPartAsync(string map, string part)
		{
			try
			{
				var mapStorage = GetMapStorage();
				var partStream = await mapStorage.GetMapPartStreamAsync(map, part);

				return new FileStreamResult(partStream, new MediaTypeHeaderValue("application/octet-stream"))
				{
					FileDownloadName = part
				};
			}
			catch (FileNotFoundException)
			{
				return NotFound($"Unknown map part \"{part}\"");
			}
		}

		private MapStorage GetMapStorage()
		{
			return new MapStorage(_logger, _mapBlobContainerClient);
		}

		private readonly ILogger<MapDownloadController> _logger;
		private readonly BlobContainerClient _mapBlobContainerClient;
	}
}
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using OpenSauce.MapServer.Models;

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
				var mapMetadata = await GetMapMetadataAsync(map);

				Response.ContentType = "application/xml";
				return Ok(
					new MapPartDefinitionModel
					{
						MapDownload = new MapDownloadModel
						{
							Algorithm = CompressionAlgorithm.Zip,
							Name = mapMetadata.UncompressedName,
							MD5 = mapMetadata.UncompressedMD5,
							UncompressedSize = mapMetadata.UncompressedSize,
							CompressedSize = mapMetadata.CompressedSize,
							Parts = mapMetadata.Parts
						}
					});
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
				var mapMetadata = await GetMapMetadataAsync(map);

				var partMetadata = mapMetadata.Parts.FirstOrDefault(p => p.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
				if (partMetadata == null)
				{
					_logger.Log(LogLevel.Error, "MapPartNotFound:{0}", part);
					throw new FileNotFoundException($"A map part was not found matching {part}");
				}

				var blobClient = await GetBlobClientAsync(mapMetadata.CompressedName);
				if (blobClient == null)
				{
					_logger.Log(LogLevel.Error, "MapPartNotFound:{0}", part);
					throw new FileNotFoundException($"A map part was not found matching {part}");
				}

				var downloadInfo = await blobClient.DownloadAsync(new HttpRange(partMetadata.StartOffset, partMetadata.Size));
				return new FileStreamResult(downloadInfo.Value.Content, new MediaTypeHeaderValue("application/octet-stream"))
				{
					FileDownloadName = part
				};
			}
			catch (FileNotFoundException)
			{
				return NotFound($"Unknown map part \"{part}\"");
			}
		}

		private async Task<MapMetadata> GetMapMetadataAsync(string map)
		{
			_logger.LogInformation("MapRequested:{0}", map);

			var blobClient = await GetBlobClientAsync($"{map}.json");
			if (blobClient == null)
			{
				_logger.Log(LogLevel.Error, "MapNotFound:{0}", map);
				throw new FileNotFoundException($"A map was not found matching {map}");
			}

			var downloadInfo = await blobClient.DownloadAsync();
			using var reader = new StreamReader(downloadInfo.Value.Content);
			return JsonConvert.DeserializeObject<MapMetadata>(await reader.ReadToEndAsync());
		}

		private async Task<BlobClient> GetBlobClientAsync(string itemName)
		{
			var containerClient = _mapBlobContainerClient;
			await foreach (var item in containerClient.GetBlobsAsync())
			{
				if (!item.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				return containerClient.GetBlobClient(item.Name);
			}

			return null;
		}

		private readonly ILogger<MapDownloadController> _logger;

		private readonly BlobContainerClient _mapBlobContainerClient;
	}
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Hallo;
using Hallo.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Primitives;
using RazorLight;
using Transacto.Framework;
using Transacto.Views;

namespace Transacto {
	public sealed class TextResponse : Response {
		private readonly string _body;

		public TextResponse(string body) {
			Headers["content-type"] = "text/plain";
			_body = body;
		}

		protected internal override ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) => stream
			.WriteAsync(Encoding.UTF8.GetBytes(_body), cancellationToken);
	}

	public class HalResponse : Response {
		private static readonly object EmptyBody = new object();

		private readonly Response _inner;

		private static MediaType HalJson => new MediaType("application/hal+json");
		private static MediaType Html => new MediaType("text/html");

		public override IDictionary<string, StringValues> Headers => _inner.Headers;
		public override HttpStatusCode StatusCode { get => _inner.StatusCode; set => _inner.StatusCode = value; }

		public HalResponse(HttpRequest request, IHal hal, Optional<Position> checkpoint) : this(request, hal,
			checkpoint, null!) {
		}
		public HalResponse(HttpRequest request, IHal hal, Optional<Position> checkpoint, Optional<object> resource) {
			_inner = request.Headers["accept"].Select(MediaType).Select(Response).FirstOrDefault() ??
			         NotAcceptableResponse.Instance;

			if (checkpoint.HasValue) {
				var value = checkpoint.Value;
				_inner.Headers.Add("etag", $"{value.CommitPosition}/{value.PreparePosition}");
			}

			Response Response(MediaType m) =>
				(HalJson.IsSubsetOf(m), Html.IsSubsetOf(m) || m.SubTypeSuffix == "html") switch {
					(true, false) => new HalJsonResponse(hal, resource),
					(false, true) => new HalHtmlResponse(hal, resource),
					_ => NotAcceptableResponse.Instance
				};

			static MediaType MediaType(string x) => new MediaType(x ?? string.Empty);
		}

		protected internal override ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) => _inner
			.WriteBody(stream, cancellationToken);

		private sealed class HalHtmlResponse : Response {
			private static readonly ConcurrentDictionary<Assembly, RazorLightEngine> Engines =
				new ConcurrentDictionary<Assembly, RazorLightEngine>();

			private readonly object _resource;
			private readonly IHal _hal;

			public HalHtmlResponse(IHal hal, Optional<object> resource) {
				_resource = resource.HasValue ? resource.Value : EmptyBody;
				_hal = hal;

				Headers.Add("content-type", "text/html");
			}

			protected internal override async ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) {
				var representation = await _hal.RepresentationOfAsync(_resource);
				await stream.WriteAsync(Encoding.UTF8.GetBytes("<html><body>"), cancellationToken);

				await stream.WriteAsync(Encoding.UTF8.GetBytes(await Render(typeof(Links), representation)),
					cancellationToken);
				await stream.WriteAsync(Encoding.UTF8.GetBytes(await Render(_hal.GetType(), representation)),
					cancellationToken);

				await stream.WriteAsync(Encoding.UTF8.GetBytes("</body></html>"), cancellationToken);
			}

			private Task<string> Render(Type type, HalRepresentation representation) => Engines
				.GetOrAdd(type.Assembly, assembly => new RazorLightEngineBuilder()
					.UseEmbeddedResourcesProject(assembly)
					.UseMemoryCachingProvider()
					.Build())
				.CompileRenderAsync(type.FullName, representation.State);
		}

		private sealed class HalJsonResponse : Response {
			private static readonly JsonSerializerOptions SerializerOptions
				= new JsonSerializerOptions {
					Converters = {
						new LinksConverter(),
						new HalRepresentationConverter()
					},
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				};

			private readonly object _resource;
			private readonly IHal _hal;

			public HalJsonResponse(IHal hal, Optional<object> resource) {
				_resource = resource.HasValue ? resource.Value : EmptyBody;
				_hal = hal;

				Headers.Add("content-type", HalJson.ToString());
			}

			protected internal override async ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) {
				var representation = await _hal.RepresentationOfAsync(_resource);
				await JsonSerializer.SerializeAsync(stream, representation, SerializerOptions, cancellationToken);
			}
		}
	}
}

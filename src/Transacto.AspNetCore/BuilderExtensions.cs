using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;

namespace Transacto {
	static partial class BuilderExtensions {
		public static IEndpointRouteBuilder MapGet(this IEndpointRouteBuilder builder, string route,
			Func<CancellationToken, ValueTask<Response>> getResponse) {
			var routeTemplate = TemplateParser.Parse(route);
			var argumentCount = routeTemplate.Parameters.Count(p => p.IsParameter);

			if (argumentCount != 0) {
				throw new Exception();
			}

			return builder.MapGetInternal<object>(route, values => null!, (_, ct) => getResponse(ct));
		}

		public static IEndpointRouteBuilder MapGet(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, ValueTask<Response>> getResponse) {
			builder.MapGet(route, async context => {
				var response = await getResponse(context);

				await response.Write(context.Response);
			});

			return builder;
		}

		public static IEndpointRouteBuilder MapPost<T>(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, T, ValueTask<Response>> getResponse) {
			builder.MapPost(route, async context => {
				var request = await JsonSerializer.DeserializeAsync<T>(context.Request.Body);

				var response = await getResponse(context, request);

				await response.Write(context.Response);
			});
			return builder;
		}

		public static IEndpointRouteBuilder MapPost(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, ValueTask<Response>> getResponse) {
			builder.MapPost(route, async context => {
				var response = await getResponse(context);

				await response.Write(context.Response);
			});
			return builder;
		}

		public static IEndpointRouteBuilder MapPut<T>(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, T, ValueTask<Response>> getResponse) {
			builder.MapPut(route, async context => {
				var request = await JsonSerializer.DeserializeAsync<T>(context.Request.Body);

				var response = await getResponse(context, request);

				await response.Write(context.Response);
			});
			return builder;
		}

		public static IEndpointRouteBuilder MapCommands(this IEndpointRouteBuilder builder, string route,
			params Type[] commandTypes) =>
			builder.MapCommandsInternal(route, TransactoSerializerOptions.Commands, commandTypes);

		public static IEndpointRouteBuilder MapBusinessTransaction<T>(this IEndpointRouteBuilder builder, string route)
			where T : IBusinessTransaction =>
			builder.MapCommandsInternal(route, TransactoSerializerOptions.BusinessTransactions(typeof(T)),
				typeof(PostGeneralLedgerEntry));

		private static IEndpointRouteBuilder MapCommandsInternal(this IEndpointRouteBuilder builder, string route,
			JsonSerializerOptions serializerOptions,
			params Type[] commandTypes) {
			var dispatcher = new CommandDispatcher(builder.ServiceProvider.GetServices<CommandHandlerModule>());
			var commandContext = builder.ServiceProvider.GetRequiredService<ICommandContext>();

			var map = commandTypes.ToDictionary(commandType => commandType.Name);

			builder.MapPost(route, async context => {
				if (!MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var mediaType) ||
				    !mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase)) {
					return new Response {StatusCode = HttpStatusCode.UnsupportedMediaType};
				}

				if (!context.Request.Form.TryGetValue("command", out var commandName)) {
					return new TextResponse($"No command type was specified.") {
						StatusCode = HttpStatusCode.BadRequest
					};
				}
				if (!map.TryGetValue(commandName, out var commandType)) {
					return new TextResponse($"The command type '{commandName}' was not recognized.") {
						StatusCode = HttpStatusCode.BadRequest
					};
				}

				if (context.Request.Form.Files.Count != 1) {
					return new TextResponse("No command was found on the request.")
						{StatusCode = HttpStatusCode.BadRequest};
				}

				await using var commandStream = context.Request.Form.Files[0].OpenReadStream();
				var command = await JsonSerializer.DeserializeAsync(commandStream, commandType,
					serializerOptions);

				await dispatcher.Handle(command, context.RequestAborted);

				return new Response {
					Headers = {
						["etag"] =
							commandContext.Position.HasValue
								? $@"""{commandContext.Position.Value.ToString()}"""
								: string.Empty
					},
					StatusCode = HttpStatusCode.OK
				};
			});

			return builder;
		}

		private static IEndpointRouteBuilder MapGetInternal<T>(this IEndpointRouteBuilder builder, string route,
			Func<object[], T> getDto, Func<T, CancellationToken, ValueTask<Response>> getResponse) {
			builder.MapMethods(route, new[] {HttpMethod.Get.Method}, async context => {
				var dto = getDto(context.GetRouteData().Values.Values.ToArray());

				var response = await getResponse(dto, context.RequestAborted);

				await response.Write(context.Response);
			});

			return builder;
		}
	}
}

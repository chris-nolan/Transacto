using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Transacto {
	public static class Negotiate {
		public static Response Content(HttpRequest request, params Response[] responses) {
			foreach (var acceptHeader in request.Headers.GetCommaSeparatedValues("accept")
				.Select(MediaTypeWithQualityHeaderValue.Parse)
				.OrderByDescending(h => h.Quality)) {
				var mediaType = new MediaType(acceptHeader.MediaType);
				var match = responses.FirstOrDefault(response => new MediaType(response.Headers["content-type"])
					.IsSubsetOf(mediaType));
				if (match == null) {
					continue;
				}

				return match;
			}

			return new Response {
				StatusCode = HttpStatusCode.NotAcceptable
			};
		}
	}
}

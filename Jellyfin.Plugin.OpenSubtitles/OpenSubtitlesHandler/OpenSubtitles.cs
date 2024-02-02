using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.OpenSubtitles.OpenSubtitlesHandler.Models;
using Jellyfin.Plugin.OpenSubtitles.OpenSubtitlesHandler.Models.Responses;

namespace Jellyfin.Plugin.OpenSubtitles.OpenSubtitlesHandler
{
    /// <summary>
    /// The open subtitles helper class.
    /// </summary>
    public static class OpenSubtitles
    {
        /// <summary>
        /// Login.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="apiKey">The api key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The api response.</returns>
        public static async Task<ApiResponse<LoginInfo>> LogInAsync(string username, string password, string apiKey, CancellationToken cancellationToken)
        {
            var body = new { username, password };
            var response = await RequestHandler.SendRequestAsync("/login", HttpMethod.Post, body, null, apiKey, 1, cancellationToken).ConfigureAwait(false);

            return new ApiResponse<LoginInfo>(response);
        }

        /// <summary>
        /// Logout.
        /// </summary>
        /// <param name="user">The user information.</param>
        /// <param name="apiKey">The api key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>logout status.</returns>
        public static async Task<bool> LogOutAsync(LoginInfo user, string apiKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(user.Token))
            {
                throw new ArgumentNullException(nameof(user.Token), "Token is null or empty");
            }

            var headers = new Dictionary<string, string> { { "Authorization", user.Token } };

            var response = await RequestHandler.SendRequestAsync("/logout", HttpMethod.Delete, null, headers, apiKey, 1, cancellationToken).ConfigureAwait(false);

            return new ApiResponse<object>(response).Ok;
        }

        /// <summary>
        /// Get user info.
        /// </summary>
        /// <param name="user">The user information.</param>
        /// <param name="apiKey">The api key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The encapsulated user info.</returns>
        public static async Task<ApiResponse<EncapsulatedUserInfo>> GetUserInfo(LoginInfo user, string apiKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(user.Token))
            {
                throw new ArgumentNullException(nameof(user.Token), "Token is null or empty");
            }

            var headers = new Dictionary<string, string> { { "Authorization", user.Token } };

            var response = await RequestHandler.SendRequestAsync("/infos/user", HttpMethod.Get, null, headers, apiKey, 1, cancellationToken).ConfigureAwait(false);

            return new ApiResponse<EncapsulatedUserInfo>(response);
        }

        /// <summary>
        /// Get the subtitle link.
        /// </summary>
        /// <param name="file">The subtitle file.</param>
        /// <param name="user">The user information.</param>
        /// <param name="apiKey">The api key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The subtitle download info.</returns>
        public static async Task<ApiResponse<SubtitleDownloadInfo>> GetSubtitleLinkAsync(int file, LoginInfo user, string apiKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(user.Token))
            {
                throw new ArgumentNullException(nameof(user.Token), "Token is null or empty");
            }

            var headers = new Dictionary<string, string> { { "Authorization", user.Token } };

            var body = new { file_id = file };
            var response = await RequestHandler.SendRequestAsync("/download", HttpMethod.Post, body, headers, apiKey, 1, cancellationToken).ConfigureAwait(false);

            return new ApiResponse<SubtitleDownloadInfo>(response, $"file id: {file}");
        }

        /// <summary>
        /// Download subtitle.
        /// </summary>
        /// <param name="url">the subtitle url.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The Http response.</returns>
        public static async Task<HttpResponse> DownloadSubtitleAsync(string url, CancellationToken cancellationToken)
        {
            var response = await OpenSubtitlesRequestHelper.Instance!.SendRequestAsync(
                url,
                HttpMethod.Get,
                null,
                new Dictionary<string, string>(),
                cancellationToken).ConfigureAwait(false);

            return new HttpResponse
            {
                Body = response.body,
                Code = response.statusCode
            };
        }

        /// <summary>
        /// Search for subtitle.
        /// </summary>
        /// <param name="options">The search options.</param>
        /// <param name="apiKey">The api key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of response data.</returns>
        public static async Task<ApiResponse<IReadOnlyList<ResponseData>>> SearchSubtitlesAsync(Dictionary<string, string> options, string apiKey, CancellationToken cancellationToken)
        {
            var opts = new Dictionary<string, string>();
            foreach (var (key, value) in options)
            {
                opts.Add(key.ToLowerInvariant(), value.ToLowerInvariant());
            }

            var max = -1;
            var current = 1;

            List<ResponseData> final = new();
            ApiResponse<SearchResult> last;
            HttpResponse response;

            do
            {
                if (current > 1)
                {
                    options["page"] = current.ToString(CultureInfo.InvariantCulture);
                }

                var url = RequestHandler.AddQueryString("/subtitles", opts);
                response = await RequestHandler.SendRequestAsync(url, HttpMethod.Get, null, null, apiKey, 1, cancellationToken).ConfigureAwait(false);

                last = new ApiResponse<SearchResult>(response, $"url: {url}", $"page: {current}");

                if (!last.Ok || last.Data == null)
                {
                    break;
                }

                if (last.Data.TotalPages == 0)
                {
                    break;
                }

                if (max == -1)
                {
                    max = last.Data.TotalPages;
                }

                var currentBefore = current;
                current = last.Data.Page + 1;
                var currentAfter = current;
                if (currentBefore == currentAfter)
                {
                    break;
                }

                final.AddRange(last.Data.Data);
            }
            while (current <= max);

            return new ApiResponse<IReadOnlyList<ResponseData>>(final, response);
        }

        /// <summary>
        /// Get language list.
        /// </summary>
        /// <param name="apiKey">The api key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of languages.</returns>
        public static async Task<ApiResponse<EncapsulatedLanguageList>> GetLanguageList(string apiKey, CancellationToken cancellationToken)
        {
            var response = await RequestHandler.SendRequestAsync("/infos/languages", HttpMethod.Get, null, null, apiKey, 1, cancellationToken).ConfigureAwait(false);

            return new ApiResponse<EncapsulatedLanguageList>(response);
        }
    }
}

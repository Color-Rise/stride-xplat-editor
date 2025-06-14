// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text.RegularExpressions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Launcher.Assets.Localization;

namespace Stride.Launcher.ViewModels;

public sealed partial class DocumentationPageViewModel : DispatcherViewModel
{
    private static readonly Regex ParsingRegex = GetParsingRegex();
    private static readonly HttpClient httpClient = new();
    private const string DocPageScheme = "page:";
    private const string PageUrlFormatString = "{0}{1}";

    public DocumentationPageViewModel(IViewModelServiceProvider serviceProvider, string version)
        : base(serviceProvider)
    {
        Version = version;
        OpenUrlCommand = new AnonymousTaskCommand(ServiceProvider, OpenUrl);
    }

    private async Task OpenUrl()
    {
        try
        {
            Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
        }
        catch (Exception)
        {
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(Strings.ErrorOpeningBrowser, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Gets the root url of the documentation that should be opened when the user want to open Stride help.
    /// </summary>
    public string DocumentationRootUrl => GetDocumentationRootUrl(Version);

    /// <summary>
    /// Gets the version related to this documentation page.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets or sets the title of this documentation page.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description of this documentation page.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the url of this documentation page.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets a command that will open the documentation page in the default web browser.
    /// </summary>
    public ICommandBase OpenUrlCommand { get; private set; }

    public static async Task<List<DocumentationPageViewModel>> FetchGettingStartedPages(IViewModelServiceProvider serviceProvider, string version)
    {
        var result = new List<DocumentationPageViewModel>();
        string urlData;
        try
        {
            using var response = await httpClient.GetAsync(string.Format(Urls.GettingStarted, version));
            response.EnsureSuccessStatusCode();
            urlData = await response.Content.ReadAsStringAsync();

            if (urlData is null)
            {
                return result;
            }
            var urls = urlData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var url in urls)
            {
                var match = ParsingRegex.Match(url);
                if (!match.Success || match.Groups.Count != 4)
                {
                    continue;
                }
                var link = match.Groups[3].Value;
                if (link.StartsWith(DocPageScheme))
                {
                    link = GetDocumentationPageUrl(version, link.Substring(DocPageScheme.Length));
                }
                var page = new DocumentationPageViewModel(serviceProvider, version)
                {
                    Title = match.Groups[1].Value.Trim(),
                    Description = match.Groups[2].Value.Trim(),
                    Url = link.Trim()
                };
                result.Add(page);
            }
            return result;
        }
        catch (Exception)
        {
            result.Clear();
        }
        return result;
    }

    /// <summary>
    /// Compute the url of a documentation page, given the page name.
    /// </summary>
    /// <param name="version">The version related to this documentation page.</param>
    /// <param name="pageName">The name of the page.</param>
    /// <returns>The complete url of the documentation page.</returns>
    private static string GetDocumentationPageUrl(string version, string pageName)
    {
        return string.Format(PageUrlFormatString, GetDocumentationRootUrl(version), pageName);
    }

    private static string GetDocumentationRootUrl(string version)
    {
        return string.Format(Urls.Documentation, version);
    }

    [GeneratedRegex(@"\{([^\{\}]+)\}\{([^\{\}]+)\}\{([^\{\}]+)\}")]
    private static partial Regex GetParsingRegex();
}

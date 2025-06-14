// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Xml;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Launcher.Assets.Localization;

namespace Stride.Launcher.ViewModels;

public sealed class NewsPageViewModel : DispatcherViewModel
{
    private static readonly HttpClient httpClient = new();

    public NewsPageViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        OpenUrlCommand = new AnonymousTaskCommand(ServiceProvider, OpenUrl);
    }

    private async Task OpenUrl()
    {
        if (Url is null) return;

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
    /// Gets or sets the url of this documentation page.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets a command that will open the documentation page in the default web browser.
    /// </summary>
    public ICommandBase OpenUrlCommand { get; private set; }

    public static async Task<List<NewsPageViewModel>> FetchNewsPages(IViewModelServiceProvider serviceProvider, int maxCount)
    {
        var result = new List<NewsPageViewModel>();
        try
        {
            using var response = await httpClient.GetAsync(Urls.RssFeed);
            response.EnsureSuccessStatusCode();
            var rss = await response.Content.ReadAsStreamAsync();

            if (rss.Length == 0)
                return result;

            int count = 0;
            using XmlReader rssReader = XmlReader.Create(rss, new XmlReaderSettings { Async = true });
            await rssReader.MoveToContentAsync();
            while (rssReader.ReadToFollowing("item") && count < maxCount)
            {
                rssReader.ReadToFollowing("title");
                string? title = await rssReader.ReadAsync() ? rssReader.Value : null;
                rssReader.ReadToFollowing("description");
                string? description = await rssReader.ReadAsync() ? rssReader.Value : null;
                rssReader.ReadToFollowing("pubDate");
                var date = new DateTime();
                bool dateValid = await rssReader.ReadAsync() && DateTime.TryParseExact(rssReader.Value, "ddd, dd MMM yyyy HH:mm:ss zz00", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                rssReader.ReadToFollowing("link");
                string? link = await rssReader.ReadAsync() ? rssReader.Value : null;
                if (dateValid && title is not null && link is not null && description is not null)
                {
                    var page = new NewsPageViewModel(serviceProvider)
                    {
                        Title = title,
                        Url = link,
                        Description = description,
                        Date = date
                    };
                    result.Add(page);
                    ++count;
                }
            }
        }
        catch (Exception)
        {
            result.Clear();
        }

        return result;
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Microsoft.Win32;
using Stride.Core.Presentation.Avalonia.Windows;
using Stride.Core.Presentation.Services;
using Stride.Launcher.Views;

namespace Stride.Launcher.Services;

/// <summary>
/// A helper class to manage Privacy Policy acceptance.
/// </summary>
internal static class PrivacyPolicyHelper
{
    internal const string PrivacyPolicyNotLoaded = "Unable to load the End User License Agreement file.";
    private const string Stride40Name = "Stride-4.0";

    static PrivacyPolicyHelper()
    {
        var localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
        using var subkey = localMachine32.OpenSubKey(@"SOFTWARE\Stride\Agreements\");
        if (subkey != null)
        {
            var value = (string)subkey.GetValue(Stride40Name);
            Stride40Accepted = value != null && value.ToLowerInvariant() == "true";
        }
    }

    /// <summary>
    /// Gets whether the Privacy Policy for Stride 3.0 has been accepted.
    /// </summary>
    internal static bool Stride40Accepted { get; private set; }

    /// <summary>
    /// Gets or sets the <see cref="Action"/> that will restart the application.
    /// </summary>
    internal static Action RestartApplication { get; set; }

    /// <summary>
    /// Checks whether the Privacy Policy for Stride 4.0 has been accepted or not. If not, displays a window asking for the agreement.
    /// If the user declines, the application is terminated. Otherwise, it is restarted with the same arguments.
    /// </summary>
    internal static void EnsurePrivacyPolicyStride40()
    {
        if (RestartApplication == null)
            throw new InvalidOperationException("The RestartApplication property must be set before calling this method.");

        if (!Stride40Accepted)
        {
            // Note: because we are not running from the main loop, we have to start a new app
            Program.RunNewApp<Application>(AppMain);
        }

        CancellationToken AppMain(Application app)
        {
            var cts = new CancellationTokenSource();
            _ = AppMainAsync(cts);
            return cts.Token;
        }

        async Task AppMainAsync(CancellationTokenSource cts)
        {
            // display privacyWindow
            var tcs = new TaskCompletionSource();
            var privacyWindow = new PrivacyPolicyWindow(true);
            privacyWindow.Closed += (_,_) => tcs.SetResult();
            privacyWindow.Show();
            await tcs.Task;

            if (!Stride40Accepted)
            {
                await MessageBox.ShowAsync("Stride", "The Privacy Policy has been declined. The application will now exit.", IDialogService.GetButtons(MessageBoxButton.OK), MessageBoxImage.Information);
                await cts.CancelAsync();
                Environment.Exit(1);
            }
            // We restart the application after Privacy Policy acceptance.
            await cts.CancelAsync();
            RestartApplication();
        }
    }

    /// <summary>
    /// Notifies that the Privacy Policy for Stride 3.0 has been accepted.
    /// </summary>
    /// <returns><c>True</c> if the acceptance could be properly saved, <c>false</c> otherwise.</returns>
    internal static bool AcceptStride40()
    {
        try
        {
            var localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            using var subkey = localMachine32.CreateSubKey(@"SOFTWARE\Stride\Agreements\");
            if (subkey == null)
                return false;

            subkey.SetValue(Stride40Name, "True");
            Stride40Accepted = true;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    internal static bool RevokeAllPrivacyPolicy()
    {
        try
        {
            var localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            using var subkey = localMachine32.CreateSubKey(@"SOFTWARE\Stride\Agreements\");
            if (subkey == null)
                return false;

            foreach (var valueName in subkey.GetValueNames())
            {
                subkey.DeleteValue(valueName);
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

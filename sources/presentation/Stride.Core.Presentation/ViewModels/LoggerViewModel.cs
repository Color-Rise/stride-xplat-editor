// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.ComponentModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Presentation.ViewModels;

public class LoggerViewModel : DispatcherViewModel, ITitledPage
{
    /// <summary>
    /// The default delay to wait before updating the <see cref="Messages"/> collection, after a message has been received.
    /// </summary>
    public const int DefaultUpdateInterval = 300;

    private readonly ObservableList<ILogMessage> messages = [];
    private readonly List<(Logger, ILogMessage)> pendingMessages = [];

    private int updateInterval = DefaultUpdateInterval;
    private bool updatePending;
    private bool hasNewMessages;

    public LoggerViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        AddLoggerCommand = new AnonymousCommand<Logger>(serviceProvider, AddLogger);
        RemoveLoggerCommand = new AnonymousCommand<Logger>(serviceProvider, RemoveLogger);
        ClearLoggersCommand = new AnonymousCommand(serviceProvider, ClearLoggers);
        ClearMessagesCommand = new AnonymousCommand(serviceProvider, ClearMessages);
        ClearNewMessageFlagCommand = new AnonymousCommand(serviceProvider, () => HasNewMessages = false);
        messages.CollectionChanged += MessagesCollectionChanged;
    }

    public LoggerViewModel(IViewModelServiceProvider serviceProvider, Logger logger)
        : this(serviceProvider)
    {
        Loggers.Add(logger, []);
        logger.MessageLogged += MessageLogged;
    }

    public LoggerViewModel(IViewModelServiceProvider serviceProvider, IEnumerable<Logger> loggers)
        : this(serviceProvider)
    {
        foreach (var logger in loggers)
        {
            Loggers.Add(logger, []);
            logger.MessageLogged += MessageLogged;
        }
    }

    /// <summary>
    /// Gets whether the monitored logs have errors.
    /// </summary>
    /// <remarks>This property does not raise the <see cref="INotifyPropertyChanging.PropertyChanging"/> and <see cref="INotifyPropertyChanged.PropertyChanged"/> events.</remarks>
    public bool HasErrors { get; private set; }

    /// <summary>
    /// Gets whether the monitored logs have new messages.
    /// </summary>
    public bool HasNewMessages { get { return hasNewMessages; } private set { SetValue(ref hasNewMessages, value); } }

    /// <summary>
    /// Gets whether the monitored logs have warnings.
    /// </summary>
    /// <remarks>This property does not raise the <see cref="INotifyPropertyChanging.PropertyChanging"/> and <see cref="INotifyPropertyChanged.PropertyChanged"/> events.</remarks>
    public bool HasWarnings { get; private set; }

    /// <summary>
    /// Gets the minimum level of message that will be recorded by this view model.
    /// </summary>
    public LogMessageType MinLevel { get; set; } = LogMessageType.Debug;

    /// <summary>
    /// Gets the collection of messages currently contained in this view model.
    /// </summary>
    public IReadOnlyObservableCollection<ILogMessage> Messages => messages;

    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the interval in milliseconds between updates of the <see cref="Messages"/> collection. When a message is logged into one of the loggers,
    /// the view model will wait this interval before actually updating the message collection to catch other potential messages in a single shot.
    /// </summary>
    /// <remarks>The default value is equal to <see cref="DefaultUpdateInterval"/>.</remarks>
    public int UpdateInterval { get { return updateInterval; } set { SetValue(ref updateInterval, value); } }

    /// <summary>
    /// Gets the command that will add a logger to monitor.
    /// </summary>
    /// <remarks>An instance of <see cref="Logger"/> must be passed as parameter of this command.</remarks>
    public ICommandBase AddLoggerCommand { get; }

    /// <summary>
    /// Gets the command that will remove all loggers from monitoring. 
    /// </summary>
    public ICommandBase ClearLoggersCommand { get; }

    /// <summary>
    /// Gets the command that will clear the <see cref="Messages"/> collection.
    /// </summary>
    public ICommandBase ClearMessagesCommand { get; }

    /// <summary>
    /// Gets the command that will reset the <see cref="HasNewMessages"/> flag.
    /// </summary>
    public ICommandBase ClearNewMessageFlagCommand { get; }

    /// <summary>
    /// Gets the command that will remove a logger from monitoring.
    /// </summary>
    /// <remarks>An instance of <see cref="Logger"/> must be passed as parameter of this command.</remarks>
    public ICommandBase RemoveLoggerCommand { get; }

    protected Dictionary<Logger, List<ILogMessage>> Loggers { get; } = [];

    /// <summary>
    /// Adds a <see cref="Logger"/> to monitor.
    /// </summary>
    /// <param name="logger">The <see cref="Logger"/> to monitor.</param>
    public virtual void AddLogger(Logger logger)
    {
        Loggers.Add(logger, []);
        logger.MessageLogged += MessageLogged;
    }

    /// <summary>
    /// Removes a <see cref="Logger"/> from monitoring.
    /// </summary>
    /// <param name="logger">The <see cref="Logger"/> to remove from monitoring.</param>
    public virtual void RemoveLogger(Logger logger)
    {
        Loggers.Remove(logger);
        logger.MessageLogged -= MessageLogged;
    }

    /// <summary>
    /// Removes all loggers from monitoring.
    /// </summary>
    public virtual void ClearLoggers()
    {
        foreach (var logger in Loggers)
        {
            logger.Key.MessageLogged -= MessageLogged;
        }
        Loggers.Clear();
    }

    /// <summary>
    /// Clears the <see cref="Messages"/> collection.
    /// </summary>
    public void ClearMessages()
    {
        messages.Clear();
        foreach (var logger in Loggers)
        {
            logger.Value.Clear();
        }
    }

    /// <summary>
    /// Removes messages that comes from the given logger from the <see cref="Messages"/> collection.
    /// </summary>
    public void ClearMessages(Logger logger)
    {
        if (Loggers.TryGetValue(logger, out var messagesToRemove))
        {
            foreach (var messageToRemove in messagesToRemove)
            {
                messages.Remove(messageToRemove);
            }
        }
    }

    /// <summary>
    /// Flushes the pending log messages to add them immediately in the view model.
    /// </summary>
    public void Flush()
    {
        // Temporary cut the update interval. We use the backing field directly to
        // prevent triggering a PropertyChanged event.
        var interval = updateInterval;
        updateInterval = 0;
        Dispatcher.InvokeTask(UpdateMessagesAsync);
        updateInterval = interval;
    }

    /// <summary>
    /// Raised when the messages collection is changed. Updates <see cref="HasWarnings"/> and <see cref="HasErrors"/> properties.
    /// </summary>
    private void MessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            if (e.NewItems != null)
            {
                foreach (ILogMessage newMessage in e.NewItems)
                {
                    switch (newMessage.Type)
                    {
                        case LogMessageType.Warning:
                            HasWarnings = true;
                            break;
                        case LogMessageType.Error:
                        case LogMessageType.Fatal:
                            HasErrors = true;
                            break;
                    }
                }
            }
            HasNewMessages = true;
        }
        else
        {
            HasWarnings = messages.Any(x => x.Type == LogMessageType.Warning);
            HasErrors = messages.Any(x => x.Type == LogMessageType.Error || x.Type == LogMessageType.Fatal);
        }
    }

    /// <summary>
    /// The callback of the <see cref="Logger.MessageLogged"/> event, used to monitor incoming messages.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="args">The event argument.</param>
    private void MessageLogged(object? sender, MessageLoggedEventArgs args)
    {
        lock (pendingMessages)
        {
            if (sender is Logger logger && args.Message.IsAtLeast(MinLevel))
            {
                pendingMessages.Add((logger, args.Message));
                if (!updatePending)
                {
                    updatePending = true;
                    Dispatcher.InvokeTask(UpdateMessagesAsync);
                }
            }
        }
    }

    /// <summary>
    /// This methods waits the <see cref="UpdateInterval"/> delay and then updates the <see cref="Messages"/> collection by adding all pending messages.
    /// </summary>
    private async Task UpdateMessagesAsync()
    {
        if (UpdateInterval >= 0) await Task.Delay(UpdateInterval);

        List<(Logger, ILogMessage)>? messagesToAdd = null;
        lock (pendingMessages)
        {
            if (pendingMessages.Count > 0)
            {
                messagesToAdd = [.. pendingMessages];
                pendingMessages.Clear();
            }
            updatePending = false;
        }
        if (messagesToAdd != null)
        {
            foreach (var messageToAdd in messagesToAdd)
            {
                messages.Add(messageToAdd.Item2);
                if (Loggers.TryGetValue(messageToAdd.Item1, out var logger))
                {
                    logger.Add(messageToAdd.Item2);
                }
            }
        }
    }
}

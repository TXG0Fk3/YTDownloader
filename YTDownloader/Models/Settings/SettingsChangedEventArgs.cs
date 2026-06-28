using System;

namespace YTDOwnloader.Models.Settings;

public class SettingsChangedEventArgs : EventArgs
{
    public required string PropertyName { get; init; }
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
}

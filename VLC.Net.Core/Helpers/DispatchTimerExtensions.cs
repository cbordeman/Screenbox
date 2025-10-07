using System.Runtime.CompilerServices;
using Avalonia.Threading;

namespace VLC.Net.Core.Helpers;

// ReSharper disable ParameterHidesMember

public static class DispatchTimerExtensions
{
    static readonly ConditionalWeakTable<DispatcherTimer, DebounceHelper> DebounceMap =
        new ConditionalWeakTable<DispatcherTimer, DebounceHelper>();

    public static void Debounce(this DispatcherTimer timer, Action action, 
        TimeSpan interval, bool immediate = false)
    {
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(action);

        if (!DebounceMap.TryGetValue(timer, out var helper))
        {
            helper = new DebounceHelper(timer);
            DebounceMap.Add(timer, helper);
        }

        helper.Debounce(action, interval, immediate);
    }

    public class DebounceHelper
    {
        readonly DispatcherTimer timer;
        Action? action;
        bool immediate;
        bool hasPendingInvoke;

        public DebounceHelper(DispatcherTimer timer)
        {
            this.timer = timer;
            this.timer.Tick += Timer_Tick;
        }

        public void Debounce(Action action, TimeSpan interval, bool immediate)
        {
            this.action = action;
            this.immediate = immediate;

            timer.Stop();
            timer.Interval = interval;

            if (immediate && !hasPendingInvoke)
            {
                this.action?.Invoke();
                hasPendingInvoke = true;
            }

            timer.Start();
        }

        void Timer_Tick(object? sender, EventArgs e)
        {
            timer.Stop();

            if (!immediate)
                action?.Invoke();

            hasPendingInvoke = false;
        }
    }
}
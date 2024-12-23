﻿// From https://stackoverflow.com/a/47933557

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownEditor2022
{
    internal class Debouncer
    {
        private List<CancellationTokenSource> _stepperCancelTokens = new();
        private readonly int _millisecondsToWait;
        private readonly object _lockThis = new(); // Use a locking object to prevent the debouncer to trigger again while the func is still running

        public Debouncer(int millisecondsToWait = 300)
        {
            _millisecondsToWait = millisecondsToWait;
        }

        public void Debounce(Action func)
        {
            CancelAllStepperTokens();
            CancellationTokenSource newTokenSrc = new();

            lock (_lockThis)
            {
                _stepperCancelTokens.Add(newTokenSrc);
            }

            _ = Task.Delay(_millisecondsToWait, newTokenSrc.Token).ContinueWith(task => // Create new request
            {
                if (!newTokenSrc.IsCancellationRequested) // if it has not been cancelled
                {
                    CancelAllStepperTokens(); // Cancel any that remain (there shouldn't be any)
                    _stepperCancelTokens = new();

                    lock (_lockThis)
                    {
                        func(); // run
                    }
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void CancelAllStepperTokens()
        {
            foreach (CancellationTokenSource token in _stepperCancelTokens)
            {
                if (!token.IsCancellationRequested)
                {
                    token.Cancel();
                }
            }
        }
    }
}

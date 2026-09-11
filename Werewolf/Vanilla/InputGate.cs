using System;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    internal static class InputGate
    {
        private static bool _errorLogged;

        internal static bool KeysFree
        {
            get
            {
                try
                {
                    return SemiFunc.NoTextInputsActive();
                }
                catch (Exception e)
                {
                    if (!_errorLogged)
                    {
                        _errorLogged = true;
                        WLog.Line("input_gate_error", secret: false, ("err", e.Message));
                    }
                    return true;
                }
            }
        }
    }
}

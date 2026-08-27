namespace Werewolf.Vanilla
{
    internal static class InputGate
    {
        internal static bool KeysFree
        {
            get
            {
                try
                {
                    return SemiFunc.NoTextInputsActive();
                }
                catch
                {
                    return true;
                }
            }
        }
    }
}

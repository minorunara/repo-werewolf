#nullable enable
using System;

namespace Werewolf.Core
{
    internal sealed class FaultIsolatedTickPipeline
    {
        internal sealed class Stage
        {
            internal Stage(string name, Action<long> tick)
            {
                if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("stage name is required", nameof(name));
                Name = name;
                Tick = tick ?? throw new ArgumentNullException(nameof(tick));
            }

            internal string Name { get; }
            internal Action<long> Tick { get; }
        }

        private readonly Stage[] _stages;

        internal FaultIsolatedTickPipeline(params Stage[] stages)
        {
            if (stages == null) throw new ArgumentNullException(nameof(stages));
            _stages = (Stage[])stages.Clone();
            for (int i = 0; i < _stages.Length; i++)
            {
                if (_stages[i] == null)
                    throw new ArgumentException("stages must not contain null", nameof(stages));
            }
        }

        internal void Tick(long now)
        {
            for (int i = 0; i < _stages.Length; i++)
            {
                Stage stage = _stages[i];
                try
                {
                    stage.Tick(now);
                }
                catch (Exception e)
                {
                    WLog.Exception("tick_error", e, secret: false, ("stage", stage.Name));
                }
            }
        }
    }
}

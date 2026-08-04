using System;

namespace Werewolf.Core
{
    public sealed class GazeConeLock
    {
        public const float ConeDegrees = 15f;

        private readonly float _cosThreshold;
        private bool _hasRef;
        private float _refX, _refY, _refZ;

        public GazeConeLock(float coneDegrees = ConeDegrees)
        {
            float deg = coneDegrees > 0f ? coneDegrees : ConeDegrees;
            _cosThreshold = (float)Math.Cos(deg * Math.PI / 180.0);
        }

        public bool Update(float dirX, float dirY, float dirZ)
        {
            float len = (float)Math.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
            if (len < 1e-6f)
            {
                _hasRef = false;
                return false;
            }
            float x = dirX / len;
            float y = dirY / len;
            float z = dirZ / len;
            if (_hasRef && x * _refX + y * _refY + z * _refZ >= _cosThreshold)
            {
                return true;
            }
            _refX = x;
            _refY = y;
            _refZ = z;
            _hasRef = true;
            return false;
        }

        public void Reset()
        {
            _hasRef = false;
        }
    }
}

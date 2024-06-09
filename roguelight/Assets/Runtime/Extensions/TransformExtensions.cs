using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace UnityEngine
{
    public static class TransformExtensions
    {
        /// <summary>
        /// Set XY of Transform position
        /// </summary>
        /// <param name="me"></param>
        /// <param name="xy"></param>
        public static void Xy(this Transform me, float2 xy)
        {
            me.position = new float3(xy, me.position.z);
        }

        /// <summary>
        /// Set XY of Transform position
        /// </summary>
        /// <param name="me"></param>
        /// <param name="xyz"></param>
        public static void Xy(this Transform me, float3 xyz)
        {
            me.position = new float3(xyz.xy, me.position.z);
        }

        /// <summary>
        /// Set Y of Transofrm position
        /// </summary>
        /// <param name="me"></param>
        /// <param name="y"></param>
        public static void Z(this Transform me, float z)
        {
            me.position = new float3(((float3)me.position).xy, z);
        }

        public static void LocalZ(this Transform me, float z)
        {
            me.localPosition = new float3(((float3)me.position).xy, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RotateTowards(this Transform self, float2 to, float mod = 1f)
        {
            var direction = math.normalize((to - ((float3)self.position).xy));
            var angle = math.atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var eulerAngles = Vector3.forward * angle;

            if (math.abs(self.localEulerAngles.z - eulerAngles.z) > 0.3f)
            {
                self.localRotation = Quaternion.Lerp(self.localRotation, Quaternion.Euler(eulerAngles), Time.deltaTime * mod);
                return false;
            }

            return true;
        }
    }
}

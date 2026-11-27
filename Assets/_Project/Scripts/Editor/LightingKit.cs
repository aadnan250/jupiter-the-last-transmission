using UnityEngine;
using UnityEngine.Rendering;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// The lighting pass for interiors. Lights stay real-time on purpose: the blackout, the alarms and the
    /// flicker change them while you play. What the rooms lacked was reflections; a box-projected reflection
    /// probe per room, captured once when the level starts, puts the room's own light back into metal, glass
    /// and visors at almost no cost (one 128 px capture).
    /// </summary>
    public static class LightingKit
    {
        public static ReflectionProbe Probe(Transform parent, string name, Vector3 center, Vector3 size, float intensity = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var p = go.AddComponent<ReflectionProbe>();
            p.mode = ReflectionProbeMode.Realtime;
            p.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            p.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            p.resolution = 128;
            p.size = size;
            p.boxProjection = true;
            p.intensity = intensity;
            p.blendDistance = 1f;
            p.hdr = true;
            p.shadowDistance = 0f;
            p.clearFlags = ReflectionProbeClearFlags.Skybox;
            return p;
        }
    }
}

using UnityEngine;

namespace Jupiter.Creatures
{
    /// <summary>
    /// A vent in the ice full of Needles. When released (the siege at Halcyon), it lets them out one by one.
    /// The crawlers are built inactive as children; this wakes them in turn.
    /// </summary>
    public class NeedleSwarm : MonoBehaviour
    {
        [SerializeField] NeedleCrawler[] needles;
        [SerializeField] float interval = 1.6f;
        [SerializeField] float startDelay = 6f;

        float t = -1f;
        int sent;

        public bool Released => t >= 0f;
        public int Sent => sent;
        public NeedleCrawler[] Needles => needles;

        void Awake()
        {
            if (needles != null) foreach (var n in needles) if (n != null) n.gameObject.SetActive(false);
        }

        public void Release() { if (!Released) t = 0f; }

        void Update()
        {
            if (!Released || needles == null || sent >= needles.Length) return;
            t += Time.deltaTime;
            while (sent < needles.Length && t >= startDelay + sent * interval)
            {
                var n = needles[sent++];
                if (n == null) continue;
                Vector2 jitter = Random.insideUnitCircle * 0.8f;
                n.Release(transform.position + new Vector3(jitter.x, 0f, jitter.y));
            }
        }
    }
}

using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// Pneumatic sliding door. Interact to open or close. The moving panel is a child transform
    /// that slides along its local X axis. Later phases will add power and lock states.
    /// </summary>
    public class SlidingDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] Transform panel;
        [SerializeField] Vector3 openOffset = new Vector3(1.6f, 0f, 0f);
        [SerializeField] float speed = 2.5f;
        [SerializeField] bool startsOpen;

        Vector3 closedLocalPos;
        bool open;

        public bool IsOpen => open;
        public string Prompt => open ? "Close" : "Open";

        void Awake()
        {
            if (panel == null && transform.childCount > 0) panel = transform.GetChild(0);
            if (panel != null) closedLocalPos = panel.localPosition;
            open = startsOpen;
            if (panel != null && open) panel.localPosition = closedLocalPos + openOffset;
        }

        public bool CanInteract(GameObject interactor) => panel != null;

        public void Interact(GameObject interactor) => open = !open;

        void Update()
        {
            if (panel == null) return;
            Vector3 target = open ? closedLocalPos + openOffset : closedLocalPos;
            panel.localPosition = Vector3.MoveTowards(panel.localPosition, target, speed * Time.deltaTime);
        }
    }
}

using UnityEngine;
using TMPro;

namespace NekoLegends
{
    public class DemoScenes : MonoBehaviour
    {
        [Header("Demo UI")]
        [SerializeField] protected TextMeshProUGUI descriptionText;

        [Header("Background")]
        [SerializeField] protected Transform BGTransform;

        public TextMeshProUGUI DescriptionText
        {
            get { return descriptionText; }
        }

        protected virtual void OnEnable()
        {

        }

        protected virtual void OnDisable()
        {

        }

        protected virtual void Start()
        {

        }
    }
}
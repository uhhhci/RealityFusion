using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MRCozmo
{
    public class DebugPanelManager : MonoBehaviour
    {
        public GameObject DebugPanel;
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void ToggleDebugPanle()
        {
            if (DebugPanel != null)
            {
                bool isActive = DebugPanel.active;
                DebugPanel.active = !isActive;
            }
        }
    }
}
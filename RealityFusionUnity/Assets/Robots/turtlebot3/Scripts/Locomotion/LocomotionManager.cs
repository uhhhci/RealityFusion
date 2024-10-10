using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using turtlebot;
using Microsoft.MixedReality.Toolkit.Input;

namespace turtlebot.control
{
    public class LocomotionManager : MonoBehaviour
    {
        [SerializeField]
        NavigationMethod m_navMethod;
        public NavigationMethod NavMethod { get => m_navMethod; set => m_navMethod = value; }
        public GameObject Floor;

        GameObject manualController;
        GameObject goToController;

        void Start()
        {
            manualController = transform.Find("ManualController").gameObject;
            goToController = transform.Find("GoToController").gameObject;
            UpdateNavMethod(m_navMethod);
            if(Floor == null)
            {
                Floor = GameObject.Find("/[ENV]/Floor/Quad");
            }
        }

        public void UpdateNavMethod(NavigationMethod method)
        {
            TurtlebotGoToNavigation nav = goToController.GetComponent<TurtlebotGoToNavigation>();

            if (method == NavigationMethod.Manual)
            {
                manualController.SetActive(true);
                goToController.SetActive(false);
                Floor.GetComponent<PointerHandler>().OnPointerClicked.RemoveAllListeners();
            }

            if (method == NavigationMethod.GoTo)
            {
                goToController.SetActive(true);
                manualController.SetActive(false);
                Floor.GetComponent<PointerHandler>().OnPointerClicked.AddListener(nav.AddGoalPoint);

            }

        }
        // Update is called once per frame
        void Update()
        {

        }
    }
}
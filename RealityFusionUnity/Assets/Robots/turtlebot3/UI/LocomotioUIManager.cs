using System.Collections;
using System.Collections.Generic;
using turtlebot.control;
using UnityEngine;
using turtlebot;
using Microsoft.MixedReality.Toolkit;

namespace turtlebot.UI
{
    public class LocomotioUIManager : MonoBehaviour
    {
        public LocomotionManager locomotionManager;
        // Start is called before the first frame update
        bool isMenuOpen = true;
        GameObject ButtonCollection;
        GameObject NearMenuBackPlate;
        GameObject ManualButtonBackPlate;
        GameObject GoToButtonBackPlate;

        void Start()
        {
            ButtonCollection = transform.Find("NearMenu/ButtonCollection").gameObject;
            NearMenuBackPlate = transform.Find("NearMenu/Backplate").gameObject;

            ManualButtonBackPlate = ButtonCollection.transform.Find("Manual/BackPlate").gameObject;
            GoToButtonBackPlate = ButtonCollection.transform.Find("GoTo/BackPlate").gameObject;

        }

        public void ToggleCollapse()
        {
            isMenuOpen = !isMenuOpen;
            ButtonCollection.SetActive(isMenuOpen);
            NearMenuBackPlate.SetActive(isMenuOpen);
        }
        public void SetManualTeleoperation()
        {
            locomotionManager.UpdateNavMethod(NavigationMethod.Manual);
            ManualButtonBackPlate.gameObject.SetActive(true);
            GoToButtonBackPlate.gameObject.SetActive(false);
        }
        public void SetGoToNavigation()
        {
            locomotionManager.UpdateNavMethod(NavigationMethod.GoTo);
            ManualButtonBackPlate.gameObject.SetActive(false);
            GoToButtonBackPlate.gameObject.SetActive(true);
        }
    }
}
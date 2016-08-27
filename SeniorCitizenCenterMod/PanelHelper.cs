using System;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace DormitoryMod {
    public class PanelHelper : ThreadingExtensionBase {

        public const bool LOG_CUSTOM_PANELS = false;
        private const bool LOG_PANEL_HELPER = false;

        public const string INFO_PANEL_NAME = "CityServiceWorldInfoPanel";
        public const string STATS_PANEL_NAME = "StatsPanel";
        public const string STATS_INFO_PANEL_NAME = "Info";
        public const string INFO_GROUP_PANEL_NAME = "InfoGroupPanel";
        public const string UPKEEP_LABEL_NAME = "Upkeep";

        bool initialized = false;
        private static bool replacedEducationGroupPanel = false;

        float originalPanelHeight = 0.0f;
        public static Color32 originalUpkeepColor;

        public override void OnBeforeSimulationTick() {
            this.handleBuildingInfoPanel();
        }

        private void handleBuildingInfoPanel() {
            UIComponent infoPanel = UIView.library.Get(INFO_PANEL_NAME);
            if (!this.initialized) {
                // Make sure the component is loaded before attempting initilization 
                if (infoPanel == null) {
                    Logger.logInfo(LOG_PANEL_HELPER, "PanelHelper.handleBuildingInfoPanel: Can't Inilitize yet because the component is still null");
                    return;
                }

                // Can start initilization
                Logger.logInfo(LOG_PANEL_HELPER, "PanelHelper.handleBuildingInfoPanel: Attempting Initilization");

                // Init the original panel height
                this.originalPanelHeight = infoPanel.height;
                Logger.logInfo(LOG_PANEL_HELPER, "PanelHelper.handleBuildingInfoPanel: Original Panel Height Detected: {0}", this.originalPanelHeight);

                // Ensure the original height is > 1 to consider this initilized
                if (this.originalPanelHeight > 1) {
                    
                    // Set the Stats Panel to a perminently larger and higher configuration
                    UIComponent statsPanel = infoPanel.Find(STATS_PANEL_NAME);
                    if(statsPanel == null) {
                        return;
                    }

                    if (statsPanel.height < 124) {
                        statsPanel.height = 125f;

                        Vector3 position = ((UIPanel) statsPanel).position;
                        position.y = position.y + 40;
                        ((UIPanel) statsPanel).position = position;
                    }

                    // Set the Stats Info panel to a perminently larger size
                    UIComponent statsInfoPanel = statsPanel.Find(STATS_INFO_PANEL_NAME);
                    if(statsInfoPanel == null) {
                        return;
                    }

                    if (statsInfoPanel.height < 119) {
                        statsInfoPanel.height = 120f;
                    }

                    // Get the original color of the Upkeep Label
                    UIComponent infoGroupPanel = infoPanel.Find(PanelHelper.INFO_GROUP_PANEL_NAME);
                    if (infoGroupPanel == null) {
                        return;
                    }

                    UILabel upkeepLabel = infoGroupPanel.Find<UILabel>(PanelHelper.UPKEEP_LABEL_NAME);
                    if (upkeepLabel == null) {
                        return;
                    }

                    PanelHelper.originalUpkeepColor = upkeepLabel.textColor;

                    Logger.logInfo(LOG_PANEL_HELPER, "PanelHelper.handleBuildingInfoPanel: Done Initilizing");
                    this.initialized = true;
                }
                return;
            }

            // Check to see if the panel height should be reset
            if (infoPanel != null && !infoPanel.isVisible && Math.Abs(this.originalPanelHeight - infoPanel.height) > 1) {
                infoPanel.height = this.originalPanelHeight;
                Logger.logInfo(LOG_PANEL_HELPER, "PanelHelper.handleBuildingInfoPanel: Reset panel height back to: {0}", infoPanel.height);

                // Also reset the Upkeep Color
                UIComponent infoGroupPanel = infoPanel.Find(PanelHelper.INFO_GROUP_PANEL_NAME);
                if (infoGroupPanel != null) {
                    UILabel upkeepLabel = infoGroupPanel.Find<UILabel>(PanelHelper.UPKEEP_LABEL_NAME);
                    if (upkeepLabel != null) {
                        upkeepLabel.textColor = PanelHelper.originalUpkeepColor;
                        Logger.logInfo(LOG_PANEL_HELPER, "PanelHelper.handleBuildingInfoPanel: Reset upkeep color back to: {0}", upkeepLabel.textColor);
                    }
                }
            }
        }

        public static void reset() {
            // Reset the values needed for panel initilization, not everything needs to be re-initilized, but the healthcare menu does
            replacedEducationGroupPanel = false;
        }

        public static bool initCustomEducationGroupPanel() {

            // Get the Tab Strip, but fetching it before it's initlized can throw an exception
            UITabstrip strip = null;
            try {
                strip = ToolsModifierControl.mainToolbar?.component as UITabstrip;
            } catch {
                // Do nothing
            }

            // Get the other needed components
            UIComponent education = strip?.Find(CustomEducationGroupPanel.EDUCATION_NAME);
            UIComponent healthcarePanelComp = strip?.tabPages?.Find(CustomEducationGroupPanel.EDUCATION_PANEL_NAME);
            EducationGroupPanel healthcareGroupPanel = healthcarePanelComp?.GetComponent<EducationGroupPanel>();

            // Ensure the Education Components are available before initilization
            if (education == null || healthcarePanelComp == null || healthcareGroupPanel == null || !education.isActiveAndEnabled || !education.isVisible) {
                Logger.logInfo(LOG_CUSTOM_PANELS, "PanelHelper.initCustomEducationGroupPanel -- Waiting to initilize Education Menu because the components aren't ready");
                return false;
            }

            // Can start initilization
            Logger.logInfo(LOG_CUSTOM_PANELS, "PanelHelper.initCustomEducationGroupPanel -- Initilizing Education Menu");

            // Check the Education Group Panel and replace it with a Custom Education Group Panel
            if (!(healthcareGroupPanel is CustomEducationGroupPanel)) {
                if (replacedEducationGroupPanel) {
                    Logger.logInfo(LOG_CUSTOM_PANELS, "PanelHelper.initCustomEducationGroupPanel -- Waiting to continue initilization of the Education Menu because the Custom Panel isn't fully initilized yet");
                    return false;
                }

                // Destroy the existing group panel
                Logger.logInfo(LOG_CUSTOM_PANELS, "PanelHelper.initCustomEducationGroupPanel -- Destroying the existing Education Group Panel: {0}", healthcareGroupPanel);
                UnityEngine.Object.Destroy(healthcareGroupPanel);

                // Create a new custom group panel
                Logger.logInfo(LOG_CUSTOM_PANELS, "PanelHelper.initCustomEducationGroupPanel -- Creating the new Custom Education Group Panel");
                healthcarePanelComp.gameObject.AddComponent(typeof (CustomEducationGroupPanel));

                // Mark this step as complete and bail to give this step a chance to complete
                replacedEducationGroupPanel = true;
                return false;
            }

            // Attempt initilization of the Custom Education Group Panel -- Will take multiple attempts to completely initilize
            Logger.logInfo(LOG_CUSTOM_PANELS, "PanelHelper.initCustomEducationGroupPanel -- Attempting initilization of the Custom Education Group Panel");
            return ((CustomEducationGroupPanel) healthcareGroupPanel).initDormitories();
            
        }
        
    }
}
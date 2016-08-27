using System;
using System.Threading;
using ColossalFramework;
using ColossalFramework.UI;

namespace DormitoryMod {
    public class CustomEducationGroupPanel : EducationGroupPanel {

        private static readonly string SPRITE_BASE = "SubBar";

        public static readonly string EDUCATION_NAME = "Education";
        public static readonly string EDUCATION_PANEL_NAME = "EducationPanel";
        private static readonly string EDUCATION_COMPONENT_NAME = "EducationDefault";

        private static readonly string DORMITORY_NAME = "Dormitory";
        private static readonly string DORMITORY_COMPONENT_NAME = "DormitoryDefault";

        private bool hasStartedInit = false;
        private bool replacedEducationPanel = false;
        private bool replacedDormitoryComponent = false;
        private bool replacedDormitoryPanel = false;

        private int refreshing = 0;

        public override ItemClass.Service service {
            get {
                return ItemClass.Service.Education;
            }
        }

        public bool initDormitories() {
            Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories");

            /*
             * Multi-step initilization process, perform each step one at a time and wait for it to completely finish before moving on.
             * If an object isn't allowed to initilize complete, interacting with it can crash the game.
             */

            // First refresh the default panels before starting
            if (!this.hasStartedInit) {
                this.internalRefreshPanel(false);
                this.hasStartedInit = true;
            }
            
            // 1) Check the Education Component and replace the Panel with a custom one that will exclude Dormitories
            UIComponent educationComponent = this.m_Strip.Find(EDUCATION_COMPONENT_NAME);
            GeneratedScrollPanel educationPanel = this.m_Strip.GetComponentInContainer(educationComponent, typeof(GeneratedScrollPanel)) as GeneratedScrollPanel;
            if (!(educationPanel is CustomEducationPanel)) {
                // Check to make sure this step is only done once, if attempting more than once, then just bail to give the process more time to finish
                if (this.replacedEducationPanel) {
                    Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories -- Waiting for replacement of the Education Panel to complete");
                    return false;
                }

                // Destroy the existing component
                Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories -- Destroying existing Education Panel: {0}", educationPanel);
                Destroy(educationPanel);

                // Set the new component
                Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories -- Creating new Custom Education Panel");
                UIComponent healthcarePanelContainer = this.m_Strip.tabPages.components[educationComponent.zOrder];
                healthcarePanelContainer.gameObject.AddComponent<CustomEducationPanel>();

                // Mark this step as complete and bail to ensure this step is allowed to finish
                Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories -- Bailing after finishing setting the Custom Education Panel");
                this.replacedEducationPanel = true;
                return false;
            }

            // 2) Check the Dormitory Component and create it if it's not present
            UIComponent dormitoryComponent = this.m_Strip.Find(DORMITORY_COMPONENT_NAME);
            if (dormitoryComponent == null) {
                // Check to make sure this step is only done once, if attempting more than once, then just bail to give the process more time to finish
                if (this.replacedDormitoryComponent) {
                    Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories -- Waiting for replacement of the Dormitory Component to complete");
                    return false;
                }

                // Create the new tab for the Dormitory
                Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories -- Creating new Dormitory Tab");
                this.SpawnButtonEntry(this.m_Strip, DORMITORY_NAME, DORMITORY_COMPONENT_NAME, true, null, SPRITE_BASE, true, false);
                
                // Mark this step as complete and bail to ensure this step is allowed to finish
                Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories -- Bailing after finished creating new Dormitory tab");
                this.replacedDormitoryComponent = true;
                return false;
            }

            // 3) Check the Dormitory Panel and create it if it's not present
            GeneratedScrollPanel dormitoryPanel = this.m_Strip.GetComponentInContainer(dormitoryComponent, typeof(DormitoryPanel)) as GeneratedScrollPanel;
            if (dormitoryPanel == null) {
                // Check to make sure this step is only done once, if attempting more than once, then just bail to give the process more time to finish
                if (this.replacedDormitoryPanel) {
                    Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories -- Waiting for replacement of the Dormitory Panel to complete");
                    return false;
                }

                // Create the new Dormitory Panel
                UIComponent dormitoryPanelContainer = this.m_Strip.tabPages.components[dormitoryComponent.zOrder];
                Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories -- Setting Panel for: {0}", dormitoryPanelContainer);
                dormitoryPanelContainer.gameObject.AddComponent<DormitoryPanel>();

                // Mark this step as complete and bail to ensure this step is allowed to finish
                Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories -- Bailing after setting Nusing Home Panel");
                this.replacedDormitoryPanel = true;
                return false;
            }

            // Ensure there are 2 tabs present now
            if (this.m_Strip.childCount != 2) {
                Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories -- Bailing because the number of tabs was expected to be 2 but was {0} -- Components detected:", this.m_Strip.childCount);
                foreach (UIComponent comp in this.m_Strip.components) {
                    Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories -- Child Component: {0}", comp);
                }
                return false;
            }
            
            // Remove all children from the Education Panel so it can be repopulated by the new panel logic -- Note: May take more than one iteration to remove them all
            if (educationPanel.childComponents.Count > 0) {
                ((CustomEducationPanel) educationPanel).removeAllChildren();
                return false;
            }
            
            // Before finishing, refresh the panel
            this.RefreshPanel();

            Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.initDormitories -- Done Initing");
            return true;
        }

        protected override bool CustomRefreshPanel() {
            return this.internalRefreshPanel(true);
        }

        private bool internalRefreshPanel(bool refreshCustoms) {
            Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.CustomRefreshPanel");
            if (Interlocked.CompareExchange(ref this.refreshing, 1, 0) == 1) {
                Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.CustomRefreshPanel -- Can't refresh, already refreshing");
                return true;
            }


            // Refresh the Custom Panels only when specified -- Can't refresh these panels before completing the init process
            if (refreshCustoms) {
                try {
                    // Refresh the Education Panel
                    UIComponent healthcareDefault = this.m_Strip.Find(EDUCATION_COMPONENT_NAME);
                    GeneratedScrollPanel healthcarePanel = this.m_Strip.GetComponentInContainer(healthcareDefault, typeof (CustomEducationPanel)) as GeneratedScrollPanel;
                    if (healthcarePanel != null) {
                        Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.CustomRefreshPanel -- Refreshing the Education Panel");
                        healthcarePanel.RefreshPanel();
                    }

                    // Refresh the Dormitory Panel
                    UIComponent dormitoryDefault = this.m_Strip.Find(DORMITORY_COMPONENT_NAME);
                    GeneratedScrollPanel dormitoryPanel = this.m_Strip.GetComponentInContainer(dormitoryDefault, typeof (DormitoryPanel)) as GeneratedScrollPanel;
                    if (dormitoryPanel != null) {
                        Logger.logInfo(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.CustomRefreshPanel -- Refreshing the Dormitory Panel");
                        dormitoryPanel.RefreshPanel();
                    }
                } catch (Exception e) {
                    Logger.logError(PanelHelper.LOG_CUSTOM_PANELS, "CustomEducationGroupPanel.CustomRefreshPanel -- Exception refreshing the Custom Panels: {0} -- {1}", e, e.StackTrace);
                    this.refreshing = 0;
                    return true;
                }
            }

            // Proceed with the existing logic
            if (this.groupFilter != GeneratedGroupPanel.GroupFilter.None) {
                this.PopulateGroups(this.groupFilter, this.sortingMethod);
            } else if (!string.IsNullOrEmpty(this.serviceName)) {
                this.DefaultGroup(this.serviceName);
            } else {
                this.DefaultGroup(EnumExtensions.Name<ItemClass.Service>(this.service));
            }

            this.refreshing = 0;
            return true;
        }

    }
}
namespace DormitoryMod {
    public class CustomEducationPanel : CustomBasePanel {

        protected override bool IsServiceValid(BuildingInfo info) {
            // Service is only valid for Education Buildings without the DormitoryAi
            return info != null && info.GetService() == this.service && !(info.m_buildingAI is DormitoryAi);
        }

    }
}
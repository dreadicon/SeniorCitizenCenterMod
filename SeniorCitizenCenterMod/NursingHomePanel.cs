namespace DormitoryMod {
    public class DormitoryPanel : CustomBasePanel {

        protected override bool IsServiceValid(BuildingInfo info) {
            // Service is only valid for Education Buildings with the DormitoryAi
            return info != null && info.m_buildingAI is DormitoryAi;
        }

    }
}
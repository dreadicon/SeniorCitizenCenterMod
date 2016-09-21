using ICities;
using UnityEngine;


namespace DormitoryMod {

    public class DormitoryMod : LoadingExtensionBase, IUserMod, ISerializableData {
        private const bool LOG_BASE = true;

        private static DormitoryMod instance;

        private GameObject dormitoryInitializerObj;
        private DormitoryInitializer dormitoryInitializer;
        private OptionsManager optionsManager = new OptionsManager();

        public string Description {
            get {
                return "Enables functionality for Dormitory Assets to function as working Dormitories.";
            }
        }

        public string Name {
            get {
                return "DormitoryMod";
            }
        }

        public static DormitoryMod getInstance() {
            return instance;
        }

        public DormitoryInitializer getDormitoryInitializer() {
            return this.dormitoryInitializer;
        }

        public OptionsManager getOptionsManager() {
            return this.optionsManager;
        }

        public void OnSettingsUI(UIHelperBase helper) {
            this.optionsManager.initialize(helper);
            this.optionsManager.loadOptions();
        }

        public override void OnCreated(ILoading loading) {
            Logger.logInfo(LOG_BASE, "DormitoryMod Created");
            instance = this;
            base.OnCreated(loading);

            if (this.dormitoryInitializerObj != null) {
                return;
            }
            
            this.dormitoryInitializerObj = new GameObject("DormitoryMod Dormitories");
            this.dormitoryInitializer = this.dormitoryInitializerObj.AddComponent<DormitoryInitializer>();
        }

        public override void OnLevelUnloading() {
            base.OnLevelUnloading();
            this.dormitoryInitializer.OnLevelUnloading();
        }

        public override void OnLevelLoaded(LoadMode mode) {
            Logger.logInfo(LOG_BASE, "DormitoryMod Level Loaded: {0}", mode);
            base.OnLevelLoaded(mode);
            if(mode == LoadMode.LoadGame) {
                this.dormitoryInitializer.OnLevelWasLoaded(DormitoryInitializer.LOADED_LEVEL_GAME);
            }
        }

        public override void OnReleased() {
            Logger.logInfo(LOG_BASE, "DormitoryMod Released");
            base.OnReleased();
            if (this.dormitoryInitializerObj != null) {
                UnityEngine.Object.Destroy(this.dormitoryInitializerObj);
            }
        }

        public byte[] LoadData(string id) {
            Logger.logInfo(Logger.LOG_OPTIONS, "Load Data: {0}", id);
            return null;
        }

        public void SaveData(string id, byte[] data) {
            Logger.logInfo(Logger.LOG_OPTIONS, "Save Data: {0} -- {1}", id, data);
        }

        public IManagers managers { get; set; }
        public string[] EnumerateData() { return null; }
        public void EraseData(string id) { }
        public bool LoadGame(string saveName) { return false; }
        public bool SaveGame(string saveName) { return false; }
    }
}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ColossalFramework;
using UnityEngine;

namespace DormitoryMod {
    public class DormitoryInitializer : MonoBehaviour {
        private const bool LOG_INITIALIZER = true;

        public const int LOADED_LEVEL_GAME = 6;
        public const int LOADED_LEVEL_ASSET_EDITOR = 19;

        private const String COLLEGE_NAME = "College";

        private static readonly Queue<IEnumerator> ACTION_QUEUE = new Queue<IEnumerator>();
        private static readonly object QUEUE_LOCK = new object();

        private readonly AiReplacementHelper aiReplacementHelper = new AiReplacementHelper();
        private int attemptingInitialization;
        private int numTimesSearchedForMedicalClinic = 0;

        private bool initialized;
        private int loadedLevel = -1;

        private void Awake() {
            // Specify that this object should not be destroyed
            // Without this statement this object would be cleaned up very quickly
            DontDestroyOnLoad(this);
        }

        private void Start() {
            Logger.logInfo(LOG_INITIALIZER, "DormitoryInitializer Starting");
        }

        public void OnLevelWasLoaded(int level) {
            this.loadedLevel = level;
            Logger.logInfo(LOG_INITIALIZER, "DormitoryInitializer.OnLevelWasLoaded: {0}", level);
        }

        public void OnLevelUnloading() {
            this.loadedLevel = -1;
            Logger.logInfo(LOG_INITIALIZER, "DormitoryInitializer.OnLevelUnloading: {0}", this.loadedLevel);
        }

        public int getLoadedLevel() {
            return this.loadedLevel;
        }

        private void Update() {
            if (!this.initialized) {
                // Still need initilization, check to see if already attempting initilization
                // Note: Not sure if it's possible for this method to be called more than once at a time, but locking just in case
                if (Interlocked.CompareExchange(ref this.attemptingInitialization, 1, 0) == 0) {
                    this.attemptInitialization();
                }
            }
        }

        private void attemptInitialization() {
            // Make sure not attempting initilization after loading has already completed -- This means the mod may not function properly, but it won't waste resources continuing to try
            if (Singleton<LoadingManager>.instance.m_loadingComplete) {
                Logger.logError("DormitoryInitializer.attemptInitialization -- *** NURSING HOMES FUNCTIONALITY DID NOT INITLIZIE PRIOR TO GAME LOADING -- THE SENIOR CITIZEN CENTER MOD MAY NOT FUNCTION PROPERLY ***");
                // Set initilized so it won't keep trying
                this.setInitialized();
            }

            // Check to see if initilization can start
            if (PrefabCollection<BuildingInfo>.LoadedCount() <= 0) {
                this.attemptingInitialization = 0;
                return;
            }

            // Wait for the Medical Clinic or other HospitalAI Building to load since all new Dormitories will copy its values
            BuildingInfo medicalBuildingInfo = this.findEducationBuildingInfo();
            if (medicalBuildingInfo == null) {
                this.attemptingInitialization = 0;
                return;
            }

            // Start loading
            Logger.logInfo(LOG_INITIALIZER, "DormitoryInitializer.attemptInitialization -- Attempting Initialization");
            Singleton<LoadingManager>.instance.QueueLoadingAction(ActionWrapper(() => {
                try {
                    if (this.loadedLevel == LOADED_LEVEL_GAME) {
                        // Reset the PanelHelper and initilize the Education Menu
                        PanelHelper.reset();
                        this.StartCoroutine(this.initEducationMenu());
                    }
                    if (this.loadedLevel == LOADED_LEVEL_GAME || this.loadedLevel == LOADED_LEVEL_ASSET_EDITOR) {
                        this.StartCoroutine(this.initDormitories(medicalBuildingInfo));
                        AddQueuedActionsToLoadingQueue();
                    }
                } catch (Exception e) {
                    Logger.logError("Error loading prefabs: {0}", e.Message);
                }
            }));

            // Set initilized
            this.setInitialized();
        }

        private void setInitialized() {
            this.initialized = true;
            this.attemptingInitialization = 0;
            this.numTimesSearchedForMedicalClinic = 0;
        }

        private BuildingInfo findEducationBuildingInfo() {
            // First check for the known Medical Clinic
            BuildingInfo educationBuildingInfo = PrefabCollection<BuildingInfo>.FindLoaded(COLLEGE_NAME);
            if (educationBuildingInfo != null) {
                return educationBuildingInfo;
            }

            // Try 5 times to search for the Medical Clinic before giving up
            if (++this.numTimesSearchedForMedicalClinic < 5) {
                return null;
            }

            // Attempt to find a suitable medical building that can be used as a template
            Logger.logInfo(LOG_INITIALIZER, "DormitoryInitializer.findMedicalBuildingInfo -- Couldn't find the Medical Clinic asset after {0} tries, attempting to search for any Building with a HospitalAi", this.numTimesSearchedForMedicalClinic);
            for (uint i=0; (long) PrefabCollection<BuildingInfo>.LoadedCount() > (long) i; ++i) {
                BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded(i);
                if (buildingInfo != null && buildingInfo.GetService() == ItemClass.Service.Education && !buildingInfo.m_buildingAI.IsWonder() && buildingInfo.m_buildingAI is HospitalAI) {
                    Logger.logInfo(LOG_INITIALIZER, "DormitoryInitializer.findMedicalBuildingInfo -- Using the {0} as a template instead of the Medical Clinic", buildingInfo);
                    return buildingInfo;
                }
            }

            // Return null to try again next time
            return null;
        }

        private IEnumerator initEducationMenu() {
            // Need to continue beyond loading complete now
            int i = 0;
            while (!Singleton<LoadingManager>.instance.m_loadingComplete || i++ < 25) {
                if (PanelHelper.initCustomEducationGroupPanel()) {
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator initDormitories(BuildingInfo buildingToCopyFrom) {
            float capcityModifier = DormitoryMod.getInstance().getOptionsManager().getCapacityModifier();
            uint index = 0U;
            while (!Singleton<LoadingManager>.instance.m_loadingComplete) {
                for (; PrefabCollection<BuildingInfo>.LoadedCount() > index; ++index) {
                    BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded(index);
                    if (buildingInfo != null && buildingInfo.name.EndsWith("_Data") && buildingInfo.name.Contains("NH123")) {
                        this.aiReplacementHelper.replaceBuildingAi<DormitoryAi>(buildingInfo, buildingToCopyFrom);
                    }
                    if (this.loadedLevel == LOADED_LEVEL_GAME && buildingInfo != null && buildingInfo.m_buildingAI is DormitoryAi) {
                        ((DormitoryAi) buildingInfo.m_buildingAI).updateCapacity(capcityModifier);
                    }
                }
                yield return new WaitForEndOfFrame();
            }
        }
        
        private static IEnumerator ActionWrapper(Action a) {
            a();
            yield break;
        }

        private static void AddQueuedActionsToLoadingQueue() {
            LoadingManager instance = Singleton<LoadingManager>.instance;
            object obj = typeof(LoadingManager).GetFieldByName("m_loadingLock").GetValue(instance);

            while (!Monitor.TryEnter(obj, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
            }
            try {
                FieldInfo fieldByName = typeof(LoadingManager).GetFieldByName("m_mainThreadQueue");
                Queue<IEnumerator> queue1 = (Queue<IEnumerator>) fieldByName.GetValue(instance);
                if (queue1 == null) {
                    return;
                }
                Queue<IEnumerator> queue2 = new Queue<IEnumerator>(queue1.Count + 1);
                queue2.Enqueue(queue1.Dequeue());
                do
                    ; while (!Monitor.TryEnter(QUEUE_LOCK, SimulationManager.SYNCHRONIZE_TIMEOUT));
                try {
                    while (ACTION_QUEUE.Count > 0) {
                        queue2.Enqueue(ACTION_QUEUE.Dequeue());
                    }
                } finally {
                    Monitor.Exit(QUEUE_LOCK);
                }
                while (queue1.Count > 0) {
                    queue2.Enqueue(queue1.Dequeue());
                }
                fieldByName.SetValue(instance, queue2);
            } finally {
                Monitor.Exit(obj);
            }
        }
    }

    public static class TypeExtensions {
        public static IEnumerable<FieldInfo> GetAllFieldsFromType(this Type type) {
            if (type == null) {
                return Enumerable.Empty<FieldInfo>();
            }
            BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            if (type.BaseType != null) {
                return type.GetFields(bindingAttr).Concat(type.BaseType.GetAllFieldsFromType());
            }
            return type.GetFields(bindingAttr);
        }

        public static FieldInfo GetFieldByName(this Type type, string name) {
            return type.GetAllFieldsFromType().Where(p => p.Name == name).FirstOrDefault();
        }
    }
}
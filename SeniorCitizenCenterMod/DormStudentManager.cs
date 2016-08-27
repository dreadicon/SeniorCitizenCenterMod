using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using ColossalFramework;
using ColossalFramework.Math;
using ICities;

namespace DormitoryMod {
    public class DormStudentManager : ThreadingExtensionBase {
        private const bool LOG_SENIORS = false;

        private const int DEFAULT_NUM_SEARCH_ATTEMPTS = 3;

        private static DormStudentManager _instance;

        private readonly BuildingManager _buildingManager;
        private readonly CitizenManager _citizenManager;

        private readonly uint[] _familiesWithStudents;
        private readonly HashSet<uint> _seniorCitizensBeingProcessed;
        private uint _numStudentFamilies;

        private Randomizer _randomizer;

        private int _refreshTimer;
        private int _running;

        public DormStudentManager() {
            Logger.logInfo(LOG_SENIORS, "StudentManager Created");
            _instance = this;

            this._randomizer = new Randomizer((uint) 73);
            this._citizenManager = Singleton<CitizenManager>.instance;
            this._buildingManager = Singleton<BuildingManager>.instance;

            // TODO: This array size is excessive but will allow for never worrying about resizing, should consider allowing for resizing instead
            this._familiesWithStudents = new uint[CitizenManager.MAX_UNIT_COUNT];

            this._seniorCitizensBeingProcessed = new HashSet<uint>();
        }

        public static DormStudentManager GetInstance() {
            return _instance;
        }

        public override void OnBeforeSimulationTick() {
            // Refresh every every so often
            if (this._refreshTimer++ % 600 == 0) {
                // Make sure refresh can occur, otherwise set the timer so it will trigger again next try
                if (Interlocked.CompareExchange(ref this._running, 1, 0) == 1) {
                    this._refreshTimer = 0;
                    return;
                }

                // Refresh the Students Array
                this.refreshStudents();

                // Reset the timer and _running flag
                this._refreshTimer = 1;
                this._running = 0;
            }
        }

        private void refreshStudents() {
            CitizenUnit[] citizenUnits = this._citizenManager.m_units.m_buffer;
            this._numStudentFamilies = 0;

            for (uint i = 0; i < citizenUnits.Length; i++) {
                for (int j = 0; j < 5; j++) {
                    uint citizenId = citizenUnits[i].GetCitizen(j);
                    if (this.IsStudent(citizenId) && this.validateStudent(citizenId)) {
                        this._familiesWithStudents[this._numStudentFamilies++] = i;
                        break;
                    }
                }
            }
        }

        public uint[] GetFamilyWithStudent() {
            return this.GetFamilyWithStudent(DEFAULT_NUM_SEARCH_ATTEMPTS);
        }

        public uint[] GetFamilyWithStudent(int numAttempts) {
            Logger.logInfo(LOG_SENIORS, "StudentManager.getFamilyWithStudent -- Start");
            // Lock to prevent refreshing while _running, otherwise bail
            if (Interlocked.CompareExchange(ref this._running, 1, 0) == 1) {
                return null;
            }

            // Get random family that contains at least one senior
            uint[] family = this.getFamilyWithStudentInternal(numAttempts);
            if (family == null) {
                Logger.logInfo(LOG_SENIORS, "StudentManager.getFamilyWithStudent -- No Family");
                this._running = 0;
                return null;
            }

            // Mark all seniors in the family as being processed
            foreach (uint familyMember in family) {
                if (this.IsStudent(familyMember)) {
                    this._seniorCitizensBeingProcessed.Add(familyMember);
                }
            }


            Logger.logInfo(LOG_SENIORS, "StudentManager.getFamilyWithStudent -- Finished: {0}", string.Join(", ", Array.ConvertAll(family, item => item.ToString())));
            this._running = 0;
            return family;
        }

        public void DoneProcessingStudent(uint seniorCitizenId) {
            this._seniorCitizensBeingProcessed.Remove(seniorCitizenId);
        }

        private uint[] getFamilyWithStudentInternal(int numAttempts) {
            // Check to see if too many attempts already
            if (numAttempts <= 0) {
                return null;
            }

            // Get a random senior citizen
            uint familyId = this.fetchRandomFamilyWithStudent();
            Logger.logInfo(LOG_SENIORS, "StudentManager.getFamilyWithStudentInternal -- Family Id: {0}", familyId);
            if (familyId == 0) {
                // No Family with Students to be located
                return null;
            }


            // Validate all seniors in the family and build an array of family members
            CitizenUnit familyWithStudent = this._citizenManager.m_units.m_buffer[familyId];
            uint[] family = new uint[5];
            bool seniorPresent = false;
            for (int i = 0; i < 5; i++) {
                uint familyMember = familyWithStudent.GetCitizen(i);
                if (this.IsStudent(familyMember)) {
                    if (!this.validateStudent(familyMember)) {
                        // This particular Student is no longer valid for some reason, call recursively with one less attempt
                        return this.getFamilyWithStudentInternal(--numAttempts);
                    }
                    seniorPresent = true;
                }
                Logger.logInfo(LOG_SENIORS, "StudentManager.getFamilyWithStudentInternal -- Family Member: {0}", familyMember);
                family[i] = familyMember;
            }

            if (!seniorPresent) {
                // No Student was found in this family (which is a bit weird), try again
                return this.getFamilyWithStudentInternal(--numAttempts);
            }

            return family;
        }

        private uint fetchRandomFamilyWithStudent() {
            if (this._numStudentFamilies <= 0) {
                return 0;
            }

            int index = this._randomizer.Int32(this._numStudentFamilies);
            return this._familiesWithStudents[index];
        }

        public bool IsStudent(uint studentCitizenId) {
            if (studentCitizenId == 0) {
                return false;
            }

            // Validate not dead

            // Validate Age
            var citizen = this._citizenManager.m_citizens.m_buffer[studentCitizenId];
            if (Citizen.Education.TwoSchools != citizen.EducationLevel || citizen.Dead || (citizen.m_flags & Citizen.Flags.Student) == Citizen.Flags.None  ) {
                return false;
            }

            return true;
        }

        private bool validateStudent(uint seniorCitizenId) {
            // Validate this Student is not already being processed
            if (this._seniorCitizensBeingProcessed.Contains(seniorCitizenId)) {
                return false;
            }

            // Validate not homeless
            ushort homeBuildingId = this._citizenManager.m_citizens.m_buffer[seniorCitizenId].m_homeBuilding;
            if (homeBuildingId == 0) {
                return false;
            }

            // Validate not already living in a nursing home
            if (this._buildingManager.m_buildings.m_buffer[homeBuildingId].Info.m_buildingAI is DormitoryAi) {
                return false;
            }

            return true;
        }
    }
}
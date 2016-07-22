﻿using System.Linq;
using AcTools.Utils;

namespace AcManager.Tools.Helpers {
    public partial class AcSettingsHolder {
        public class GameplaySettings : IniSettings {
            public SettingEntry[] UnitsTypes { get; } = {
                new SettingEntry("0", ToolsStrings.Gameplay_Units_Metrical),
                new SettingEntry("1", ToolsStrings.Gameplay_Units_Imperial)
            };

            internal GameplaySettings() : base(@"gameplay") {}

            #region GUI
            private SettingEntry _units;

            public SettingEntry Units {
                get { return _units; }
                set {
                    if (!UnitsTypes.Contains(value)) value = UnitsTypes[0];
                    if (Equals(value, _units)) return;
                    _units = value;
                    OnPropertyChanged();
                }
            }

            private bool _allowOverlapping;

            public bool AllowOverlapping {
                get { return _allowOverlapping; }
                set {
                    if (Equals(value, _allowOverlapping)) return;
                    _allowOverlapping = value;
                    OnPropertyChanged();
                }
            }

            private bool _displayTimeGap;

            public bool DisplayTimeGap {
                get { return _displayTimeGap; }
                set {
                    if (Equals(value, _displayTimeGap)) return;
                    _displayTimeGap = value;
                    OnPropertyChanged();
                }
            }

            private bool _displayDamage;

            public bool DisplayDamage {
                get { return _displayDamage; }
                set {
                    if (Equals(value, _displayDamage)) return;
                    _displayDamage = value;
                    OnPropertyChanged();
                }
            }

            private bool _displayLeaderboard;

            public bool DisplayLeaderboard {
                get { return _displayLeaderboard; }
                set {
                    if (Equals(value, _displayLeaderboard)) return;
                    _displayLeaderboard = value;
                    OnPropertyChanged();
                }
            }

            private bool _displayMirror;

            public bool DisplayMirror {
                get { return _displayMirror; }
                set {
                    if (Equals(value, _displayMirror)) return;
                    _displayMirror = value;
                    OnPropertyChanged();
                }
            }

            private bool _displayDriverNames;

            public bool DisplayDriverNames {
                get { return _displayDriverNames; }
                set {
                    if (Equals(value, _displayDriverNames)) return;
                    _displayDriverNames = value;
                    OnPropertyChanged();
                }
            }
            #endregion

            private int _steeringWheelLimit;

            public int SteeringWheelLimit {
                get { return _steeringWheelLimit; }
                set {
                    value = value.Clamp(0, 450);
                    if (Equals(value, _steeringWheelLimit)) return;
                    _steeringWheelLimit = value;
                    OnPropertyChanged();
                }
            }

            protected override void LoadFromIni() {
                Units = Ini["OPTIONS"].GetEntry("USE_MPH", UnitsTypes);
                DisplayTimeGap = Ini["TIME_DIFFERENCE"].GetBool("IS_ACTIVE", true);
                DisplayDamage = Ini["DAMAGE_DISPLAYER"].GetBool("IS_ACTIVE", true);
                DisplayLeaderboard = Ini["OVERLAY_LEADERBOARD"].GetBool("ACTIVE", true);
                DisplayMirror = Ini["VIRTUAL_MIRROR"].GetBool("ACTIVE", true);
                DisplayDriverNames = Ini["DRIVER_NAME_DISPLAYER"].GetBool("IS_ACTIVE", false);
                AllowOverlapping = Ini["GUI"].GetBool("ALLOW_OVERLAPPING_FORMS", true);
                SteeringWheelLimit = Ini["STEER_ANIMATION"].GetInt("MAX_DEGREES", 0);
            }

            protected override void SetToIni() {
                Ini["OPTIONS"].Set("USE_MPH", Units);
                Ini["TIME_DIFFERENCE"].Set("IS_ACTIVE", DisplayTimeGap);
                Ini["DAMAGE_DISPLAYER"].Set("IS_ACTIVE", DisplayDamage);
                Ini["OVERLAY_LEADERBOARD"].Set("ACTIVE", DisplayLeaderboard);
                Ini["VIRTUAL_MIRROR"].Set("ACTIVE", DisplayMirror);
                Ini["DRIVER_NAME_DISPLAYER"].Set("IS_ACTIVE", DisplayDriverNames);
                Ini["GUI"].Set("ALLOW_OVERLAPPING_FORMS", AllowOverlapping);
                Ini["STEER_ANIMATION"].Set("MAX_DEGREES", SteeringWheelLimit);
            }
        }

        private static GameplaySettings _gameplay;

        public static GameplaySettings Gameplay => _gameplay ?? (_gameplay = new GameplaySettings());
    }
}

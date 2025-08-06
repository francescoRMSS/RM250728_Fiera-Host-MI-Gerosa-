
namespace RM.src.RM250619.Classes.PLC
{
    /// <summary>
    /// Contiene i tag degli indirizzi PLC
    /// </summary>
    public class PLCTagName
    {
        #region Scrittura a PLC

        #region Robot

        /// <summary>
        /// Clock da scrivere ogni secondo (il clock su PLC gira ogni 2s)
        /// </summary>
        public const string LifeBit_out = "PLC1_" + "LifeBit_out";

        /// <summary>
        /// Se il valore è 1, indica che la lista di lavoro del robot contiene almeno un programma
        /// </summary>
        public const string Hardware_Ready_To_Start = "PLC1_" + "Hardware_Redy_To_Start";

        /// <summary>
        /// A 1 quando quando il robot lavora in modalità automatico
        /// </summary>
        public const string Automatic_Start = "PLC1_" + "Automatic_Start";

        /// <summary>
        /// A 1 quando quando il robot ha almeno un programma i nmemoria
        /// </summary>
        public const string Program_In_Memory = "PLC1_" + "Program_In_Memory";

        /// <summary>
        /// Anno - versione robot
        /// </summary>
        public const string VersionYear = "PLC1_" + "VersionYear";

        /// <summary>
        /// Mese - versione robot
        /// </summary>
        public const string VersionMonth = "PLC1_" + "VersionMonth";

        /// <summary>
        /// Giorno - versione robot
        /// </summary>
        public const string VersionDay = "PLC1_" + "VersionDay";

        /// <summary>
        /// Errore nel sistema
        /// </summary>
        public const string System_error = "PLC1_" + "System_error";

        /// <summary>
        /// Emergenza
        /// </summary>
        public const string Emergency = "PLC1_" + "Emergency";

        /// <summary>
        /// Posizione sicura del robot
        /// </summary>
        public const string SafePos = "PLC1_" + "SafePos";

        /// <summary>
        /// Ciclo auto terminato
        /// </summary>
        public const string Auto_Cycle_End = "PLC1_" + "Auto_Cycle_End";

        /// <summary>
        /// A 1 quando il Robot si trova in zona di home
        /// </summary>
        public const string ACT_Zone_Home_inPos = "PLC1_" + "ACT_Zone_Home_inPos";

        /// <summary>
        /// A 1 quando il Robot si trova in zona di pick
        /// </summary>
        public const string ACT_Zone_Pick_inPos = "PLC1_" + "ACT_Zone_Pick_inPos";

        /// <summary>
        /// A 1 quando il Robot si trova in zona di place
        /// </summary>
        public const string ACT_Zone_Place_inPos = "PLC1_" + "ACT_Zone_Place_inPos";

        /// <summary>
        /// A 1 quando è avviato il ciclo main
        /// </summary>
        public const string CycleRun_Main = "PLC1_" + "CycleRun_Main";

        /// <summary>
        /// A 1 quando è avviato il ciclo home
        /// </summary>
        public const string CycleRun_Home = "PLC1_" + "CycleRun_Home";

        /// <summary>
        /// A 1 quando è avviato il ciclo pick
        /// </summary>
        public const string CycleRun_Pick = "PLC1_" + "CycleRun_Pick";

        /// <summary>
        /// A 1 quando è avviato il ciclo place
        /// </summary>
        public const string CycleRun_Place = "PLC1_" + "CycleRun_Place";

        #endregion

        #region HMI

        /// <summary>
        /// Livello collisione robot
        /// </summary>
        public const string ACT_CollisionLevel = "PLC1_" + "ACT_CollisionLevel";

        /// <summary>
        /// Numero di step main cycle
        /// </summary>
        public const string ACT_Step_Cycle_Home = "PLC1_" + "ACT_Step_HomeCycle";

        /// <summary>
        /// Numero di step main cycle
        /// </summary>
        public const string ACT_Step_MainCycle = "PLC1_" + "ACT_Step_MainCycle";

        /// <summary>
        /// Numero di step ciclo di pick
        /// </summary>
        public const string ACT_Step_Cycle_Pick = "PLC1_" + "ACT_Step_Cycle_Pick";

        /// <summary>
        /// Numero di step ciclo di place
        /// </summary>
        public const string ACT_Step_Cycle_Place = "PLC1_" + "ACT_Step_Cycle_Place";

        /// <summary>
        /// x axis actual position [mm]
        /// </summary>
        public const string x_actual_pos = "PLC1_" + "x_actual_pos";

        /// <summary>
        /// y axis actual position [mm]
        /// </summary>
        public const string y_actual_pos = "PLC1_" + "y_actual_pos";

        /// <summary>
        /// z axis actual position [mm]
        /// </summary>
        public const string z_actual_pos = "PLC1_" + "z_actual_pos";

        /// <summary>
        /// rx axis actual position [°]
        /// </summary>
        public const string rx_actual_pos = "PLC1_" + "rx_actual_pos";

        /// <summary>
        /// ry axis actual position [°]
        /// </summary>
        public const string ry_actual_pos = "PLC1_" + "ry_actual_pos";

        /// <summary>
        /// rz axis actual position [°]
        /// </summary>
        public const string rz_actual_pos = "PLC1_" + "rz_actual_pos";

        /// <summary>
        /// j1 actual position [°]
        /// </summary>
        public const string j1_actual_pos = "PLC1_" + "j1_actual_pos";

        /// <summary>
        /// j2 actual position [°]
        /// </summary>
        public const string j2_actual_pos = "PLC1_" + "j2_actual_pos";

        /// <summary>
        /// j3 actual position [°]
        /// </summary>
        public const string j3_actual_pos = "PLC1_" + "j3_actual_pos";

        /// <summary>
        /// j4 actual position [°]
        /// </summary>
        public const string j4_actual_pos = "PLC1_" + "j4_actual_pos";

        /// <summary>
        /// j5 actual position [°]
        /// </summary>
        public const string j5_actual_pos = "PLC1_" + "j5_actual_pos";

        /// <summary>
        /// j6 actual position [°]
        /// </summary>
        public const string j6_actual_pos = "PLC1_" + "j6_actual_pos";

        #endregion

        #endregion

        #region Lettura da PLC

        #region Robot

        /// <summary>
        /// Clock da leggere ogni secondo (su clock su PLC gira ogni 2s)
        /// </summary>
        public static string LifeBit_in = "PLC1_" + "com_robot_1";

        /// <summary>
        /// A 1 durante tutta la durante del ciclo
        /// </summary>
        public static string Start_Auto_Robot = "PLC1_" + "com_robot_2";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_to_read_50 = "PLC1_" + "com_robot_3";

        /// <summary>
        /// spare
        /// </summary>
        public static string CMD_OverrideAuto = "PLC1_" + "com_robot_4";

        /// <summary>
        /// 0-joint, 2-base, 4-tool, 8-workpiece
        /// </summary>
        public const string Jog_Ref_type = "PLC1_" + "com_robot_5";

        /// <summary>
        /// Jog speed set [%]
        /// </summary>
        public const string Jog_speed_set = "PLC1_" + "com_robot_6";

        /// <summary>
        /// 1=x / 2=y / 3=z / 4=rx / 5=ry / 6=rz
        /// </summary>
        public const string Axis_selection = "PLC1_" + "com_robot_7";

        /// <summary>
        /// x axis threshold [mm]
        /// </summary>
        public const string x_threshold = "PLC1_" + "com_robot_8";

        /// <summary>
        /// y axis threshold [mm]
        /// </summary>
        public const string y_threshold = "PLC1_" + "com_robot_9";

        /// <summary>
        /// z axis threshold [mm]
        /// </summary>
        public const string z_threshold = "PLC1_" + "com_robot_10";

        /// <summary>
        /// rx axis threshold [°]
        /// </summary>
        public const string rx_threshold = "PLC1_" + "com_robot_11";

        /// <summary>
        /// ry axis threshold [°]
        /// </summary>
        public const string ry_threshold = "PLC1_" + "com_robot_12";

        /// <summary>
        /// rz axis threshold [°]
        /// </summary>
        public const string rz_threshold = "PLC1_" + "com_robot_13";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_52_to_read = "PLC1_" + "com_robot_14";

        /// <summary>
        /// Enable robot from PLC
        /// </summary>
        public const string Enable = "PLC1_" + "com_robot_15";

        /// <summary>
        ///
        /// </summary>
        public const string MovePause = "PLC1_" + "com_robot_16";

        /// <summary>
        /// 0=Off, 1=auto, 2=man
        /// </summary>
        public const string Operating_Mode = "PLC1_" + "com_robot_17";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogX_pos = "PLC1_" + "com_robot_18";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogX_neg = "PLC1_" + "com_robot_19";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogY_pos = "PLC1_" + "com_robot_20";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogY_neg = "PLC1_" + "com_robot_21";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogZ_pos = "PLC1_" + "com_robot_22";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogZ_neg = "PLC1_" + "com_robot_23";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRX_pos = "PLC1_" + "com_robot_24";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRX_neg = "PLC1_" + "com_robot_25";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRY_pos = "PLC1_" + "com_robot_26";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRY_neg = "PLC1_" + "com_robot_27";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRZ_pos = "PLC1_" + "com_robot_28";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRZ_neg = "PLC1_" + "com_robot_29";

        #endregion

        #region HMI

        /// <summary>
        /// A 1 per far girare il main
        /// </summary>
        public static string CMD_StartCicloAuto = "PLC1_" + "com_robot_50";

        /// <summary>
        /// A 1 per fermare il main
        /// </summary>
        public static string CMD_StopCicloAuto = "PLC1_" + "com_robot_51";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string Alarm_Presence = "PLC1_" + "com_robot_52";

        /// <summary>
        /// A 1 per avvia routine per tornare in home
        /// </summary>
        public static string CMD_GoHome = "PLC1_" + "com_robot_53";

        /// <summary>
        /// A 1 quando il robot è in manuale
        /// </summary>
        public static string Manual_Mode = "PLC1_" + "com_robot_54";

        /// <summary>
        /// A 1 quando il robot è in automatico
        /// </summary>
        public static string Automatic_Mode = "PLC1_" + "com_robot_55";

        /// <summary>
        /// Formato selezionato
        /// </summary>
        public static string CMD_SelectedFormat = "PLC1_" + "com_robot_56";

        /// <summary>
        /// A 1 per resettare gli allarmi
        /// </summary>
        public static string CMD_ResetAlarms = "PLC1_" + "com_robot_57";

        /// <summary>
        /// A 1 per resettare gli allarmi
        /// </summary>
        public static string spare_2_to_read = "PLC1_" + "com_robot_58";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_3_to_read = "PLC1_" + "com_robot_59";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_4_to_read = "PLC1_" + "com_robot_60";

        /// <summary>
        /// Scatola ruotata/non ruotata
        /// </summary>
        public static string CMD_Box_Rotate_180 = "PLC1_" + "com_robot_61";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_to_read = "PLC1_" + "com_robot_62";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_1_to_read = "PLC1_" + "com_robot_63";

        /// <summary>
        /// A 1 per consenso di pick
        /// </summary>
        public static string Enable_To_Pick = "PLC1_" + "com_robot_64";

        /// <summary>
        /// A 1 per consenso di place
        /// </summary>
        public static string Enable_To_Place = "PLC1_" + "com_robot_65";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_5_to_read = "PLC1_" + "com_robot_66";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_6_to_read = "PLC1_" + "com_robot_67";

        /// <summary>
        /// A 1 per eseguire pick
        /// </summary>
        public static string CMD_Pick = "PLC1_" + "com_robot_68";

        /// <summary>
        /// A 1 per eseguire place
        /// </summary>
        public static string CMD_Place = "PLC1_" + "com_robot_69";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_7_to_read = "PLC1_" + "com_robot_70";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_8_to_read = "PLC1_" + "com_robot_71";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_9_to_read = "PLC1_" + "com_robot_72";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_10_to_read = "PLC1_" + "com_robot_73";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_11_to_read = "PLC1_" + "com_robot_74";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_12_to_read = "PLC1_" + "com_robot_75";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_13_to_read = "PLC1_" + "com_robot_76";

        /// <summary>
        /// apertura manuale pinze
        /// </summary>
        public static string CMD_MAN_openGrippers = "PLC1_" + "com_robot_77";

        /// <summary>
        /// chiusura manuale pinze
        /// </summary>
        public static string CMD_MAN_closeGrippers = "PLC1_" + "com_robot_78";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_64_to_read = "PLC1_" + "com_robot_79";

        /// <summary>
        /// Registra punto robot
        /// </summary>
        public static string CMD_RegisterPoint = "PLC1_" + "com_robot_80";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_62_to_read = "PLC1_" + "com_robot_81";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_63_to_read = "PLC1_" + "com_robot_82";

        /// <summary>
        /// Errore comunicazione pulsantiera
        /// </summary>
        public static string MobilePanel_CommError = "PLC1_" + "com_robot_83";

        /// xOffset punto di pick
        /// </summary>
        public static string OFFSET_Pick_X = "PLC1_" + "com_robot_94";

        /// <summary>
        /// yOffset punto di pick
        /// </summary>
        public static string OFFSET_Pick_Y = "PLC1_" + "com_robot_95";

        /// <summary>
        /// zOffset punto di pick
        /// </summary>
        public static string OFFSET_Pick_Z = "PLC1_" + "com_robot_96";

        /// <summary>
        /// xOffset punto di place
        /// </summary>
        public static string OFFSET_Place_X = "PLC1_" + "com_robot_97";

        /// <summary>
        /// yOffset punto di place
        /// </summary>
        public static string OFFSET_Place_Y = "PLC1_" + "com_robot_98";

        /// <summary>
        /// zOffset punto di place
        /// </summary>
        public static string OFFSET_Place_Z = "PLC1_" + "com_robot_99";

        #endregion

        #endregion


    }
}
